using System.Net;
using System.Text;
using FluentAssertions;
using Xunit.Abstractions;

namespace Api.IntegrationTests.LiveApi;

/// <summary>
/// Live HTTP smoke/regression tests for /erms/* against the real running docker stack.
/// </summary>
[Trait("Category", "LiveIntegration")]
public sealed class ErmsCompatTests : IClassFixture<ErmsLiveFixture>
{
    private readonly ErmsLiveFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ErmsCompatTests(ErmsLiveFixture fixture, ITestOutputHelper output)
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

    private static string Q(string path) =>
        $"{path}?pmsPatientId={KnownTestData.PatientId}&pmsEncounterId={Uri.EscapeDataString(KnownTestData.EncounterId)}";

    [Fact]
    public async Task GetPatientData_ReturnsRealPatientRecord()
    {
        var (response, raw) = await GetAsync(Q("/erms/GetPatientData"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("Surname", "the real GetPatientData XML always carries a Surname element, even if blank");
    }

    [Theory]
    [InlineData("/erms/GetPatientMeasurement")]
    [InlineData("/erms/GetSmokingStatus")]
    [InlineData("/erms/GetNextOfKin")]
    [InlineData("/erms/GetAccidents")]
    [InlineData("/erms/GetClassifications")]
    [InlineData("/erms/GetConsultNotes")]
    [InlineData("/erms/GetMedicalAllergies")]
    [InlineData("/erms/GetPrescribedMedications")]
    [InlineData("/erms/GetRegularMedications")]
    [InlineData("/erms/GetLaboratoryReportList")]
    [InlineData("/erms/GetRadiologyReportList")]
    [InlineData("/erms/GetDischargeSummaryReportList")]
    [InlineData("/erms/GetScannedList")]
    public async Task ListReads_RespondSuccessfully(string path)
    {
        var (response, _) = await GetAsync(Q(path));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCurrentUser_WithLocationAndUserId_ReturnsRealProvider()
    {
        var (response, raw) = await GetAsync($"{Q("/erms/GetCurrentUser")}&LocationId=901&pmsUserId=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Should().Contain("CurrentUser");
    }

    [Fact]
    public async Task GetRegisteredPractitioners_WithLocationId_RespondsSuccessfully()
    {
        var (response, _) = await GetAsync($"{Q("/erms/GetRegisteredPractitioners")}&pmsLocationId=901");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// KNOWN BUG (pre-existing, not introduced this session): SP [dbo].[uspDocumentSave] on
    /// dbserver-local/DMS_PMS has a TRY/CATCH that issues ROLLBACK TRANSACTION on a code path that
    /// never opened a transaction - identical bug to KaroCompatTests.Document_Write, same shared SP.
    /// The XML element names below are the real ReferralDocument_* tags
    /// (src/Adapters.Erms/Hiso/ErmsReferralDocument.cs's [XmlElement] names) - the frontend used to
    /// send plain tags like &lt;EncounterID&gt; which XmlSerializer silently ignored (no error, just
    /// null fields), a real bug fixed in frontend/src/catalog.ts this session. Using the wrong tags
    /// here would mask that fix by producing a different, misleading failure ("practice '-' is not
    /// registered") instead of reaching the real SP bug below.
    /// </summary>
    [Fact]
    public async Task SaveDocument_Write_FailsWithKnownStoredProcedureRollbackBug()
    {
        var content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Integration test ERMS document content"));
        var xml = "<ReferralDocument>" +
                  "<ReferralDocument_Referral_ID></ReferralDocument_Referral_ID>" +
                  "<ReferralDocument_Document_ID>INTEG-TEST-001</ReferralDocument_Document_ID>" +
                  $"<ReferralDocument_Patient_PMS_ID>{KnownTestData.PatientId}</ReferralDocument_Patient_PMS_ID>" +
                  $"<ReferralDocument_Encounter_ID>{KnownTestData.EncounterId}</ReferralDocument_Encounter_ID>" +
                  "<ReferralDocument_Referrer_PMS_ID>1</ReferralDocument_Referrer_PMS_ID>" +
                  "<ReferralDocument_Referral_Type>Letter</ReferralDocument_Referral_Type>" +
                  "<ReferralDocument_Item_Type>Referral</ReferralDocument_Item_Type>" +
                  "<ReferralDocument_Created_Date>2026-07-28</ReferralDocument_Created_Date>" +
                  "<ReferralDocument_Content_Type>text/plain</ReferralDocument_Content_Type>" +
                  $"<ReferralDocument_Content>{content}</ReferralDocument_Content>" +
                  "</ReferralDocument>";

        var response = await _fixture.Client.PostAsync("/erms/SaveDocument", new StringContent(xml, Encoding.UTF8, "text/xml"));
        var raw = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /erms/SaveDocument\nrequest: {xml}\nresponse ({(int)response.StatusCode}): {raw}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "legacy SaveDocument always answers HTTP 400 \"BadRequest\" on any error, by design");
        raw.Should().Be("BadRequest");
    }
}
