namespace HekCoreApi.Contracts.Observations;

/// <summary>At least one field must be non-empty (KARO-BR-14) - enforced at the API layer, not just documented.</summary>
public sealed record ObservationInput(
    double? Height = null,
    double? Weight = null,
    double? Bmi = null,
    double? BloodPressureSystolic = null,
    double? BloodPressureDiastolic = null,
    double? WaistCircumference = null,
    string? SmokingStatus = null,
    double? HeartRate = null,
    double? Temperature = null)
{
    public bool HasAnyValue() =>
        Height is not null || Weight is not null || Bmi is not null || BloodPressureSystolic is not null ||
        BloodPressureDiastolic is not null || WaistCircumference is not null || !string.IsNullOrEmpty(SmokingStatus) ||
        HeartRate is not null || Temperature is not null;
}
