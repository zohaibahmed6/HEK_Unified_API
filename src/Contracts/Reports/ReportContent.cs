namespace HekCoreApi.Contracts.Reports;

/// <summary>Replaces ERMS's RTF+Base64 transcoding (ERMS-BR-13) with a documented modern format per FR-LAB-01.</summary>
public sealed record ReportContent(string ReportId, string Content, string ContentEncoding);
