using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ERMSWebAPI.Pegasus_Models
{
    public class PegasusAPIModel
    {
        public class PatientData
        {
            public int id { get; set; }
            [DataMember]
            public string lastname { get; set; }
            [DataMember]
            public string firstname { get; set; }
            [DataMember]
            public string title { get; set; }
            [DataMember]
            public string homestreet { get; set; }
            [DataMember]
            public string homesuburb { get; set; }
            [DataMember]
            public string homecity { get; set; }
            [DataMember]
            public string homepostcode { get; set; }
            [DataMember]
            public string postalstreet { get; set; }
            [DataMember]
            public string postalsuburb { get; set; }
            [DataMember]
            public string postalcity { get; set; }
            [DataMember]
            public string postalpostcode { get; set; }
            [DataMember]
            public string dayphone { get; set; }
            [DataMember]
            public string ahphone { get; set; }
            [DataMember]
            public string cellphone { get; set; }
            [DataMember]
            public DateTime dob { get; set; }
            [DataMember]
            public string nhinumber { get; set; }
            [DataMember]
            public string gmscode { get; set; }
            [DataMember]
            public string csccardnnumber { get; set; }
            [DataMember]
            public string cscstartdate { get; set; }
            [DataMember]
            public string cscexpiredate { get; set; }
            [DataMember]
            public int iscscholder { get; set; }
            [DataMember]
            public string huccode { get; set; }
            [DataMember]
            public string huccardnnumber { get; set; }
            [DataMember]
            public string hucstartdate { get; set; }
            [DataMember]
            public string hucexpiresdate { get; set; }
            [DataMember]
            public int ishucholder { get; set; }
            [DataMember]
            public string gendercode { get; set; }
            [DataMember]
            public string ethnicitycode1 { get; set; }
            [DataMember]
            public string ethnicitycodedesc1 { get; set; }
            [DataMember]
            public string ethnicitycode2 { get; set; }
            [DataMember]
            public string ethnicitycode2desc { get; set; }
            [DataMember]
            public string ethnicitycode3 { get; set; }
            [DataMember]
            public string ethnicitycode3desc { get; set; }
            [DataMember]
            public string ethnicitycode4 { get; set; }
            [DataMember]
            public string ethnicitycode4desc { get; set; }
            [DataMember]
            public string ethnicitycode5 { get; set; }
            [DataMember]
            public string ethnicitycode5desc { get; set; }
            [DataMember]
            public string ethnicitycode6 { get; set; }
            [DataMember]
            public string ethnicitycode6desc { get; set; }
            [DataMember]
            public string quintile { get; set; }
            [DataMember]
            public string genderdesc { get; set; }
            [DataMember]
            public string email { get; set; }
            [DataMember]
            public string enrolmentcode { get; set; }
            [DataMember]
            public string nokname { get; set; }
            [DataMember]
            public string nokresidence { get; set; }
            [DataMember]
            public string nokstreet { get; set; }
            [DataMember]
            public string noksuburb { get; set; }
            [DataMember]
            public string nokcity { get; set; }
            [DataMember]
            public string nokphone { get; set; }
            [DataMember]
            public string nokahphone { get; set; }
            [DataMember]
            public string nokrelationship { get; set; }
            [DataMember]
            public string isenrolled { get; set; }
            [DataMember]
            public string iscapitated { get; set; }
        }

        public class SessionData
        {
            [DataMember]
            public string computername { get; set; }
            [DataMember]
            public string hostapplicationversion { get; set; }
            [DataMember]
            public string hostapplicationuser { get; set; }
            [DataMember]
            public string osversion { get; set; }
        }

        public class ProviderData
        {
            [DataMember]
            public string providername { get; set; }
            [DataMember]
            public string providercode { get; set; }
            [DataMember]
            public string registrationnumber { get; set; }
            [DataMember]
            public string providerinternalname { get; set; }
            [DataMember]
            public string providerrolecode { get; set; }
            [DataMember]
            public string providerhpicpn { get; set; }
        }
        public class DiagnosisData
        {
            [DataMember]
            public string diagnosisnote { get; set; }
            [DataMember]
            public string diagnosisclassstatus { get; set; }
            [DataMember]
            public DateTime diagnosisdateonsetdatetime { get; set; }
            [DataMember]
            public DateTime diagnosiswhenclassdatetime { get; set; }
            [DataMember]
            public string diagnosiscode { get; set; }
            [DataMember]
            public string diagnosiscodingsystem { get; set; }
        }

        public class SurgeryData
        {
            [DataMember]
            public string locationcode { get; set; }
            [DataMember]
            public string surgeryname { get; set; }
            [DataMember]
            public string locationstreet { get; set; }
            [DataMember]
            public string locationsuburb { get; set; }
            [DataMember]
            public string locationcity { get; set; }
            [DataMember]
            public string locationpostcode { get; set; }
            [DataMember]
            public string postalstreet { get; set; }
            [DataMember]
            public string postalsuburb { get; set; }
            [DataMember]
            public string postalcity { get; set; }
            [DataMember]
            public string postalpostcode { get; set; }
            [DataMember]
            public string dayphone { get; set; }
            [DataMember]
            public string afterhoursphone { get; set; }
            [DataMember]
            public string faxphone { get; set; }
            [DataMember]
            public string hpifacilitynumber { get; set; }

        }

        public class Invoice
        {
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string UserId { get; set; }
            public string ResourceType { get; set; }
            public string System { get; set; }
            public string LocationId { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string ClaimType { get; set; }
            public string Fee { get; set; }
        }
        public class UserAuthenticate
        {
            [DataMember]
            public string Token { get; set; }
            [DataMember]
            public string Expiry { get; set; }
            [DataMember]
            public string PracticeId { get; set; }
            [DataMember]
            public string error { get; set; }
        }
    }
}