using Yalfan.Domain.Parties;

namespace Yalfan.Domain.Tests.Parties;

public class ContractorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly MobileNumber Mobile = MobileNumber.Create("09121234567").Value;
    private static readonly Guid Sewing = Guid.CreateVersion7();
    private static readonly Guid Packing = Guid.CreateVersion7();
    private static readonly Guid Handles = Guid.CreateVersion7();

    [Fact]
    public void A_new_contractor_is_active_with_a_normalized_name_and_no_iban()
    {
        var contractor = Contractor.Create("  علي   رضايي ", Mobile, [Sewing], "  کارگاه در خضر ", Now).Value;

        Assert.Equal("علی رضایی", contractor.FullName);
        Assert.Equal(Mobile, contractor.MobileNumber);
        Assert.Equal("کارگاه در خضر", contractor.Notes);
        Assert.Equal([Sewing], contractor.StageIds);
        Assert.Null(contractor.Iban);
        Assert.True(contractor.IsActive);
        Assert.Equal(Now, contractor.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void A_name_is_required(string? name)
    {
        Assert.Equal(PartiesErrors.FullNameRequired, Contractor.Create(name, Mobile, [Sewing], null, Now).Error);
    }

    [Fact]
    public void A_name_has_a_maximum_length()
    {
        Assert.True(Contractor.Create(new string('ر', Contractor.FullNameMaxLength), Mobile, [Sewing], null, Now).IsSuccess);
        Assert.Equal(
            PartiesErrors.FullNameTooLong,
            Contractor.Create(new string('ر', Contractor.FullNameMaxLength + 1), Mobile, [Sewing], null, Now).Error);
    }

    [Fact]
    public void Empty_notes_are_no_notes_and_notes_have_a_maximum_length()
    {
        Assert.Null(Contractor.Create("علی", Mobile, [Sewing], "   ", Now).Value.Notes);
        Assert.Equal(
            PartiesErrors.NotesTooLong,
            Contractor.Create("علی", Mobile, [Sewing], new string('ن', Contractor.NotesMaxLength + 1), Now).Error);
    }

    [Fact]
    public void A_contractor_does_at_least_one_stage()
    {
        Assert.Equal(PartiesErrors.StageRequired, Contractor.Create("علی", Mobile, [], null, Now).Error);
    }

    [Fact]
    public void A_stage_ticked_twice_is_one_stage()
    {
        var contractor = Contractor.Create("علی", Mobile, [Sewing, Sewing, Packing], null, Now).Value;

        Assert.Equal(new[] { Sewing, Packing }.Order(), contractor.StageIds);
    }

    [Fact]
    public void Updating_the_profile_replaces_the_stages_and_keeps_the_iban()
    {
        var contractor = Contractor.Create("علی", Mobile, [Sewing, Packing], null, Now).Value;
        var iban = Iban.Create("IR710570029971601460641001").Value;
        contractor.ChangeIban(iban);
        var newMobile = MobileNumber.Create("09351234567").Value;

        var result = contractor.UpdateProfile("علی رضایی", newMobile, [Packing, Handles], "فقط دسته");

        Assert.True(result.IsSuccess);
        Assert.Equal("علی رضایی", contractor.FullName);
        Assert.Equal(newMobile, contractor.MobileNumber);
        Assert.Equal("فقط دسته", contractor.Notes);
        Assert.Equal(new[] { Packing, Handles }.Order(), contractor.StageIds);
        Assert.Equal(iban, contractor.Iban);
    }

    [Fact]
    public void A_refused_update_changes_nothing()
    {
        var contractor = Contractor.Create("علی", Mobile, [Sewing], "یادداشت", Now).Value;

        var result = contractor.UpdateProfile("حسن", MobileNumber.Create("09351234567").Value, [], null);

        Assert.Equal(PartiesErrors.StageRequired, result.Error);
        Assert.Equal("علی", contractor.FullName);
        Assert.Equal(Mobile, contractor.MobileNumber);
        Assert.Equal("یادداشت", contractor.Notes);
        Assert.Equal([Sewing], contractor.StageIds);
    }

    [Fact]
    public void The_iban_is_set_replaced_and_removed()
    {
        var contractor = Contractor.Create("علی", Mobile, [Sewing], null, Now).Value;
        var first = Iban.Create("IR710570029971601460641001").Value;
        var second = Iban.Create("IR062960000000100324200001").Value;

        contractor.ChangeIban(first);
        Assert.Equal(first, contractor.Iban);

        contractor.ChangeIban(second);
        Assert.Equal(second, contractor.Iban);

        contractor.RemoveIban();
        Assert.Null(contractor.Iban);
    }

    [Fact]
    public void A_contractor_is_deactivated_and_activated_again()
    {
        var contractor = Contractor.Create("علی", Mobile, [Sewing], null, Now).Value;

        contractor.Deactivate();
        Assert.False(contractor.IsActive);

        contractor.Activate();
        Assert.True(contractor.IsActive);
    }
}
