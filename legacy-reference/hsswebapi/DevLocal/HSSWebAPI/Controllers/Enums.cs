using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HSSWebAPI.Controllers
{
    public static class Enums
    {
        public enum CallType
        {
            ACC18,
            Demographic,
            Ethnicity,
            ACCEvents,
            ReleveantAccident,
            ConsultNote,
            ConsultDetail,
            Screening,
            ScreeningWithDetail,
            ScreeningDetail,
            MedicalWarning,
            Diagnosis,
            FamilyHistory,
            LongTermMedication,
            RecentMedication,

            QueryReferal,
            SaveReferal,
            ReferalStatus,
            ReferralDetails,
            Investigations,
            GetPatientInbox,
            GetPatientInboxDetail,
            GetPatientInboxDoc,

            CFDemographic,
            CFMedicalWarning,
            CFMedicines,
            CFObservation,
            CFRecalls,
            CFLabs,
            CFIMMS,

            CFDiabetesProject,
            CFDiabeticFootExamination,
            CFRetinopathy,
            CFUrinalysis,
            CFMAM_CX,
            CFOtherCalculated,

            CFConformance,
            CFConditions,
            CFFamilyHistory,
            CFMedicinesReview,

            CFDCIPReview,
            CFCVRAssessment,

            GetNextofKin,
            GetReferalInformation,
            GetLongTermMedication,
            GetLabMessages,
            GetBodyMapData,

            CFGetDocumentDetail,
        };
    }
}