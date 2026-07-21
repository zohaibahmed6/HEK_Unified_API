-- ============================================================================
-- HEK Core API — dummy seed data for local end-to-end testing.
-- Run AFTER 03_LegacySchema_Build.sql. Target: PMS_NZ_V2 (or wherever built).
-- Idempotent: safe to re-run.
-- ============================================================================

-- Matches the practice seeded into the tenant registry: TEST-PRACTICE-001 -> dbserver-local.fff / PMS_NZ_V2
DECLARE @PracticeId NVARCHAR(64) = N'TEST-PRACTICE-001';
DECLARE @PatientId INT = 1001;
DECLARE @EncounterId INT = 5001;

IF NOT EXISTS (SELECT 1 FROM [HSS].[Patients] WHERE [PatientId] = @PatientId)
INSERT INTO [HSS].[Patients] ([PatientId], [PracticeId], [FirstName], [LastName], [DateOfBirth], [DateOfEnrolment], [EndEnrolmentDate], [EncounterId], [Dob], [Nhi])
VALUES (@PatientId, @PracticeId, N'Amy', N'Mouse', '1985-05-05', '2015-03-01', NULL, @EncounterId, '1985-05-05', N'ZCN4440');

IF NOT EXISTS (SELECT 1 FROM [HSS].[ClinicalNotes] WHERE [PatientId] = @PatientId)
INSERT INTO [HSS].[ClinicalNotes] ([PatientId], [EncounterId], [Author], [Content])
VALUES (@PatientId, @EncounterId, N'Dr. Test Provider', N'Routine check-up, patient in good health.');

IF NOT EXISTS (SELECT 1 FROM [HSS].[Conditions] WHERE [PatientId] = @PatientId)
INSERT INTO [HSS].[Conditions] ([PatientId], [EncounterId], [DiagnosisCode], [Description], [IsLongTerm])
VALUES (@PatientId, @EncounterId, N'J45', N'Asthma', 1);

IF NOT EXISTS (SELECT 1 FROM [HSS].[Medications] WHERE [PatientId] = @PatientId)
BEGIN
    INSERT INTO [HSS].[Medications] ([PatientId], [EncounterId], [Name], [PrescribedDate], [Kind]) VALUES (@PatientId, @EncounterId, N'Salbutamol Inhaler', GETDATE(), N'regular');
    INSERT INTO [HSS].[Medications] ([PatientId], [EncounterId], [Name], [PrescribedDate], [Kind]) VALUES (@PatientId, @EncounterId, N'Amoxicillin', GETDATE(), N'prescribed');
END

IF NOT EXISTS (SELECT 1 FROM [HSS].[Reports] WHERE [PatientId] = @PatientId)
BEGIN
    INSERT INTO [HSS].[Reports] ([PatientId], [EncounterId], [Kind], [Type], [Content]) VALUES (@PatientId, @EncounterId, N'Lab', N'Blood Test', N'Results within normal range.');
    INSERT INTO [HSS].[Reports] ([PatientId], [EncounterId], [Kind], [Type], [Content]) VALUES (@PatientId, @EncounterId, N'Radiology', N'Chest X-Ray', N'No abnormalities detected.');
END

IF NOT EXISTS (SELECT 1 FROM [HSS].[Documents] WHERE [PatientId] = @PatientId)
INSERT INTO [HSS].[Documents] ([PatientId], [Direction], [ContentType], [Subject], [ReferenceId], [Content])
VALUES (@PatientId, N'in', N'application/pdf', N'Referral Letter', N'REF-0001', CAST(N'dummy content' AS VARBINARY(MAX)));

IF NOT EXISTS (SELECT 1 FROM [HSS].[Observations] WHERE [PatientId] = @PatientId)
INSERT INTO [HSS].[Observations] ([PatientId], [EncounterId], [ConceptId], [Value], [Height], [Weight], [Bmi], [BloodPressureSystolic], [BloodPressureDiastolic])
VALUES (@PatientId, @EncounterId, N'vitals', N'normal', 170, 65, 22.5, 120, 80);

