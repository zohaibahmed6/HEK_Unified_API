using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Adapters.Karo.Demographics;

/// <summary>Legacy: `Root&lt;Demographic&gt;("Patient", "hss")` (`APIModels.cs:132`), camelCase JSON via `PrepareJSON`.</summary>
public sealed record KaroRootResponse<T>(string? PatientId, string ResourceType, string System, List<T> Entry);

public sealed record KaroDemographic(List<KaroDemographicInfo> ListDemographicInfo, List<KaroCardInfo> Listcardtype);
