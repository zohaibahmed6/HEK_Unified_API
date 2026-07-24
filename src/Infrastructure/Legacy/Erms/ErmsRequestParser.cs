using System.Text;
using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

public sealed class ErmsRequestParser : IErmsRequestParser
{
    private readonly IKaroEncryptionService _encryption;

    public ErmsRequestParser(IKaroEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public string? DecodeBase64(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Real Indici IDs (patientId/appointmentId) are `long`, not `int` - see Decrypt() below for
        // the same fix and full rationale (2026-07-24, confirmed real bug, not a legacy quirk).
        if (long.TryParse(value, out _))
        {
            return value;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch
        {
            return value;
        }
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

    public (string? EncounterId, string PracticeSuffix, string? Pho, string? RawSecondSegment) ParseEncounterId(string? encounterId)
    {
        encounterId = DecodeBase64(encounterId);

        var practiceSuffix = string.Empty;
        string? pho = null;
        string? rawSecondSegment = null;

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
                    rawSecondSegment = splitEncounter[1];
                }

                if (splitEncounter.Length > 2)
                {
                    practiceSuffix += "_" + splitEncounter[2];
                }

                if (splitEncounter.Length > 3)
                {
                    practiceSuffix = "_" + splitEncounter[3];
                    pho = splitEncounter[3];
                }
            }
            else
            {
                encounterId = Decrypt(encounterId);
            }
        }

        return (encounterId, practiceSuffix, pho, rawSecondSegment);
    }
}
