using Yalfan.Domain.Production;

namespace Yalfan.Domain.Tests.Production;

public class StageTests
{
    [Fact]
    public void A_stage_keeps_its_name_normalized()
    {
        var stage = Stage.Create("  دوخت   دسته كيف ", 1).Value;

        Assert.Equal("دوخت دسته کیف", stage.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void A_name_is_required(string? name)
    {
        Assert.Equal(ProductionErrors.NameRequired, Stage.Create(name, 1).Error);
    }

    [Fact]
    public void A_name_has_a_maximum_length()
    {
        Assert.True(Stage.Create(new string('ر', Stage.NameMaxLength), 1).IsSuccess);
        Assert.Equal(ProductionErrors.NameTooLong, Stage.Create(new string('ر', Stage.NameMaxLength + 1), 1).Error);
    }

    [Fact]
    public void Renaming_normalizes_the_new_name()
    {
        var stage = Stage.Create("دوخت", 1).Value;

        Assert.True(stage.Rename(" دوخت  بدنه ").IsSuccess);

        Assert.Equal("دوخت بدنه", stage.Name);
    }

    [Fact]
    public void A_bad_rename_keeps_the_old_name()
    {
        var stage = Stage.Create("برش", 1).Value;

        Assert.Equal(ProductionErrors.NameTooLong, stage.Rename(new string('ر', Stage.NameMaxLength + 1)).Error);
        Assert.Equal("برش", stage.Name);
    }

    [Fact]
    public void A_new_stage_is_active_at_the_place_it_is_given()
    {
        var stage = Stage.Create("اتو", 4).Value;

        Assert.True(stage.IsActive);
        Assert.Equal(4, stage.Position);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_place_starts_at_one(int position)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Stage.Create("اتو", position));
    }

    [Fact]
    public void A_stage_is_deactivated_and_activated_again_without_losing_its_place()
    {
        var stage = Stage.Create("اتو", 3).Value;

        stage.Deactivate();
        Assert.False(stage.IsActive);

        stage.Activate();
        Assert.True(stage.IsActive);
        Assert.Equal(3, stage.Position);
    }

    [Fact]
    public void Reordering_puts_every_stage_at_its_place_in_the_new_list()
    {
        var cutting = Stage.Create("برش", 1).Value;
        var sewing = Stage.Create("دوخت", 2).Value;
        var packing = Stage.Create("بسته‌بندی", 3).Value;
        var handles = Stage.Create("دوخت دسته", 4).Value;

        var result = Stage.Reorder([cutting, sewing, packing, handles], [cutting.Id, sewing.Id, handles.Id, packing.Id]);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2, 3, 4], new[] { cutting, sewing, handles, packing }.Select(stage => stage.Position));
    }

    [Fact]
    public void An_inactive_stage_is_reordered_like_the_others()
    {
        var cutting = Stage.Create("برش", 1).Value;
        var sewing = Stage.Create("دوخت", 2).Value;
        sewing.Deactivate();

        Assert.True(Stage.Reorder([cutting, sewing], [sewing.Id, cutting.Id]).IsSuccess);

        Assert.Equal(1, sewing.Position);
        Assert.Equal(2, cutting.Position);
    }

    public static TheoryData<string> OutdatedLists => new() { "missing", "unknown", "twice", "extra" };

    [Theory]
    [MemberData(nameof(OutdatedLists))]
    public void A_list_that_does_not_name_every_stage_exactly_once_changes_nothing(string mistake)
    {
        var cutting = Stage.Create("برش", 1).Value;
        var sewing = Stage.Create("دوخت", 2).Value;
        var packing = Stage.Create("بسته‌بندی", 3).Value;

        IReadOnlyList<Guid> order = mistake switch
        {
            "missing" => [packing.Id, cutting.Id],
            "unknown" => [packing.Id, cutting.Id, Guid.CreateVersion7()],
            "twice" => [packing.Id, cutting.Id, cutting.Id],
            _ => [packing.Id, sewing.Id, cutting.Id, Guid.CreateVersion7()],
        };

        Assert.Equal(ProductionErrors.StageOrderOutdated, Stage.Reorder([cutting, sewing, packing], order).Error);
        Assert.Equal([1, 2, 3], new[] { cutting, sewing, packing }.Select(stage => stage.Position));
    }
}
