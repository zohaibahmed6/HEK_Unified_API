namespace HekCoreApi.Contracts.Demographics;

/// <summary>ERMS APIController's PatientData shape (OpenAPI: DemographicsErms, JSON-translated from legacy XML). Kept separate, not merged.</summary>
public sealed record DemographicsErms(int PatientId, int EncounterId, string FirstName, string LastName, DateOnly Dob, string? Nhi);
