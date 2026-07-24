using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>Ported from `APIController.cs`'s shared encounterId/practiceid parsing block (identical across every real operation).</summary>
public sealed class KaroRequestParser : IKaroRequestParser
{
    private readonly IKaroEncryptionService _encryption;

    public KaroRequestParser(IKaroEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public (string? EncounterId, string PracticeSuffix, string PracticeSuffixNumeric) ParseEncounterId(string? encounterId)
    {
        var practiceSuffix = string.Empty;
        var practiceSuffixNumeric = string.Empty;

        if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains('_'))
        {
            var splitEncounter = encounterId.Split(new[] { "__" }, StringSplitOptions.None);
            if (splitEncounter.Length == 1)
            {
                splitEncounter = encounterId.Split('_');
            }

            if (splitEncounter.Length > 0)
            {
                encounterId = Decrypt(splitEncounter[0]);
                if (splitEncounter.Length > 1)
                {
                    practiceSuffix = "_" + splitEncounter[1];
                    practiceSuffixNumeric = splitEncounter[1];
                }

                if (splitEncounter.Length > 2)
                {
                    practiceSuffix += "_" + splitEncounter[2];
                }

                if (splitEncounter.Length > 3)
                {
                    practiceSuffix = "_" + splitEncounter[3];
                }
            }
        }

        return (encounterId, practiceSuffix, practiceSuffixNumeric);
    }

    public string? Decrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Real Indici IDs (patientId/appointmentId) are `long`, not `int` - a plain int.TryParse
        // silently overflows on real values above Int32.MaxValue (2,147,483,647), wrongly falling
        // through to Decrypt() and returning an empty string. Confirmed real bug (2026-07-24), not a
        // legacy quirk to preserve - fixed per Zohaib's direction ("in indici appointmentids type is long").
        return long.TryParse(value, out _) ? value : _encryption.Decrypt(value);
    }
}
