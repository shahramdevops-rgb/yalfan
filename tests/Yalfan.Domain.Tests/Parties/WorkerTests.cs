using Yalfan.Domain.Parties;

namespace Yalfan.Domain.Tests.Parties;

public class WorkerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateOnly Started = new(2021, 3, 21);
    private static readonly MobileNumber Mobile = MobileNumber.Create("09121234567").Value;
    private static readonly Guid Cutting = Guid.CreateVersion7();
    private static readonly Guid Sewing = Guid.CreateVersion7();
    private static readonly Guid Packing = Guid.CreateVersion7();

    [Fact]
    public void A_new_worker_is_active_with_a_normalized_name_a_start_date_and_no_iban()
    {
        var worker = Worker.Create("  زهرا   كريمي ", Mobile, [Cutting], Started, "  شیفت صبح ", Today, Now).Value;

        Assert.Equal("زهرا کریمی", worker.FullName);
        Assert.Equal(Mobile, worker.MobileNumber);
        Assert.Equal([Cutting], worker.StageIds);
        Assert.Equal(Started, worker.StartDate);
        Assert.Equal("شیفت صبح", worker.Notes);
        Assert.Null(worker.Iban);
        Assert.True(worker.IsActive);
        Assert.Equal(Now, worker.CreatedAt);
    }

    [Fact]
    public void A_worker_may_do_no_stage()
    {
        var worker = Worker.Create("رضا", Mobile, [], Started, "انباردار", Today, Now).Value;

        Assert.Empty(worker.StageIds);
    }

    [Fact]
    public void A_stage_ticked_twice_is_one_stage()
    {
        var worker = Worker.Create("رضا", Mobile, [Sewing, Sewing, Packing], Started, null, Today, Now).Value;

        Assert.Equal(new[] { Sewing, Packing }.Order(), worker.StageIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void A_name_is_required(string? name)
    {
        Assert.Equal(PartiesErrors.FullNameRequired, Worker.Create(name, Mobile, [], Started, null, Today, Now).Error);
    }

    [Fact]
    public void A_name_and_notes_have_the_same_limits_as_a_contractors()
    {
        Assert.Equal(Contractor.FullNameMaxLength, Worker.FullNameMaxLength);
        Assert.Equal(Contractor.NotesMaxLength, Worker.NotesMaxLength);
        Assert.Equal(
            PartiesErrors.FullNameTooLong,
            Worker.Create(new string('ر', Worker.FullNameMaxLength + 1), Mobile, [], Started, null, Today, Now).Error);
        Assert.Equal(
            PartiesErrors.NotesTooLong,
            Worker.Create("رضا", Mobile, [], Started, new string('ن', Worker.NotesMaxLength + 1), Today, Now).Error);
        Assert.Null(Worker.Create("رضا", Mobile, [], Started, "   ", Today, Now).Value.Notes);
    }

    [Fact]
    public void Today_is_a_start_date_and_tomorrow_is_not()
    {
        Assert.Equal(Today, Worker.Create("رضا", Mobile, [], Today, null, Today, Now).Value.StartDate);
        Assert.Equal(
            PartiesErrors.StartDateInFuture,
            Worker.Create("رضا", Mobile, [], Today.AddDays(1), null, Today, Now).Error);
    }

    [Fact]
    public void Updating_the_profile_replaces_the_stages_and_the_start_date_and_keeps_the_iban()
    {
        var worker = Worker.Create("رضا", Mobile, [Cutting, Sewing], Started, null, Today, Now).Value;
        var iban = Iban.Create("IR710570029971601460641001").Value;
        worker.ChangeIban(iban);
        var newMobile = MobileNumber.Create("09351234567").Value;
        var newStart = new DateOnly(2020, 1, 1);

        var result = worker.UpdateProfile("رضا احمدی", newMobile, [Sewing, Packing], newStart, "تاریخ تقریبی", Today);

        Assert.True(result.IsSuccess);
        Assert.Equal("رضا احمدی", worker.FullName);
        Assert.Equal(newMobile, worker.MobileNumber);
        Assert.Equal(new[] { Sewing, Packing }.Order(), worker.StageIds);
        Assert.Equal(newStart, worker.StartDate);
        Assert.Equal("تاریخ تقریبی", worker.Notes);
        Assert.Equal(iban, worker.Iban);
    }

    [Fact]
    public void A_refused_update_changes_nothing()
    {
        var worker = Worker.Create("رضا", Mobile, [Sewing], Started, "یادداشت", Today, Now).Value;

        var result = worker.UpdateProfile("حسن", MobileNumber.Create("09351234567").Value, [], Today.AddYears(1), null, Today);

        Assert.Equal(PartiesErrors.StartDateInFuture, result.Error);
        Assert.Equal("رضا", worker.FullName);
        Assert.Equal(Mobile, worker.MobileNumber);
        Assert.Equal([Sewing], worker.StageIds);
        Assert.Equal(Started, worker.StartDate);
        Assert.Equal("یادداشت", worker.Notes);
    }

    [Fact]
    public void The_iban_is_set_replaced_and_removed()
    {
        var worker = Worker.Create("رضا", Mobile, [], Started, null, Today, Now).Value;
        var first = Iban.Create("IR710570029971601460641001").Value;
        var second = Iban.Create("IR062960000000100324200001").Value;

        worker.ChangeIban(first);
        Assert.Equal(first, worker.Iban);

        worker.ChangeIban(second);
        Assert.Equal(second, worker.Iban);

        worker.RemoveIban();
        Assert.Null(worker.Iban);
    }

    [Fact]
    public void A_worker_is_deactivated_and_activated_again()
    {
        var worker = Worker.Create("رضا", Mobile, [], Started, null, Today, Now).Value;

        worker.Deactivate();
        Assert.False(worker.IsActive);

        worker.Activate();
        Assert.True(worker.IsActive);
    }
}
