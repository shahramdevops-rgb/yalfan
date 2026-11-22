using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Api.IntegrationTests.Production;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;
using static Yalfan.Api.IntegrationTests.Parties.ContractorApi;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>Contractors through the API (roadmap 1.3, ADR 0019).</summary>
public sealed class ContractorTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task An_operator_adds_a_contractor_and_reads_him_back()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var sewing = await client.SeededStageAsync("دوخت");
        var packing = await client.SeededStageAsync("بسته‌بندی");
        var mobile = ApiFactory.NextMobile();

        var response = await client.PostAsJsonAsync(
            Contractors,
            new
            {
                fullName = "  علي   رضايي ",
                mobileNumber = "+98" + mobile[1..],
                stageIds = new[] { packing, sewing },
                notes = " کارگاه در خیابان خضر ",
                iban = "ir71 0570 0299 7160 1460 6410 01",
            },
            Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = (await response.Body()).Id();

        var contractor = await (await client.GetAsync($"{Contractors}/{id}", Cancel)).Body();
        Assert.Equal("علی رضایی", contractor.GetProperty("fullName").GetString());
        Assert.Equal(mobile, contractor.GetProperty("mobileNumber").GetString());
        Assert.Equal("کارگاه در خیابان خضر", contractor.GetProperty("notes").GetString());
        Assert.Equal(["دوخت", "بسته‌بندی"], contractor.StageNames());
        Assert.Equal("IR** **** **** **** **** **10 01", contractor.GetProperty("maskedIban").GetString());
        Assert.True(contractor.GetProperty("isActive").GetBoolean());
        Assert.False(contractor.TryGetProperty("iban", out _));
    }

    [Fact]
    public async Task A_contractor_without_an_iban_has_none_and_an_empty_iban_is_none()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var contractor = await client.CreateContractorAsync(iban: "  ");

        Assert.Equal(JsonValueKind.Null, contractor.GetProperty("maskedIban").ValueKind);
    }

    [Fact]
    public async Task Saving_the_profile_replaces_the_stages_and_never_touches_the_iban()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var sewing = await client.SeededStageAsync("دوخت");
        var packing = await client.SeededStageAsync("بسته‌بندی");
        var handles = await client.CreateStageAsync(CatalogApi.UniqueName("دوخت دسته"));
        var id = (await client.CreateContractorAsync(iban: Iban, stageIds: [sewing, packing])).Id();
        var newMobile = ApiFactory.NextMobile();

        var response = await client.PutAsJsonAsync(
            $"{Contractors}/{id}",
            new { fullName = "حسین کریمی", mobileNumber = newMobile, stageIds = new[] { packing, handles }, notes = (string?)null },
            Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contractor = await (await client.GetAsync($"{Contractors}/{id}", Cancel)).Body();
        Assert.Equal("حسین کریمی", contractor.GetProperty("fullName").GetString());
        Assert.Equal(newMobile, contractor.GetProperty("mobileNumber").GetString());
        Assert.Equal([packing, handles], contractor.GetProperty("stages").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()));
        Assert.Equal("IR** **** **** **** **** **10 01", contractor.GetProperty("maskedIban").GetString());
    }

    [Fact]
    public async Task The_iban_is_changed_and_removed_on_its_own()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateContractorAsync(iban: Iban)).Id();

        var changed = await client.PutAsJsonAsync($"{Contractors}/{id}/iban", new { iban = OtherIban }, Cancel);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal("IR** **** **** **** **** **00 01", (await changed.Body()).GetProperty("maskedIban").GetString());

        var removed = await client.DeleteAsync($"{Contractors}/{id}/iban", Cancel);
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await removed.Body()).GetProperty("maskedIban").ValueKind);
    }

    [Theory]
    [InlineData("IR710570029971601460641002")]
    [InlineData("GB710570029971601460641001")]
    [InlineData("6037991234567890")]
    public async Task A_wrong_iban_is_a_validation_error_on_the_iban_field(string iban)
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateContractorAsync()).Id();

        var response = await client.PutAsJsonAsync($"{Contractors}/{id}/iban", new { iban }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("iban", error.GetProperty("field").GetString());
        Assert.Equal("iban.invalid", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Only_the_owner_sees_the_whole_iban()
    {
        using var operatorClient = await factory.CreateClientAsAsync(Role.Operator);
        using var managerClient = await factory.CreateClientAsAsync(Role.Manager);
        using var ownerClient = await factory.CreateClientAsAsync(Role.Owner);
        var id = (await operatorClient.CreateContractorAsync(iban: Iban)).Id();

        var byOwner = await ownerClient.GetAsync($"{Contractors}/{id}/iban", Cancel);
        var byManager = await managerClient.GetAsync($"{Contractors}/{id}/iban", Cancel);
        var byOperator = await operatorClient.GetAsync($"{Contractors}/{id}/iban", Cancel);

        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        Assert.Equal(Iban, (await byOwner.Body()).GetProperty("iban").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, byManager.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byOperator.StatusCode);
    }

    [Fact]
    public async Task Asking_for_the_whole_iban_of_a_contractor_who_has_none_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Owner);
        var id = (await client.CreateContractorAsync()).Id();

        var response = await client.GetAsync($"{Contractors}/{id}/iban", Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("parties.iban_not_set", await response.ErrorCode());
    }

    [Fact]
    public async Task A_mobile_belongs_to_one_contractor_whatever_form_it_is_typed_in()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var mobile = ApiFactory.NextMobile();
        await client.CreateContractorAsync(mobileNumber: mobile);
        var persianForm = string.Concat(mobile.Select(c => (char)('۰' + (c - '0'))));

        var response = await client.PostAsJsonAsync(
            Contractors,
            new { fullName = "حسن", mobileNumber = persianForm, stageIds = new[] { await client.SeededStageAsync("دوخت") } },
            Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("parties.mobile_taken", await response.ErrorCode());
    }

    [Fact]
    public async Task Saving_with_another_contractors_mobile_is_refused_but_keeping_the_own_one_is_not()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var sewing = await client.SeededStageAsync("دوخت");
        var taken = ApiFactory.NextMobile();
        var own = ApiFactory.NextMobile();
        await client.CreateContractorAsync(mobileNumber: taken);
        var id = (await client.CreateContractorAsync(mobileNumber: own)).Id();

        var toTaken = await client.PutAsJsonAsync($"{Contractors}/{id}", new { fullName = "حسن", mobileNumber = taken, stageIds = new[] { sewing } }, Cancel);
        var toOwn = await client.PutAsJsonAsync($"{Contractors}/{id}", new { fullName = "حسن", mobileNumber = own, stageIds = new[] { sewing } }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, toTaken.StatusCode);
        Assert.Equal("parties.mobile_taken", await toTaken.ErrorCode());
        Assert.Equal(HttpStatusCode.OK, toOwn.StatusCode);
    }

    [Fact]
    public async Task Two_contractors_may_share_a_name()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var name = CatalogApi.UniqueName("محمد احمدی");

        var first = await client.CreateContractorAsync(fullName: name);
        var second = await client.CreateContractorAsync(fullName: name);

        Assert.NotEqual(first.Id(), second.Id());
    }

    [Fact]
    public async Task A_contractor_with_no_stage_is_a_validation_error_on_the_stages_field()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync(
            Contractors, new { fullName = "حسن", mobileNumber = ApiFactory.NextMobile(), stageIds = Array.Empty<Guid>() }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("stageIds", error.GetProperty("field").GetString());
        Assert.Equal("parties.stage_required", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_stage_that_does_not_exist_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync(
            Contractors, new { fullName = "حسن", mobileNumber = ApiFactory.NextMobile(), stageIds = new[] { Guid.CreateVersion7() } }, Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("parties.stage_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task A_contractor_is_deactivated_and_activated_again()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateContractorAsync()).Id();

        var deactivated = await client.PostAsync($"{Contractors}/{id}/deactivate", null, Cancel);
        Assert.False((await deactivated.Body()).GetProperty("isActive").GetBoolean());

        var activated = await client.PostAsync($"{Contractors}/{id}/activate", null, Cancel);
        Assert.True((await activated.Body()).GetProperty("isActive").GetBoolean());
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("PUT", "/iban")]
    [InlineData("DELETE", "/iban")]
    [InlineData("POST", "/deactivate")]
    public async Task A_contractor_that_does_not_exist_is_not_found(string method, string path)
    {
        using var client = await factory.CreateClientAsAsync(Role.Owner);
        var request = new HttpRequestMessage(new HttpMethod(method), $"{Contractors}/{Guid.CreateVersion7()}{path}");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new
            {
                fullName = "حسن",
                mobileNumber = ApiFactory.NextMobile(),
                stageIds = new[] { await client.SeededStageAsync("دوخت") },
                iban = Iban,
            });
        }

        var response = await client.SendAsync(request, Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("parties.contractor_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_read_the_contractors()
    {
        using var client = factory.CreateBrowserClient();

        var response = await client.GetAsync(Contractors, Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