IF NOT EXISTS (SELECT 1 FROM [HSS].[TemplateSchemas] WHERE [Identifier] = N'diap')
BEGIN
    INSERT INTO [HSS].[TemplateSchemas] ([Identifier], [Name], [Caption], [Type]) VALUES (N'diap', N'hba1c', N'HbA1c Level', N'float');
    INSERT INTO [HSS].[TemplateSchemas] ([Identifier], [Name], [Caption], [Type]) VALUES (N'diap', N'footExamDone', N'Foot Exam Done', N'boolean');
END

IF NOT EXISTS (SELECT 1 FROM [HSS].[RecallCategories])
BEGIN
    INSERT INTO [HSS].[RecallCategories] ([CategoryId], [Name], [GroupName]) VALUES (N'CAT-001', N'Diabetes Review', N'chronic-care');
    INSERT INTO [HSS].[RecallCategories] ([CategoryId], [Name], [GroupName]) VALUES (N'CAT-002', N'Immunisation', N'preventive');
END

IF NOT EXISTS (SELECT 1 FROM [HSS].[ScreeningCodes])
BEGIN
    INSERT INTO [HSS].[ScreeningCodes] ([Code], [Description]) VALUES (N'CVD-RISK', N'Cardiovascular Risk Assessment');
    INSERT INTO [HSS].[ScreeningCodes] ([Code], [Description]) VALUES (N'CST', N'Cervical Screening Test');
END

IF NOT EXISTS (SELECT 1 FROM [HSS].[Providers])
INSERT INTO [HSS].[Providers] ([ProviderId], [Name], [PracticeLocationId]) VALUES (N'PROV-001', N'Dr. Test Provider', N'LOC-001');

IF NOT EXISTS (SELECT 1 FROM [HSS].[SurgeryData] WHERE [PracticeId] = @PracticeId)
INSERT INTO [HSS].[SurgeryData] ([PracticeId], [Name], [Address]) VALUES (@PracticeId, N'Local Test Practice', N'123 Test Street, Christchurch');

IF NOT EXISTS (SELECT 1 FROM [HSS].[SessionData] WHERE [PracticeId] = @PracticeId)
INSERT INTO [HSS].[SessionData] ([PracticeId], [SessionInfo]) VALUES (@PracticeId, N'{"sessionType":"dev-test"}');

-- HISO concept-engine parameter dictionary rows (Hiso.ProcedureParams) - one row per param the
-- matching Hiso.* procedure declares (see 03_LegacySchema_Build.sql for the procedure signatures).
IF NOT EXISTS (SELECT 1 FROM [Hiso].[ProcedureParams])
BEGIN
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Demographics', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_ConsultNotes', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_ConsultNotes', N'@fromdate');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_ConsultNotes', N'@todate');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_ConsultNotes', N'@sortby');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Diagnosis', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Medications', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Measurements', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_LaboratoryReport', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Acc45Form', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Acc45Form', N'@ppracticeid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Acc45Form_Static', N'@ppatientid');
    INSERT INTO [Hiso].[ProcedureParams] VALUES (N'Hiso.uspGetPatient_Acc45Form_Static', N'@ppracticeid');
END

IF NOT EXISTS (SELECT 1 FROM [Hiso].[DeliveryOptions] WHERE [PracticeId] = @PracticeId)
INSERT INTO [Hiso].[DeliveryOptions] ([PracticeId], [Url], [PracticeEdi]) VALUES (@PracticeId, N'https://edi.example.test', N'TESTEDI');

IF NOT EXISTS (SELECT 1 FROM [Hiso].[Concepts])
BEGIN
    INSERT INTO [Hiso].[Concepts] ([ConceptCode], [ConceptName]) VALUES (N'233604007', N'Fracture of foot');
    INSERT INTO [Hiso].[Concepts] ([ConceptCode], [ConceptName]) VALUES (N'44054006', N'Type 2 diabetes mellitus');
END

PRINT 'HEK Core API dummy seed data inserted.';
GO
