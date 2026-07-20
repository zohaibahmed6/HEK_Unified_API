namespace HekCoreApi.Contracts.Demographics;

/// <summary>KARO's DemographicInfo shape (OpenAPI: DemographicsKaro, per KARO_HSS_doc.md sample). Kept separate, not merged.</summary>
public sealed record DemographicsKaro(
    int PatientId,
    string PracticeId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    DateOnly? DateOfEnrolment,
    DateOnly? EndEnrolmentDate);
