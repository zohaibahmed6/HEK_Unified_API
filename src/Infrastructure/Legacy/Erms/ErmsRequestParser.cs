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

        if (int.TryParse(value, out _))
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

        return int.TryParse(value, out _) ? value : _encryption.Decrypt(value);
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
