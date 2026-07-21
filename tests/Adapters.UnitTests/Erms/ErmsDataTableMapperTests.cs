using System.Data;
using HekCoreApi.Adapters.Erms.Hiso;
using Xunit;

namespace Adapters.UnitTests.Erms;

/// <summary>
/// Locks in the exact quirks of the `ERMSDataTableToListHiso&lt;T&gt;` port: `"|&amp;|"` cell split,
/// `"|?|"` inner split, missing/empty column skipping, per-property silent failure, and the legacy
/// null-not-empty-list return on a table-level exception.
/// </summary>
public sealed class ErmsDataTableMapperTests
{
    private static DataTable Table(params (string Column, string? Value)[] cells)
    {
        var table = new DataTable();
        foreach (var (column, _) in cells)
        {
            table.Columns.Add(column, typeof(string));
        }

        var row = table.NewRow();
        foreach (var (column, value) in cells)
        {
            row[column] = (object?)value ?? DBNull.Value;
        }

        table.Rows.Add(row);
        return table;
    }

    [Fact]
    public void ToListHiso_SplitsConceptIdAndText()
    {
        var list = ErmsDataTableMapper.ToListHiso<PatientData>(Table(("Surname", "12345|&|Smith"), ("FirstName", "678|&|Jane")));

        Assert.NotNull(list);
        var patient = Assert.Single(list!);
        Assert.Equal("12345", patient.Surname.ConceptID);
        Assert.Equal("Smith", patient.Surname.Text);
        Assert.Equal("Jane", patient.FirstName.Text);
    }

    [Fact]
    public void ToListHiso_ConceptIdOnly_LeavesTextNull()
    {
        var list = ErmsDataTableMapper.ToListHiso<PatientData>(Table(("PatientNHI", "999")));

        Assert.Equal("999", list![0].PatientNHI.ConceptID);
        Assert.Null(list[0].PatientNHI.Text);
    }

    [Fact]
    public void ToListHiso_EmptyOrMissingColumns_KeepDefaultWrappers()
    {
        var list = ErmsDataTableMapper.ToListHiso<PatientData>(Table(("Surname", ""), ("Unrelated", "x|&|y")));

        var patient = list![0];
        Assert.Null(patient.Surname.ConceptID);
        Assert.Null(patient.Surname.Text);
    }

    [Fact]
    public void ToListHiso_InnerQualifierSplit_TextIsFirstSegment_ExtrasSilentlySkipProperty()
    {
        // PatientData wrappers have no Name/QualifierID properties: legacy NREs per-property and the
        // silent catch leaves the default wrapper instance untouched (ConceptID never applied either).
        var withQualifiers = ErmsDataTableMapper.ToListHiso<PatientData>(Table(("Surname", "1|&|text|?|name|?|qid|?|qname|?|dt")));
        Assert.Null(withQualifiers![0].Surname.ConceptID);
        Assert.Null(withQualifiers[0].Surname.Text);

        // With only the text segment (no name/qualifiers), the inner IndexOutOfRange is swallowed and
        // ConceptID/Text are still set.
        var textOnly = ErmsDataTableMapper.ToListHiso<PatientData>(Table(("Surname", "1|&|text|?|")));
        Assert.Equal("1", textOnly![0].Surname.ConceptID);
        Assert.Equal("text", textOnly[0].Surname.Text);
    }

    [Fact]
    public void ToListHiso_NullTable_ReturnsNull_NotEmptyList()
    {
        Assert.Null(ErmsDataTableMapper.ToListHiso<PatientData>(null!));
    }

    [Fact]
    public void ToListHiso_NonStringColumn_SkipsPropertySilently()
    {
        var table = new DataTable();
        table.Columns.Add("Surname", typeof(int));
        var row = table.NewRow();
        row["Surname"] = 42;
        table.Rows.Add(row);

        var list = ErmsDataTableMapper.ToListHiso<PatientData>(table);

        var patient = Assert.Single(list!);
        Assert.Null(patient.Surname.ConceptID);
    }
}
