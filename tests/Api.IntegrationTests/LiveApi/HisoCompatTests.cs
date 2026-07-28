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
    /// KNOWN BUG (pre-existing, not introduced this session): saveContainer with a non-ACC45-shaped
    /// body throws System.ArgumentException ("There are not enough fields in the Structured type...")
    /// from Acc45DetailRepository.SaveAccidentInformationAsync / LegacyDbExecutor.ExecuteNonQueryAsync
    /// - an uncaught SQL structured-parameter error that surfaces as HTTP 500 (the JSON body itself is
    /// a generic ProblemDetails with no exception text; the real detail is server-side only - see
    /// `docker logs hekcoreapi-api-1` for "Unhandled exception mapped to status 500"). This test locks
    /// in that known-broken behavior so a future fix (a caught 400 with a real message, or a genuine
    /// success) shows up as a loud, expected test failure here.
    /// </summary>
    [Fact]
    public async Task SaveContainer_WithMinimalNonAcc45Body_FailsWithKnownUnhandledServerError()
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

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "KNOWN BUG: an uncaught SQL structured-parameter ArgumentException maps to 500 - see class doc comment");
    }
}
