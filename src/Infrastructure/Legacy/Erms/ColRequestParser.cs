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

        if (int.TryParse(value, out _))
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
