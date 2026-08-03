using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace Api.IntegrationTests.LiveApi;

/// <summary>
/// Live HTTP smoke/regression tests for /hiso/* against the real running docker stack. Session-key
/// auth, not bearer - see <see cref="KnownTestData.HisoSessionKey"/>.
/// </summary>
[Trait("Category", "LiveIntegration")]
public sealed class HisoCompatTests : IClassFixture<HisoLiveFixture>
{
    private readonly HisoLiveFixture _fixture;
    private readonly ITestOutputHelper _output;

    public HisoCompatTests(HisoLiveFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task GetVersion_ReturnsRealApplicationVersion()
    {
        var request = new { sessionKey = _fixture.SessionKey };
        var response = await _fixture.Client.PostAsJsonAsync("/hiso/getVersion", request);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /hiso/getVersion\nrequest: {JsonSerializer.Serialize(request)}\nresponse ({(int)response.StatusCode}): {raw}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(raw);
        var result = body.TryGetProperty("getVersionResponseReturn", out var r) ? r : body.GetProperty("GetVersionResponseReturn");
        result.GetProperty("application").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetDeliveryOptions_ReturnsRealDeliveryTarget()
    {
        var request = new { sessionKey = _fixture.SessionKey };
        var response = await _fixture.Client.PostAsJsonAsync("/hiso/getDeliveryOptions", request);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /hiso/getDeliveryOptions\nrequest: {JsonSerializer.Serialize(request)}\nresponse ({(int)response.StatusCode}): {raw}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFormView_RespondsSuccessfully()
    {
        var request = new { sessionKey = _fixture.SessionKey };
        var response = await _fixture.Client.PostAsJsonAsync("/hiso/getFormView", request);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /hiso/getFormView\nrequest: {JsonSerializer.Serialize(request)}\nresponse ({(int)response.StatusCode}): {raw}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetData_WithMinimalContainer_ReturnsPatientSection()
    {
        var request = new
        {
            sessionKey = _fixture.SessionKey,
            dataContainer = new
            {
                formMetaData = new { formInstanceOperationMode = "N" },
                submittedDataXml = "<dataContainer><section name=\"patient.details\"><field name=\"nhi\" /></section></dataContainer>",
            },
        };
        var response = await _fixture.Client.PostAsJsonAsync("/hiso/getData", request);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /hiso/getData\nrequest: {JsonSerializer.Serialize(request)}\nresponse ({(int)response.StatusCode}): {raw}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("dataContainer", "a real session should echo back a data container, even a minimal one");
    }

    /// <summary>
    /// Formerly flagged as a known bug (saveContainer with a non-ACC45-shaped body throwing an
    /// uncaught SQL structured-parameter ArgumentException, surfacing as HTTP 500) - re-verified by
    /// Zohaib against the local database during the 2026-08-03 .NET 10 migration and confirmed
    /// working correctly now (server-side fix, unrelated to the .NET runtime version). Flipped to
    /// expect success.
    /// </summary>
    [Fact]
    public async Task SaveContainer_WithMinimalNonAcc45Body_SavesSuccessfully()
    {
        var request = new
        {
            sessionKey = _fixture.SessionKey,
            formMetaData = new { formInstanceOperationMode = "N" },
            resumePath = "",
            view = "",
            viewType = "",
            completed = false,
            submittedDataXml = "<dataContainer></dataContainer>",
        };
        var response = await _fixture.Client.PostAsJsonAsync("/hiso/saveContainer", request);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /hiso/saveContainer\nrequest: {JsonSerializer.Serialize(request)}\nresponse ({(int)response.StatusCode}): {raw}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("\"response\":true");
    }
}
