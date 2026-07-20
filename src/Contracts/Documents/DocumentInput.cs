namespace HekCoreApi.Contracts.Documents;

/// <summary>direction and contentType are both first-class fields, not aliases (Contract Design doc Section 8 Decision 2).</summary>
public sealed record DocumentInput(string Direction, string ContentType, string? Subject, string? ReferenceId, string Content);
