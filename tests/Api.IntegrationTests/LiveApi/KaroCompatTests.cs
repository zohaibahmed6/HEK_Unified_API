using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace Api.IntegrationTests.LiveApi;

/// <summary>
/// Live HTTP smoke/regression tests for /karo/* against the real running docker stack. Reuses the
/// exact corrected payload shapes (string, not number, for userId/fee/isLongTerm/temperature/etc)
/// discovered and fixed in frontend/src/catalog.ts this session - the backend DTOs
/// (HekCoreApi.Adapters.Karo.Karo*Request) are all-string records.
/// </summary>
[Trait("Category", "LiveIntegration")]
public sealed class KaroCompatTests : IClassFixture<KaroLiveFixture>
{
    private readonly KaroLiveFixture _fixture;
    private readonly ITestOutputHelper _output;

    public KaroCompatTests(KaroLiveFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private async Task<(HttpResponseMessage response, string raw)> GetAsync(string path)
    {
        var response = await _fixture.Client.GetAsync(path);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"GET {path}\nresponse ({(int)response.StatusCode}): {raw}");
        return (response, raw);
    }

    private async Task<(HttpResponseMessage response, string raw)> PostAsync(string path, object body)
    {
        var response = await _fixture.Client.PostAsJsonAsync(path, body);
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"POST {path}\nrequest: {JsonSerializer.Serialize(body)}\nresponse ({(int)response.StatusCode}): {raw}");
        return (response, raw);
    }

    private static string Q(string path) =>
        $"{path}?patientId={KnownTestData.PatientId}&encounterId={Uri.EscapeDataString(KnownTestData.EncounterId)}";

    [Fact]
    public async Task Demographics_ReturnsRealPatientRecord()
    {
        var (response, raw) = await GetAsync(Q("/karo/demographics"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("nhi", "a real demographics record always has an NHI field, even if blank");
    }

    [Theory]
    [InlineData("/karo/clinicalnotes")]
    [InlineData("/karo/conditions")]
    [InlineData("/karo/labresults")]
    [InlineData("/karo/medications")]
    [InlineData("/karo/recalls")]
    [InlineData("/karo/screeningcodes")]
    public async Task ListReads_RespondSuccessfully(string path)
    {
        var (response, _) = await GetAsync(Q(path));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Documents_WithNoIdentifierFilter_ReturnsRealDocumentList()
    {
        var (response, raw) = await GetAsync(Q("/karo/documents"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("identifier", "the real document list rows carry an identifier used for patientattachment drilldown");
    }

    [Fact]
    public async Task Provider_WithUserId1_ReturnsRealProviderRecord()
    {
        var (response, raw) = await GetAsync($"{Q("/karo/provider")}&userId=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("Provider", "the real envelope's resourceType is literally \"Provider\"");
    }

    [Fact]
    public async Task ClinicalNotes_Write_SavesSuccessfully()
    {
        var (response, raw) = await PostAsync("/karo/clinicalnotes", new
        {
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
            userId = "1",
            subjectiveNotes = "Automated integration test - subjective note",
            objectiveNotes = "BP 120/80, HR 72, afebrile",
            assessment = "Stable, no acute concerns (integration test)",
            plans = "Follow up in 2 weeks (integration test)",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("\"status\":\"success\"");
    }

    [Fact]
    public async Task Conditions_Write_SavesSuccessfully()
    {
        var (response, raw) = await PostAsync("/karo/conditions", new
        {
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
            userId = "1",
            type = "disorder",
            onSetDate = "2026-07-27",
            summary = "Integration test condition entry",
            isLongTerm = "false",
            conceptId = "44054006",
            name = "Type 2 diabetes mellitus",
            fsn = "Type 2 diabetes mellitus (disorder)",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("\"status\":\"success\"");
    }

    /// <summary>
    /// Formerly flagged as a known bug ([HSS].[uspInsertUpdateService] on dbserver-local/PMS_NZ_V2
    /// returning "too many arguments specified") - re-verified by Zohaib against the local database
    /// during the 2026-08-03 .NET 10 migration and confirmed working correctly now (SP signature/DB
    /// state resolved server-side, unrelated to the .NET runtime version). Flipped to expect success.
    /// </summary>
    [Fact]
    public async Task Invoice_Write_SavesSuccessfully()
    {
        var uniqueCode = $"GP-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var (response, raw) = await PostAsync("/karo/invoice", new
        {
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
            userId = "1",
            name = "GP Standard Consultation",
            code = uniqueCode,
            fee = "55.50",
            payee = "Patient",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("\"status\":\"success\"");
    }

    [Fact]
    public async Task Observations_Write_SavesSuccessfully()
    {
        var (response, raw) = await PostAsync("/karo/observations", new
        {
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
            userId = "1",
            temperature = "36.8",
            waistCircumference = "85",
            height = "170",
            weight = "68",
            bpSys = "120",
            bpDia = "80",
            heartRate = "72",
            notes = "Integration test vitals entry",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("\"status\":\"success\"");
    }

    /// <summary>
    /// KNOWN LIMITATION (not a code bug): SP [HSS].[uspInsertUpdateRecall] on dbserver-local/PMS_NZ_V2
    /// silently returns 0 (no exception) when categoryId doesn't match a real seeded
    /// RecallCategory row - and this test practice (901) has none seeded for "Immunisation"/"Recall".
    /// Locks in the current failure so a future test-data seed shows up as a loud pass, not a silent gap.
    /// </summary>
    [Fact]
    public async Task Recalls_Write_FailsWithKnownMissingCategorySeedData()
    {
        var (response, raw) = await PostAsync("/karo/recalls", new
        {
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
            userId = "1",
            priority = "Normal",
            group = "Immunisation",
            dueDate = "2026-09-01",
            notes = "Integration test recall entry",
            categoryId = "1",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("Unable to Save Recall", "KNOWN LIMITATION: no valid RecallCategory seeded for practice 901 in this environment - see class doc comment");
    }

    /// <summary>
    /// Formerly flagged as a known bug ([dbo].[uspDocumentSave] on dbserver-local/DMS_PMS issuing a
    /// stray ROLLBACK TRANSACTION) - re-verified by Zohaib against the local database during the
    /// 2026-08-03 .NET 10 migration and confirmed working correctly now (SP fixed server-side,
    /// unrelated to the .NET runtime version). Flipped to expect success.
    /// </summary>
    [Fact]
    public async Task Document_Write_SavesSuccessfully()
    {
        var content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Integration test document content"));
        var (response, raw) = await PostAsync("/karo/document", new
        {
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
            messageData = content,
            contentType = "text/plain",
            messageSubject = "Integration Test Document",
            itemType = "Letter",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("\"status\":\"success\"");
    }
}
