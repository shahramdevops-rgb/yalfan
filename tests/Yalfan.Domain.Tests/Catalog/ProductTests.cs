using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Domain.Tests.Catalog;

public class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Canvas = Guid.CreateVersion7();
    private static readonly Guid Leather = Guid.CreateVersion7();
    private static readonly Guid Black = Guid.CreateVersion7();
    private static readonly Guid Brown = Guid.CreateVersion7();

    private static Product WithoutSizes(int number = 101) =>
        Product.Create(number, "کیف دوشی", null, null, [], Now).Value;

    private static Product WithSizes(params string[] sizes) =>
        Product.Create(101, "کوله مدرسه", null, null, sizes, Now).Value;

    [Fact]
    public void A_new_model_is_active_with_its_number_and_persian_text()
    {
        var category = Guid.CreateVersion7();

        var product = Product.Create(105, " كيف  دوشی ", "  چرم طبیعی ", category, [], Now).Value;

        Assert.Equal(105, product.Number);
        Assert.Equal("کیف دوشی", product.PersianName);
        Assert.Equal("چرم طبیعی", product.PersianDescription);
        Assert.Equal(category, product.CategoryId);
        Assert.True(product.IsActive);
        Assert.Equal(Now, product.CreatedAt);
        Assert.Equal(Locale.Fa, Assert.Single(product.Translations).Locale);
        Assert.False(product.HasSizes);
        Assert.Empty(product.Variants);
    }

    [Fact]
    public void An_empty_description_is_stored_as_none()
    {
        Assert.Null(Product.Create(101, "کیف", "   ", null, [], Now).Value.PersianDescription);
    }

    [Fact]
    public void Name_and_description_are_checked()
    {
        Assert.Equal(CatalogErrors.NameRequired, Product.Create(101, " ", null, null, [], Now).Error);
        Assert.Equal(
            CatalogErrors.NameTooLong,
            Product.Create(101, new string('ک', Product.NameMaxLength + 1), null, null, [], Now).Error);
        Assert.Equal(
            CatalogErrors.DescriptionTooLong,
            Product.Create(101, "کیف", new string('ک', Product.DescriptionMaxLength + 1), null, [], Now).Error);
    }

    [Fact]
    public void Three_colours_make_three_variants_numbered_with_the_model()
    {
        var product = WithoutSizes(number: 104);

        var skus = new[] { Black, Brown, Guid.CreateVersion7() }
            .Select(colour => product.AddVariant(Canvas, colour, null).Value.Sku)
            .ToList();

        Assert.Equal(["104-1", "104-2", "104-3"], skus);
        Assert.Equal([1, 2, 3], product.Variants.Select(v => v.Number));
        Assert.All(product.Variants, v => Assert.True(v.IsActive));
    }

    [Fact]
    public void The_same_colour_in_another_fabric_is_another_variant()
    {
        var product = WithoutSizes();

        Assert.True(product.AddVariant(Canvas, Black, null).IsSuccess);
        Assert.True(product.AddVariant(Leather, Black, null).IsSuccess);
        Assert.Equal(2, product.Variants.Count);
    }

    [Fact]
    public void The_same_fabric_colour_and_size_cannot_be_added_twice()
    {
        var product = WithoutSizes();
        product.AddVariant(Canvas, Black, null);

        Assert.Equal(CatalogErrors.VariantExists, product.AddVariant(Canvas, Black, null).Error);
        Assert.Single(product.Variants);
    }

    [Fact]
    public void Sizes_keep_the_order_they_were_typed_in()
    {
        var product = WithSizes("کوچک", "متوسط", "بزرگ");

        Assert.True(product.HasSizes);
        Assert.Equal(["کوچک", "متوسط", "بزرگ"], product.Sizes.Select(s => s.PersianName));
        Assert.Equal([1, 2, 3], product.Sizes.Select(s => s.Position));
    }

    [Fact]
    public void Two_sizes_of_one_model_cannot_share_a_name()
    {
        Assert.Equal(
            CatalogErrors.SizeNameDuplicate,
            Product.Create(101, "کوله", null, null, ["بزرگ", " بزرگ "], Now).Error);
    }

    [Fact]
    public void A_size_needs_a_name()
    {
        Assert.Equal(CatalogErrors.NameRequired, Product.Create(101, "کوله", null, null, ["کوچک", ""], Now).Error);
    }

    [Fact]
    public void Each_size_of_a_colour_is_its_own_variant()
    {
        var product = WithSizes("کوچک", "بزرگ");

        foreach (var size in product.Sizes)
        {
            Assert.True(product.AddVariant(Canvas, Black, size.Id).IsSuccess);
        }

        Assert.Equal(product.Sizes.Select(s => (Guid?)s.Id), product.Variants.Select(v => v.SizeId));
    }

    [Fact]
    public void A_model_with_sizes_needs_a_size_on_every_variant()
    {
        Assert.Equal(CatalogErrors.SizeRequired, WithSizes("کوچک", "بزرگ").AddVariant(Canvas, Black, null).Error);
    }

    [Fact]
    public void A_model_without_sizes_takes_no_size()
    {
        Assert.Equal(CatalogErrors.SizeNotAllowed, WithoutSizes().AddVariant(Canvas, Black, Guid.CreateVersion7()).Error);
    }

    [Fact]
    public void A_size_of_another_model_is_not_found()
    {
        var other = WithSizes("بزرگ");

        Assert.Equal(CatalogErrors.SizeNotFound, WithSizes("بزرگ").AddVariant(Canvas, Black, other.Sizes[0].Id).Error);
    }

    [Fact]
    public void A_size_is_found_by_its_name_as_typed_on_any_keyboard()
    {
        var product = WithSizes("کوچک", "بزرگ");

        Assert.Equal(product.Sizes[0].Id, product.FindSize("كوچك")?.Id);
        Assert.Null(product.FindSize("متوسط"));
    }
}
