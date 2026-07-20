namespace HekCoreApi.Contracts.Demographics;

/// <summary>COL/Pegasus PegasusAPIModel.PatientData shape (OpenAPI: DemographicsCol). Kept separate, not merged.</summary>
public sealed record DemographicsCol(int PatientId, string PracticeId, string FirstName, string LastName);
