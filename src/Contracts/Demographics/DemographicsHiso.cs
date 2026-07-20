namespace HekCoreApi.Contracts.Demographics;

/// <summary>HISO's concept-driven demographics shape (OpenAPI: DemographicsHiso). Kept separate, not merged - Contract Design doc Section 4.2/8 Decision 6.</summary>
public sealed record DemographicsHiso(int PatientId, string PracticeId, string FirstName, string LastName, DateOnly DateOfBirth);
