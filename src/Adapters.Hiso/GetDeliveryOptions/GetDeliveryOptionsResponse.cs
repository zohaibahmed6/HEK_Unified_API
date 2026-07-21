namespace HekCoreApi.Adapters.Hiso.GetDeliveryOptions;

/// <summary>
/// Legacy `getDeliveryOptions` (`FormSessionService.svc.cs`): config-sourced, not DB-sourced.
/// `messageID`/`recipientAccount` are real response fields but never populated in legacy (planned,
/// never wired) - included as always-null for shape fidelity.
/// </summary>
public sealed record GetDeliveryOptionsResponse(GetDeliveryOptionsResponseReturn GetDeliveryOptionsResponseReturn)
{
    public static GetDeliveryOptionsResponse From(string? url, string? senderAccount, string? senderPassword) =>
        new(new GetDeliveryOptionsResponseReturn(url, null, null, senderAccount, senderPassword));
}

public sealed record GetDeliveryOptionsResponseReturn(string? URL, string? MessageID, string? RecipientAccount, string? SenderAccount, string? SenderPassword);
