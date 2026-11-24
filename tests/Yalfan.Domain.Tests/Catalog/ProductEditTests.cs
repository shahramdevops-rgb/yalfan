using System.Globalization;
using Yalfan.Domain.Catalog;

namespace Yalfan.Domain.Tests.Catalog;

/// <summary>Changing a model after it was entered (task 1.6.2, ADR 0023).</summary>
public class ProductEditTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Canvas = Guid.CreateVersion7();
    private static readonly Guid Leather = Guid.CreateVersion7();
    private static readonly Guid Black = Guid.CreateVersion7();
    private static readonly Guid Brown = Guid.CreateVersion7();

    /// <summary>Every fabric and colour is still in use.</summary>
    private static readonly Func<Guid, Guid, bool> Anything = (_, _) => true;

    private static Product WithoutSizes() => Product.Create(101, "کیف دوشی", null, null, [], Now).Value;

    private static Product WithSizes(params string[] sizes) => Product.Create(102, "کوله مدرسه", null, null, sizes, Now).Value;

    [Fact]
    public void Name_description_and_category_are_changed_but_not_the_number()
    {
        var product = Product.Create(105, "کیف", "قدیمی", Guid.CreateVersion7(), [], Now).Value;
        var category = Guid.CreateVersion7();

        Assert.True(product.Update(" كيف  کمری ", "  دو جیب ", category).IsSuccess);

        Assert.Equal("کیف کمری", product.PersianName);
        Assert.Equal("دو جیب", product.PersianDescription);
        Assert.Equal(category, product.CategoryId);
        Assert.Equal(105, product.Number);
    }

    [Fact]
    public void An_emptied_description_and_category_are_removed()
    {
        var product = Product.Create(101, "کیف", "قدیمی", Guid.CreateVersion7(), [], Now).Value;

        product.Update("کیف", "  ", null);

        Assert.Null(product.PersianDescription);
        Assert.Null(product.CategoryId);
    }

    [Fact]
    public void A_refused_change_leaves_the_model_as_it_was()
    {
        var product = Product.Create(101, "کیف", "قدیمی", null, [], Now).Value;

        Assert.Equal(CatalogErrors.NameRequired, product.Update(" ", "تازه", null).Error);
        Assert.Equal(
            CatalogErrors.DescriptionTooLong,
            product.Update("کیف تازه", new string('ک', Product.DescriptionMaxLength + 1), null).Error);

        Assert.Equal("کیف", product.PersianName);
        Assert.Equal("قدیمی", product.PersianDescription);
    }

    [Fact]
    public void A_deactivated_model_keeps_its_bags_switches_and_takes_nothing_new()
    {
        var product = WithoutSizes();
        product.AddVariant(Canvas, Black, null);

        product.Deactivate();

        Assert.False(product.IsActive);
        Assert.True(product.Variants[0].IsActive);
        Assert.Equal(CatalogErrors.ProductInactive, product.AddVariant(Canvas, Brown, null).Error);
        Assert.Equal(CatalogErrors.ProductInactive, product.AddSize("بزرگ", Anything).Error);

        product.Activate();

        Assert.True(product.IsActive);
        Assert.True(product.AddVariant(Canvas, Brown, null).IsSuccess);
    }

    [Fact]
    public void A_bag_is_switched_off_and_on_again()
    {
        var product = WithoutSizes();
        var bag = product.AddVariant(Canvas, Black, null).Value;

        Assert.True(product.SetVariantActive(bag.Id, isActive: false).IsSuccess);
        Assert.False(bag.IsActive);

        Assert.True(product.SetVariantActive(bag.Id, isActive: true).IsSuccess);
        Assert.True(bag.IsActive);
    }

    [Fact]
    public void A_bag_of_another_model_is_not_found()
    {
        var other = WithoutSizes();
        var bag = other.AddVariant(Canvas, Black, null).Value;

        Assert.Equal(CatalogErrors.VariantNotFound, WithoutSizes().SetVariantActive(bag.Id, isActive: false).Error);
    }

    [Fact]
    public void The_first_size_of_a_model_becomes_the_size_of_its_bags_and_their_codes_stay()
    {
        var product = WithoutSizes();
        product.AddVariant(Canvas, Black, null);
        var switchedOff = product.AddVariant(Canvas, Brown, null).Value;
        product.SetVariantActive(switchedOff.Id, isActive: false);

        var medium = product.AddSize(" متوسط ", Anything).Value;

        Assert.True(product.HasSizes);
        Assert.Equal("متوسط", medium.PersianName);
        Assert.Equal(1, medium.Position);
        Assert.Equal(2, product.Variants.Count);
        Assert.All(product.Variants, v => Assert.Equal(medium.Id, v.SizeId));
        Assert.Equal(["101-1", "101-2"], product.Variants.Select(v => v.Sku));
    }

    [Fact]
    public void A_further_size_is_made_in_every_fabric_and_colour_still_made()
    {
        var product = WithSizes("کوچک", "بزرگ");
        var small = product.Sizes[0].Id;
        var large = product.Sizes[1].Id;
        product.AddVariant(Canvas, Black, small);
        product.AddVariant(Canvas, Black, large);
        product.AddVariant(Leather, Brown, small);

        var medium = product.AddSize("متوسط", Anything).Value;

        Assert.Equal(3, medium.Position);
        var inMedium = product.Variants.Where(v => v.SizeId == medium.Id).ToList();
        Assert.Equal([(Canvas, Black), (Leather, Brown)], inMedium.Select(v => (v.FabricId, v.ColourId)));
        Assert.Equal(["102-4", "102-5"], inMedium.Select(v => v.Sku));
    }

    [Fact]
    public void A_further_size_leaves_out_a_colour_no_longer_made()
    {
        var product = WithSizes("کوچک");
        var small = product.Sizes[0].Id;
        product.AddVariant(Canvas, Black, small);
        var brown = product.AddVariant(Canvas, Brown, small).Value;
        product.SetVariantActive(brown.Id, isActive: false);

        var large = product.AddSize("بزرگ", Anything).Value;

        var inLarge = Assert.Single(product.Variants, v => v.SizeId == large.Id);
        Assert.Equal(Black, inLarge.ColourId);
    }

    [Fact]
    public void A_further_size_leaves_out_a_fabric_or_colour_the_caller_says_is_not_used()
    {
        var product = WithSizes("کوچک");
        var small = product.Sizes[0].Id;
        product.AddVariant(Canvas, Black, small);
        product.AddVariant(Leather, Black, small);

        var large = product.AddSize("بزرگ", (fabricId, _) => fabricId != Leather).Value;

        var inLarge = Assert.Single(product.Variants, v => v.SizeId == large.Id);
        Assert.Equal(Canvas, inLarge.FabricId);
    }

    [Fact]
    public void A_new_size_is_checked_like_the_typed_ones()
    {
        var product = WithSizes("کوچک", "بزرگ");

        Assert.Equal(CatalogErrors.SizeNameDuplicate, product.AddSize(" بزرگ ", Anything).Error);
        Assert.Equal(CatalogErrors.NameRequired, product.AddSize(" ", Anything).Error);
        Assert.Equal(CatalogErrors.NameTooLong, product.AddSize(new string('ک', ProductSize.NameMaxLength + 1), Anything).Error);
        Assert.Equal(2, product.Sizes.Count);
    }

    [Fact]
    public void A_model_has_at_most_twenty_sizes()
    {
        var product = WithSizes(
            Enumerable.Range(1, Product.MaxSizes).Select(n => string.Create(CultureInfo.InvariantCulture, $"سایز {n}")).ToArray());

        Assert.Equal(CatalogErrors.TooManySizes, product.AddSize("یکی دیگر", Anything).Error);
    }

    [Fact]
    public void A_size_is_renamed_and_stays_unique_within_the_model()
    {
        var product = WithSizes("کوچک", "بزرگ");
        var small = product.Sizes[0].Id;

        Assert.True(product.RenameSize(small, " كوچك‌تر ").IsSuccess);
        Assert.Equal("کوچک‌تر", product.Sizes[0].PersianName);

        Assert.Equal(CatalogErrors.SizeNameDuplicate, product.RenameSize(small, "بزرگ").Error);
        Assert.Equal(CatalogErrors.NameRequired, product.RenameSize(small, "").Error);
        Assert.Equal(CatalogErrors.SizeNotFound, product.RenameSize(Guid.CreateVersion7(), "میانه").Error);
        Assert.Equal("کوچک‌تر", product.Sizes[0].PersianName);
    }

    [Fact]
    public void Renaming_a_size_to_its_own_name_is_not_a_duplicate()
    {
        var product = WithSizes("کوچک", "بزرگ");

        Assert.True(product.RenameSize(product.Sizes[1].Id, "بزرگ").IsSuccess);
    }

    [Fact]
    public void Sizes_are_put_in_the_dragged_order()
    {
        var product = WithSizes("کوچک", "بزرگ", "متوسط");
        var ids = product.Sizes.Select(s => s.Id).ToList();

        Assert.True(product.ReorderSizes([ids[0], ids[2], ids[1]]).IsSuccess);

        Assert.Equal(["کوچک", "متوسط", "بزرگ"], product.Sizes.Select(s => s.PersianName));
        Assert.Equal([1, 2, 3], product.Sizes.Select(s => s.Position));
    }

    [Fact]
    public void An_order_that_does_not_name_every_size_once_changes_nothing()
    {
        var product = WithSizes("کوچک", "بزرگ");
        var ids = product.Sizes.Select(s => s.Id).ToList();

        Assert.Equal(CatalogErrors.SizeOrderOutdated, product.ReorderSizes([ids[1]]).Error);
        Assert.Equal(CatalogErrors.SizeOrderOutdated, product.ReorderSizes([ids[1], ids[1]]).Error);
        Assert.Equal(CatalogErrors.SizeOrderOutdated, product.ReorderSizes([ids[1], Guid.CreateVersion7()]).Error);
        Assert.Equal(CatalogErrors.SizeOrderOutdated, product.ReorderSizes([ids[1], ids[0], Guid.CreateVersion7()]).Error);

        Assert.Equal(["کوچک", "بزرگ"], product.Sizes.Select(s => s.PersianName));
    }

    [Fact]
    public void A_deactivated_model_may_still_have_its_sizes_corrected()
    {
        var product = WithSizes("کوچک", "بزرگ");
        var ids = product.Sizes.Select(s => s.Id).ToList();
        product.Deactivate();

        Assert.True(product.RenameSize(ids[0], "خرد").IsSuccess);
        Assert.True(product.ReorderSizes([ids[1], ids[0]]).IsSuccess);
    }
}
