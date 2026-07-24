using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>Ported from `COLController.cs`'s inline split/`GetDcrptValue` logic - see <see cref="IColRequestParser"/> for the quirks vs ERMS.</summary>
public sealed class ColRequestParser : IColRequestParser
{
    private readonly IKaroEncryptionService _encryption;

    public ColRequestParser(IKaroEncryptionService encryption)
    {
        _encryption = encryption;
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
        if (long.TryParse(value, out _))
        {
            return value;
        }

        // Legacy GetDcrptValue: returns "" on any decrypt failure.
        try
        {
            return _encryption.Decrypt(value);
        }
        catch
        {
            return string.Empty;
        }
    }

    public (string? EncounterId, string PracticeSuffix) ParseEncounterId(string? encounterId)
    {
        var practiceSuffix = string.Empty;

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
                }

                if (splitEncounter.Length > 2)
                {
                    // Legacy quirk: third segment OVERWRITES the suffix (ERMS appends here).
                    practiceSuffix = "_" + splitEncounter[2];
                }
            }
            else
            {
                encounterId = Decrypt(encounterId);
            }
        }

        return (encounterId, practiceSuffix);
    }
}
