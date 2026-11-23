using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;
using static Yalfan.Api.IntegrationTests.Parties.ContractorApi;
using static Yalfan.Api.IntegrationTests.Parties.WorkerApi;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>In-house workers through the API (roadmap 1.4, ADR 0020).</summary>
public sealed class WorkerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task An_operator_adds_a_worker_and_reads_the_worker_back()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var cutting = await client.SeededStageAsync("برش");
        var sewing = await client.SeededStageAsync("دوخت");
        var mobile = ApiFactory.NextMobile();

        var response = await client.PostAsJsonAsync(
            Workers,
            new
            {
                fullName = "  زهرا   كريمي ",
                mobileNumber = "+98" + mobile[1..],
                stageIds = new[] { sewing, cutting },
                startDate = "2021-03-21",
                notes = " تاریخ شروع تقریبی است ",
                iban = "ir71 0570 0299 7160 1460 6410 01",
            },
            Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = (await response.Body()).Id();

        var worker = await (await client.GetAsync($"{Workers}/{id}", Cancel)).Body();
        Assert.Equal("زهرا کریمی", worker.GetProperty("fullName").GetString());
        Assert.Equal(mobile, worker.GetProperty("mobileNumber").GetString());
        Assert.Equal(["برش", "دوخت"], worker.StageNames());
        Assert.Equal("2021-03-21", worker.GetProperty("startDate").GetString());
        Assert.Equal("تاریخ شروع تقریبی است", worker.GetProperty("notes").GetString());
        Assert.Equal("IR** **** **** **** **** **10 01", worker.GetProperty("maskedIban").GetString());
        Assert.True(worker.GetProperty("isActive").GetBoolean());
        Assert.False(worker.TryGetProperty("iban", out _));
    }

    [Fact]
    public async Task A_worker_with_no_stage_and_no_iban_is_added()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var worker = await client.CreateWorkerAsync(stageIds: [], iban: "  ");

        Assert.Empty(worker.StageNames());
        Assert.Equal(JsonValueKind.Null, worker.GetProperty("maskedIban").ValueKind);
    }

    [Fact]
    public async Task Saving_the_profile_replaces_the_stages_and_the_start_date_and_never_touches_the_iban()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var cutting = await client.SeededStageAsync("برش");
        var packing = await client.SeededStageAsync("بسته‌بندی");
        var id = (await client.CreateWorkerAsync(iban: Iban, stageIds: [cutting])).Id();
        var newMobile = ApiFactory.NextMobile();

        var response = await client.PutAsJsonAsync(
            $"{Workers}/{id}",
            new { fullName = "مریم احمدی", mobileNumber = newMobile, stageIds = new[] { packing }, startDate = "2019-09-23", notes = (string?)null },
            Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var worker = await (await client.GetAsync($"{Workers}/{id}", Cancel)).Body();
        Assert.Equal("مریم احمدی", worker.GetProperty("fullName").GetString());
        Assert.Equal(newMobile, worker.GetProperty("mobileNumber").GetString());
        Assert.Equal(["بسته‌بندی"], worker.StageNames());
        Assert.Equal("2019-09-23", worker.GetProperty("startDate").GetString());
        Assert.Equal(JsonValueKind.Null, worker.GetProperty("notes").ValueKind);
        Assert.Equal("IR** **** **** **** **** **10 01", worker.GetProperty("maskedIban").GetString());
    }

    [Fact]
    public async Task A_missing_start_date_is_a_validation_error_on_the_start_date_field()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync(
            Workers, new { fullName = "زهرا", mobileNumber = ApiFactory.NextMobile(), stageIds = Array.Empty<Guid>() }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("startDate", error.GetProperty("field").GetString());
        Assert.Equal("validation.required", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_start_date_in_the_future_is_refused_on_adding_and_on_saving()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var worker = await client.CreateWorkerAsync();

        var added = await client.PostAsJsonAsync(
            Workers, new { fullName = "زهرا", mobileNumber = ApiFactory.NextMobile(), startDate = "2099-01-01" }, Cancel);
        var saved = await client.PutAsJsonAsync(
            $"{Workers}/{worker.Id()}",
            new { fullName = "زهرا", mobileNumber = worker.GetProperty("mobileNumber").GetString(), startDate = "2099-01-01" },
            Cancel);

        foreach (var response in new[] { added, saved })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
            Assert.Equal("startDate", error.GetProperty("field").GetString());
            Assert.Equal("parties.start_date_in_future", error.GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task The_iban_is_changed_and_removed_on_its_own()
    {
        using var client = await factory.CreateClientAsAsync(Role.Manager);
        var id = (await client.CreateWorkerAsync(iban: Iban)).Id();

        var changed = await client.PutAsJsonAsync($"{Workers}/{id}/iban", new { iban = OtherIban }, Cancel);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal("IR** **** **** **** **** **00 01", (await changed.Body()).GetProperty("maskedIban").GetString());

        var removed = await client.DeleteAsync($"{Workers}/{id}/iban", Cancel);
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await removed.Body()).GetProperty("maskedIban").ValueKind);
    }

    [Fact]
    public async Task A_wrong_iban_is_a_validation_error_on_the_iban_field()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateWorkerAsync()).Id();

        var response = await client.PutAsJsonAsync($"{Workers}/{id}/iban", new { iban = "IR710570029971601460641002" }, Cancel);

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
        var id = (await operatorClient.CreateWorkerAsync(iban: Iban)).Id();
        var withoutIban = (await operatorClient.CreateWorkerAsync()).Id();

        var byOwner = await ownerClient.GetAsync($"{Workers}/{id}/iban", Cancel);
        var byManager = await managerClient.GetAsync($"{Workers}/{id}/iban", Cancel);
        var byOperator = await operatorClient.GetAsync($"{Workers}/{id}/iban", Cancel);
        var none = await ownerClient.GetAsync($"{Workers}/{withoutIban}/iban", Cancel);

        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        Assert.Equal(Iban, (await byOwner.Body()).GetProperty("iban").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, byManager.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byOperator.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        Assert.Equal("parties.iban_not_set", await none.ErrorCode());
    }

    [Fact]
    public async Task A_mobile_belongs_to_one_worker_but_may_also_be_a_contractors()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var mobile = ApiFactory.NextMobile();
        await client.CreateContractorAsync(mobileNumber: mobile);
        var worker = await client.CreateWorkerAsync(mobileNumber: mobile);
        var persianForm = string.Concat(mobile.Select(c => (char)('۰' + (c - '0'))));
        var other = (await client.CreateWorkerAsync()).Id();

        var added = await client.PostAsJsonAsync(
            Workers, new { fullName = "زهرا", mobileNumber = persianForm, startDate = StartDate }, Cancel);
        var saved = await client.PutAsJsonAsync(
            $"{Workers}/{other}", new { fullName = "زهرا", mobileNumber = mobile, startDate = StartDate }, Cancel);
        var keptOwn = await client.PutAsJsonAsync(
            $"{Workers}/{worker.Id()}", new { fullName = "زهرا", mobileNumber = mobile, startDate = StartDate }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, added.StatusCode);
        Assert.Equal("parties.mobile_taken", await added.ErrorCode());
        Assert.Equal(HttpStatusCode.Conflict, saved.StatusCode);
        Assert.Equal("parties.mobile_taken", await saved.ErrorCode());
        Assert.Equal(HttpStatusCode.OK, keptOwn.StatusCode);
    }

    [Fact]
    public async Task Two_workers_may_share_a_name()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var name = CatalogApi.UniqueName("فاطمه رحیمی");

        var first = await client.CreateWorkerAsync(fullName: name);
        var second = await client.CreateWorkerAsync(fullName: name);

        Assert.NotEqual(first.Id(), second.Id());
    }

    [Fact]
    public async Task A_stage_that_does_not_exist_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync(
            Workers,
            new { fullName = "زهرا", mobileNumber = ApiFactory.NextMobile(), stageIds = new[] { Guid.CreateVersion7() }, startDate = StartDate },
            Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("parties.stage_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task A_worker_is_deactivated_and_activated_again()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateWorkerAsync()).Id();

        var deactivated = await client.PostAsync($"{Workers}/{id}/deactivate", null, Cancel);
        Assert.False((await deactivated.Body()).GetProperty("isActive").GetBoolean());

        var activated = await client.PostAsync($"{Workers}/{id}/activate", null, Cancel);
        Assert.True((await activated.Body()).GetProperty("isActive").GetBoolean());
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("GET", "/iban")]
    [InlineData("PUT", "/iban")]
    [InlineData("DELETE", "/iban")]
    [InlineData("POST", "/activate")]
    public async Task A_worker_that_does_not_exist_is_not_found(string method, string path)
    {
        using var client = await factory.CreateClientAsAsync(Role.Owner);
        var request = new HttpRequestMessage(new HttpMethod(method), $"{Workers}/{Guid.CreateVersion7()}{path}");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new
            {
                fullName = "زهرا",
                mobileNumber = ApiFactory.NextMobile(),
                startDate = StartDate,
                iban = Iban,
            });
        }

        var response = await client.SendAsync(request, Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("parties.worker_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_read_the_workers()
    {
        using var client = factory.CreateBrowserClient();

        var response = await client.GetAsync(Workers, Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
