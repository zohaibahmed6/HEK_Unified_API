namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// Legacy: `ConfigurationManager.AppSettings["QualifierList"]`, a comma-separated list of qualifier
/// codes (confirmed from legacy-reference/Hiso/ConceptMapper/HisoConceptDetail.cs - the old hardcoded
/// array is present but commented out in source, config-driven since at least that revision). Drives
/// the "_dateTaken" companion-column logic inside `measurements` sections. Empty by default until
/// Zohaib supplies the real list - not guessed.
/// </summary>
public sealed class HisoConceptMappingOptions
{
    public const string SectionName = "Hiso:ConceptMapping";

    public string[] QualifierList { get; set; } = [];
}
