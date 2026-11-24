using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Domain.Tests.Catalog;

/// <summary>Category, fabric and colour share one shape.</summary>
public class CatalogOptionTests
{
    [Fact]
    public void A_colour_keeps_its_normalized_name_as_its_only_translation()
    {
        var colour = Colour.Create("  مشكى ").Value;

        Assert.Equal("مشکی", colour.PersianName);
        Assert.Equal(Locale.Fa, Assert.Single(colour.Names).Locale);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void A_name_is_required(string? name)
    {
        Assert.Equal(CatalogErrors.NameRequired, Colour.Create(name).Error);
        Assert.Equal(CatalogErrors.NameRequired, Fabric.Create(name).Error);
        Assert.Equal(CatalogErrors.NameRequired, Category.Create(name).Error);
    }

    [Fact]
    public void A_name_has_a_maximum_length()
    {
        Assert.True(Colour.Create(new string('ر', Colour.NameMaxLength)).IsSuccess);
        Assert.Equal(CatalogErrors.NameTooLong, Colour.Create(new string('ر', Colour.NameMaxLength + 1)).Error);
    }

    [Fact]
    public void Renaming_changes_the_persian_name_in_place()
    {
        var fabric = Fabric.Create("برزنت").Value;
        var row = Assert.Single(fabric.Names);

        Assert.True(fabric.Rename("برزنت ضدآب").IsSuccess);

        Assert.Equal("برزنت ضدآب", fabric.PersianName);
        Assert.Same(row, Assert.Single(fabric.Names));
    }

    [Fact]
    public void A_bad_rename_keeps_the_old_name()
    {
        var category = Category.Create("کوله").Value;

        Assert.Equal(CatalogErrors.NameRequired, category.Rename(" ").Error);
        Assert.Equal("کوله", category.PersianName);
    }

    [Fact]
    public void A_new_category_fabric_or_colour_is_active()
    {
        Assert.True(Category.Create("کوله").Value.IsActive);
        Assert.True(Fabric.Create("برزنت").Value.IsActive);
        Assert.True(Colour.Create("مشکی").Value.IsActive);
    }

    [Fact]
    public void A_colour_is_deactivated_and_activated_again_keeping_its_name()
    {
        var colour = Colour.Create("سرمه‌ای").Value;

        colour.Deactivate();
        Assert.False(colour.IsActive);

        colour.Activate();
        Assert.True(colour.IsActive);
        Assert.Equal("سرمه‌ای", colour.PersianName);
    }

    [Fact]
    public void A_category_and_a_fabric_are_deactivated_the_same_way()
    {
        var category = Category.Create("کوله").Value;
        var fabric = Fabric.Create("برزنت").Value;

        category.Deactivate();
        fabric.Deactivate();

        Assert.False(category.IsActive);
        Assert.False(fabric.IsActive);
    }
}
