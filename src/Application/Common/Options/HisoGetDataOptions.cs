namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// Legacy `getData` appSettings, confirmed from legacy-reference/Hiso/FormSessionService.svc.cs and
/// its Web.config: `IsDynamic` (real deployment value `"1"`) gates whether `getData` does any real
/// work at all - when off, legacy returns an empty stub, which this project reproduces exactly rather
/// than inventing new functionality nobody has ever had. `addDMSRef` (real deployment value `"1"`)
/// stamps an empty `referenceID` attribute onto `clinical.diagnosticReport`/`scannedDocument` group
/// nodes before concept resolution.
/// </summary>
public sealed class HisoGetDataOptions
{
    public const string SectionName = "Hiso:GetData";

    public bool IsDynamic { get; set; } = true;

    public bool AddDmsRef { get; set; } = true;
}
