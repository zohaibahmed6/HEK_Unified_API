namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>ERMS's real connection routing: `"ConnIndiciDB" + practiceSuffix`, same model as `IKaroPracticeConnectionResolver` but kept separate per Zohaib's isolation requirement.</summary>
public interface IErmsPracticeConnectionResolver
{
    Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default);
}
