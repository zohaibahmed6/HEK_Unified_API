namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Ported from ERMS's real `Authenticate` -> `HSSDA.InsertAndValidateToken(...expiryInDays...)`, same `[HSS].[uspInsertAndValidateToken]` proc KARO uses, but with the real `expiryInDays` param ERMS passes.</summary>
public interface IErmsAuthRepository
{
    Task<KaroAuthResult?> InsertAndValidateTokenAsync(string practiceSuffix, string? username, string? password, string? patientId, string? encounterId, string? token = null, double expiryInDays = 0, string? pho = null, CancellationToken ct = default);
}
