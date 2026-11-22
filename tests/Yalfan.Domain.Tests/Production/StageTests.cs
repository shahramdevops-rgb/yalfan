using Yalfan.Domain.Production;

namespace Yalfan.Domain.Tests.Production;

public class StageTests
{
    [Fact]
    public void A_stage_keeps_its_name_normalized()
    {
        var stage = Stage.Create("  دوخت   دسته كيف ").Value;

        Assert.Equal("دوخت دسته کیف", stage.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void A_name_is_required(string? name)
    {
        Assert.Equal(ProductionErrors.NameRequired, Stage.Create(name).Error);
    }

    [Fact]
    public void A_name_has_a_maximum_length()
    {
        Assert.True(Stage.Create(new string('ر', Stage.NameMaxLength)).IsSuccess);
        Assert.Equal(ProductionErrors.NameTooLong, Stage.Create(new string('ر', Stage.NameMaxLength + 1)).Error);
    }

    [Fact]
    public void Renaming_normalizes_the_new_name()
    {
        var stage = Stage.Create("دوخت").Value;

        Assert.True(stage.Rename(" دوخت  بدنه ").IsSuccess);

        Assert.Equal("دوخت بدنه", stage.Name);
    }

    [Fact]
    public void A_bad_rename_keeps_the_old_name()
    {
        var stage = Stage.Create("برش").Value;

        Assert.Equal(ProductionErrors.NameTooLong, stage.Rename(new string('ر', Stage.NameMaxLength + 1)).Error);
        Assert.Equal("برش", stage.Name);
    }
}
