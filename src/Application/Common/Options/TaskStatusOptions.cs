namespace HekCoreApi.Application.Common.Options;

/// <summary>HISO-BR-20: task status set via configured status IDs, not hardcoded values.</summary>
public sealed class TaskStatusOptions
{
    public const string SectionName = "TaskStatus";

    public string ActiveStatusId { get; set; } = "1";
    public string CompletedStatusId { get; set; } = "2";
}
