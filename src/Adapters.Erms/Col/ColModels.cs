namespace HekCoreApi.Adapters.Erms.Col;

// Exact ports of COL/Pegasus's JSON models (`ERMSWebAPI/Pegasus_Models/PegasusAPIModel.cs`).
// Property names are deliberately lowercase - they are both the legacy DataTable column names the
// row mapper matches on AND the exact JSON property names Newtonsoft emitted (default settings, no
// camel-casing). `DataTableToList`'s Convert.ChangeType port relies on the property types too
// (e.g. `dob` DateTime, `id`/`iscscholder` int).

#pragma warning disable IDE1006 // lowercase names are the legacy wire contract
public class ColPatientData
{
    public int id { get; set; }
    public string? lastname { get; set; }
    public string? firstname { get; set; }
    public string? title { get; set; }
    public string? homestreet { get; set; }
    public string? homesuburb { get; set; }
    public string? homecity { get; set; }
    public string? homepostcode { get; set; }
    public string? postalstreet { get; set; }
    public string? postalsuburb { get; set; }
    public string? postalcity { get; set; }
    public string? postalpostcode { get; set; }
    public string? dayphone { get; set; }
    public string? ahphone { get; set; }
    public string? cellphone { get; set; }
    public DateTime dob { get; set; }
    public string? nhinumber { get; set; }
    public string? gmscode { get; set; }
    public string? csccardnnumber { get; set; }
    public string? cscstartdate { get; set; }
    public string? cscexpiredate { get; set; }
    public int iscscholder { get; set; }
    public string? huccode { get; set; }
    public string? huccardnnumber { get; set; }
    public string? hucstartdate { get; set; }
    public string? hucexpiresdate { get; set; }
    public int ishucholder { get; set; }
    public string? gendercode { get; set; }
    public string? ethnicitycode1 { get; set; }
    public string? ethnicitycodedesc1 { get; set; }
    public string? ethnicitycode2 { get; set; }
    public string? ethnicitycode2desc { get; set; }
    public string? ethnicitycode3 { get; set; }
    public string? ethnicitycode3desc { get; set; }
    public string? ethnicitycode4 { get; set; }
    public string? ethnicitycode4desc { get; set; }
    public string? ethnicitycode5 { get; set; }
    public string? ethnicitycode5desc { get; set; }
    public string? ethnicitycode6 { get; set; }
    public string? ethnicitycode6desc { get; set; }
    public string? quintile { get; set; }
    public string? genderdesc { get; set; }
    public string? email { get; set; }
    public string? enrolmentcode { get; set; }
    public string? nokname { get; set; }
    public string? nokresidence { get; set; }
    public string? nokstreet { get; set; }
    public string? noksuburb { get; set; }
    public string? nokcity { get; set; }
    public string? nokphone { get; set; }
    public string? nokahphone { get; set; }
    public string? nokrelationship { get; set; }
    public string? isenrolled { get; set; }
    public string? iscapitated { get; set; }
}

public class ColSessionData
{
    public string? computername { get; set; }
    public string? hostapplicationversion { get; set; }
    public string? hostapplicationuser { get; set; }
    public string? osversion { get; set; }
}

public class ColProviderData
{
    public string? providername { get; set; }
    public string? providercode { get; set; }
    public string? registrationnumber { get; set; }
    public string? providerinternalname { get; set; }
    public string? providerrolecode { get; set; }
    public string? providerhpicpn { get; set; }
}

public class ColDiagnosisData
{
    public string? diagnosisnote { get; set; }
    public string? diagnosisclassstatus { get; set; }
    public DateTime diagnosisdateonsetdatetime { get; set; }
    public DateTime diagnosiswhenclassdatetime { get; set; }
    public string? diagnosiscode { get; set; }
    public string? diagnosiscodingsystem { get; set; }
}

public class ColSurgeryData
{
    public string? locationcode { get; set; }
    public string? surgeryname { get; set; }
    public string? locationstreet { get; set; }
    public string? locationsuburb { get; set; }
    public string? locationcity { get; set; }
    public string? locationpostcode { get; set; }
    public string? postalstreet { get; set; }
    public string? postalsuburb { get; set; }
    public string? postalcity { get; set; }
    public string? postalpostcode { get; set; }
    public string? dayphone { get; set; }
    public string? afterhoursphone { get; set; }
    public string? faxphone { get; set; }
    public string? hpifacilitynumber { get; set; }
}
#pragma warning restore IDE1006

/// <summary>Legacy `PegasusAPIModel.UserAuthenticate` - the JSON Authenticate response (always HTTP 200; `error` carries the failure text). Lowercase `error` is the real legacy casing.</summary>
public class ColUserAuthenticate
{
    public string? Token { get; set; }
    public string? Expiry { get; set; }
    public string? PracticeId { get; set; }
#pragma warning disable IDE1006
    public string? error { get; set; }
#pragma warning restore IDE1006
}

/// <summary>Legacy `SaveInvoice` request body (`COLController.cs:465`) - JSON, PascalCase.</summary>
public class ColSaveInvoiceRequest
{
    public string? ServiceCode { get; set; }
    public string? ServiceName { get; set; }
    public string? AmountInclGST { get; set; }
    public string? Description { get; set; }
    public string? AccountHolderID { get; set; }
    public string? Payee { get; set; }
    public string? ServiceProvider { get; set; }
    public string? ServiceProviderType { get; set; }
    public string? ServiceDate { get; set; }
    public string? PegasusReference { get; set; }
    public string? EncounterID { get; set; }
    public string? PatientID { get; set; }
    public string? Userid { get; set; }
    public string? ClaimShortCode { get; set; }
}
