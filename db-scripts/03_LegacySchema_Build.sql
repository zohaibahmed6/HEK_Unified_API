-- ============================================================================
-- HEK Core API — legacy-shaped schema build script
-- Target: PMS_NZ_V2 (dbserver-local.fff), or wherever you point it.
-- Generated from db-scripts/REQUIRED_SCHEMA_INVENTORY.md — every table/procedure
-- here exists because a specific Infrastructure repository calls it with these
-- exact names/parameters. Run with an account that has CREATE TABLE/PROCEDURE/
-- SCHEMA rights (pms_nz currently does not — see AI_USAGE_LOG.md).
-- Idempotent: safe to re-run.
-- ============================================================================

IF SCHEMA_ID(N'HSS') IS NULL EXEC('CREATE SCHEMA [HSS]');
GO
IF SCHEMA_ID(N'Hiso') IS NULL EXEC('CREATE SCHEMA [Hiso]');
GO
IF SCHEMA_ID(N'Task') IS NULL EXEC('CREATE SCHEMA [Task]');
GO
IF SCHEMA_ID(N'Profile') IS NULL EXEC('CREATE SCHEMA [Profile]');
GO
IF SCHEMA_ID(N'Appointment') IS NULL EXEC('CREATE SCHEMA [Appointment]');
GO
IF SCHEMA_ID(N'Prompt') IS NULL EXEC('CREATE SCHEMA [Prompt]');
GO

-- ============================================================================
-- Section 1: Backing tables
-- ============================================================================

IF OBJECT_ID(N'[HSS].[Patients]') IS NULL
CREATE TABLE [HSS].[Patients] (
    [PatientId] INT NOT NULL PRIMARY KEY,
    [PracticeId] NVARCHAR(64) NOT NULL,
    [FirstName] NVARCHAR(128) NOT NULL,
    [LastName] NVARCHAR(128) NOT NULL,
    [DateOfBirth] DATE NOT NULL,
    [DateOfEnrolment] DATE NULL,
    [EndEnrolmentDate] DATE NULL,
    [EncounterId] INT NULL,
    [Dob] DATE NULL,
    [Nhi] NVARCHAR(16) NULL
);
GO

IF OBJECT_ID(N'[HSS].[ClinicalNotes]') IS NULL
CREATE TABLE [HSS].[ClinicalNotes] (
    [NoteId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [EncounterId] INT NOT NULL,
    [Author] NVARCHAR(256) NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Content] NVARCHAR(MAX) NOT NULL
);
GO

IF OBJECT_ID(N'[HSS].[Conditions]') IS NULL
CREATE TABLE [HSS].[Conditions] (
    [ConditionId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [EncounterId] INT NOT NULL,
    [DiagnosisCode] NVARCHAR(32) NOT NULL,
    [Description] NVARCHAR(512) NULL,
    [IsLongTerm] BIT NOT NULL DEFAULT 0,
    [SideCode] NVARCHAR(32) NULL,
    [SideDescription] NVARCHAR(256) NULL
);
GO

IF OBJECT_ID(N'[HSS].[Medications]') IS NULL
CREATE TABLE [HSS].[Medications] (
    [MedicationId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [EncounterId] INT NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [PrescribedDate] DATE NULL,
    [Kind] NVARCHAR(16) NOT NULL DEFAULT 'regular' -- 'regular' | 'prescribed'
);
GO

IF OBJECT_ID(N'[HSS].[Reports]') IS NULL
CREATE TABLE [HSS].[Reports] (
    [ReportId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [EncounterId] INT NOT NULL,
    [Kind] NVARCHAR(16) NOT NULL, -- 'Lab' | 'Radiology'
    [Type] NVARCHAR(64) NULL,
    [Date] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Content] NVARCHAR(MAX) NULL
);
GO

IF OBJECT_ID(N'[HSS].[Documents]') IS NULL
CREATE TABLE [HSS].[Documents] (
    [DocumentId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [Direction] NVARCHAR(8) NOT NULL, -- 'in' | 'out'
    [ContentType] NVARCHAR(128) NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Subject] NVARCHAR(256) NULL,
    [ReferenceId] NVARCHAR(128) NULL,
    [Content] VARBINARY(MAX) NULL
);
GO

IF OBJECT_ID(N'[HSS].[Observations]') IS NULL
CREATE TABLE [HSS].[Observations] (
    [ObservationId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [EncounterId] INT NOT NULL,
    [ConceptId] NVARCHAR(64) NULL,
    [Value] NVARCHAR(256) NULL,
    [RecordedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Height] FLOAT NULL, [Weight] FLOAT NULL, [Bmi] FLOAT NULL,
    [BloodPressureSystolic] FLOAT NULL, [BloodPressureDiastolic] FLOAT NULL,
    [WaistCircumference] FLOAT NULL, [SmokingStatus] NVARCHAR(64) NULL,
    [HeartRate] FLOAT NULL, [Temperature] FLOAT NULL
);
GO

IF OBJECT_ID(N'[HSS].[TemplateSchemas]') IS NULL
CREATE TABLE [HSS].[TemplateSchemas] (
    [Identifier] NVARCHAR(64) NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Caption] NVARCHAR(256) NOT NULL,
    [Type] NVARCHAR(32) NOT NULL,
    PRIMARY KEY ([Identifier], [Name])
);
GO

IF OBJECT_ID(N'[HSS].[EncounterSummaries]') IS NULL
CREATE TABLE [HSS].[EncounterSummaries] (
    [PatientId] INT NOT NULL,
    [EncounterId] INT NOT NULL,
    [Identifier] NVARCHAR(64) NOT NULL,
    [Fields] NVARCHAR(MAX) NOT NULL,
    PRIMARY KEY ([PatientId], [EncounterId], [Identifier])
);
GO

IF OBJECT_ID(N'[HSS].[RecallCategories]') IS NULL
CREATE TABLE [HSS].[RecallCategories] (
    [CategoryId] NVARCHAR(64) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(256) NOT NULL,
    [GroupName] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID(N'[HSS].[Recalls]') IS NULL
CREATE TABLE [HSS].[Recalls] (
    [RecallId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [CategoryId] NVARCHAR(64) NULL,
    [DueDate] DATE NOT NULL
);
GO

IF OBJECT_ID(N'[HSS].[ScreeningCodes]') IS NULL
CREATE TABLE [HSS].[ScreeningCodes] (
    [Code] NVARCHAR(32) NOT NULL PRIMARY KEY,
    [Description] NVARCHAR(256) NOT NULL
);
GO

IF OBJECT_ID(N'[HSS].[ScreeningResults]') IS NULL
CREATE TABLE [HSS].[ScreeningResults] (
    [Id] INT IDENTITY PRIMARY KEY,
    [PatientId] INT NOT NULL,
    [EncounterId] INT NOT NULL,
    [Code] NVARCHAR(32) NOT NULL,
    [Value] NVARCHAR(256) NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

IF OBJECT_ID(N'[HSS].[Providers]') IS NULL
CREATE TABLE [HSS].[Providers] (
    [ProviderId] NVARCHAR(64) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(256) NOT NULL,
    [PracticeLocationId] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID(N'[HSS].[SurgeryData]') IS NULL
CREATE TABLE [HSS].[SurgeryData] (
    [PracticeId] NVARCHAR(64) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(256) NULL,
    [Address] NVARCHAR(512) NULL
);
GO

IF OBJECT_ID(N'[HSS].[SessionData]') IS NULL
CREATE TABLE [HSS].[SessionData] (
    [PracticeId] NVARCHAR(64) NOT NULL PRIMARY KEY,
    [SessionInfo] NVARCHAR(512) NULL
);
GO

IF OBJECT_ID(N'[HSS].[Invoices]') IS NULL
CREATE TABLE [HSS].[Invoices] (
    [InvoiceId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [ServiceCode] NVARCHAR(32) NOT NULL,
    [ServiceName] NVARCHAR(256) NULL,
    [AmountInclGST] DECIMAL(10,2) NOT NULL,
    [Payee] NVARCHAR(256) NULL,
    [ServiceProvider] NVARCHAR(256) NULL,
    [ServiceDate] DATE NULL,
    [PegasusReference] NVARCHAR(128) NULL,
    [ClaimShortCode] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID(N'[Hiso].[ProcedureParams]') IS NULL
CREATE TABLE [Hiso].[ProcedureParams] (
    [ProcedureName] NVARCHAR(128) NOT NULL,
    [Parameter_name] NVARCHAR(128) NOT NULL,
    PRIMARY KEY ([ProcedureName], [Parameter_name])
);
GO

IF OBJECT_ID(N'[Hiso].[Acc45Forms]') IS NULL
CREATE TABLE [Hiso].[Acc45Forms] (
    [FormInstanceId] NVARCHAR(64) NOT NULL PRIMARY KEY,
    [ViewType] NVARCHAR(64) NULL,
    [View] NVARCHAR(64) NULL,
    [DataContainer] NVARCHAR(MAX) NULL,
    [PatientId] INT NULL,
    [AppointmentId] NVARCHAR(64) NULL,
    [PracticeId] NVARCHAR(64) NULL,
    [Completed] BIT NULL,
    [DmsGuid] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID(N'[Hiso].[DeliveryOptions]') IS NULL
CREATE TABLE [Hiso].[DeliveryOptions] (
    [PracticeId] NVARCHAR(64) NOT NULL PRIMARY KEY,
    [Url] NVARCHAR(512) NULL,
    [PracticeEdi] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID(N'[Hiso].[Concepts]') IS NULL
CREATE TABLE [Hiso].[Concepts] (
    [ConceptCode] NVARCHAR(32) NOT NULL PRIMARY KEY,
    [ConceptName] NVARCHAR(256) NOT NULL
);
GO

IF OBJECT_ID(N'[Hiso].[Tasks]') IS NULL
CREATE TABLE [Hiso].[Tasks] (
    [TaskId] NVARCHAR(64) NOT NULL PRIMARY KEY DEFAULT (CONVERT(NVARCHAR(64), NEWID())),
    [PatientId] INT NOT NULL,
    [Subject] NVARCHAR(512) NOT NULL,
    [StatusId] NVARCHAR(16) NOT NULL
);
GO

IF OBJECT_ID(N'[Appointment].[tblHealthLinkSession]') IS NULL
CREATE TABLE [Appointment].[tblHealthLinkSession] (
    [SessionGUID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [ProviderID] NVARCHAR(64) NOT NULL,
    [PatientID] NVARCHAR(64) NOT NULL,
    [AppointmentID] NVARCHAR(64) NOT NULL,
    [PracticeID] NVARCHAR(64) NOT NULL,
    [CreatedAtUtc] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET() -- flagged inference, PROJECT_STATUS.md item 25
);
GO

-- Dormant DMSDA.cs-related tables (lowest priority - not wired to any live endpoint)
IF OBJECT_ID(N'[Prompt].[tblInboxFolderItem]') IS NULL
CREATE TABLE [Prompt].[tblInboxFolderItem] (
    [InboxFolderItemID] INT NOT NULL PRIMARY KEY,
    [DMSID] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID(N'[dbo].[Documents]') IS NULL
CREATE TABLE [dbo].[Documents] (
    [DocumentID] INT IDENTITY PRIMARY KEY,
    [ClientID] INT NULL, [CategoryID] INT NULL, [DocumentName] NVARCHAR(256) NULL,
    [DocumentTypeID] INT NULL, [Description] NVARCHAR(512) NULL, [DocumentKey] NVARCHAR(128) NULL,
    [DocumentSize] INT NULL, [ProfileID] NVARCHAR(16) NULL, [DocumentData] VARBINARY(MAX) NULL,
    [IsCorrupt] BIT NOT NULL DEFAULT 0
);
GO

-- ============================================================================
-- Section 2: Stored procedures — Hiso schema (concept-mapping engine + ACC45)
-- ============================================================================

CREATE OR ALTER PROCEDURE [Hiso].[USPGetProcedureParamList] @pProcedureName NVARCHAR(128) AS
BEGIN
    SELECT [Parameter_name] FROM [Hiso].[ProcedureParams] WHERE [ProcedureName] = @pProcedureName;
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_Demographics] @ppatientid NVARCHAR(64) AS
BEGIN
    SELECT [FirstName], [LastName], [DateOfBirth] FROM [HSS].[Patients] WHERE [PatientId] = TRY_CAST(@ppatientid AS INT);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_ConsultNotes]
    @ppatientid NVARCHAR(64), @fromdate NVARCHAR(64) = NULL, @todate NVARCHAR(64) = NULL, @sortby NVARCHAR(16) = NULL AS
BEGIN
    SELECT [NoteId], [Author], [CreatedAt], [Content] FROM [HSS].[ClinicalNotes]
    WHERE [PatientId] = TRY_CAST(@ppatientid AS INT)
    ORDER BY CASE WHEN @sortby = 'asc' THEN [CreatedAt] END ASC, CASE WHEN @sortby IS NULL OR @sortby = 'desc' THEN [CreatedAt] END DESC;
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_Diagnosis] @ppatientid NVARCHAR(64) AS
BEGIN
    SELECT [ConditionId], [DiagnosisCode], [Description], [IsLongTerm], [SideCode], [SideDescription]
    FROM [HSS].[Conditions] WHERE [PatientId] = TRY_CAST(@ppatientid AS INT);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_Medications] @ppatientid NVARCHAR(64) AS
BEGIN
    SELECT [MedicationId], [Name], [PrescribedDate] FROM [HSS].[Medications] WHERE [PatientId] = TRY_CAST(@ppatientid AS INT);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_Measurements] @ppatientid NVARCHAR(64) AS
BEGIN
    SELECT [ObservationId], [ConceptId], [Value], [RecordedAt] FROM [HSS].[Observations] WHERE [PatientId] = TRY_CAST(@ppatientid AS INT);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_LaboratoryReport] @ppatientid NVARCHAR(64) AS
BEGIN
    SELECT [ReportId], [Type], [Date] FROM [HSS].[Reports] WHERE [PatientId] = TRY_CAST(@ppatientid AS INT) AND [Kind] = 'Lab';
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_Acc45Form] @ppatientid NVARCHAR(64) = NULL, @ppracticeid NVARCHAR(64) = NULL AS
BEGIN
    SELECT [FormInstanceId], [ViewType], [View], [DataContainer]
    FROM [Hiso].[Acc45Forms] WHERE (@ppracticeid IS NULL OR [PracticeId] = @ppracticeid);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetPatient_Acc45Form_Static] @ppatientid NVARCHAR(64) = NULL, @ppracticeid NVARCHAR(64) = NULL AS
BEGIN
    SELECT [FormInstanceId], [ViewType], [View], [DataContainer]
    FROM [Hiso].[Acc45Forms] WHERE (@ppracticeid IS NULL OR [PracticeId] = @ppracticeid);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetDeliveryOptions] @pPracticeID NVARCHAR(64) AS
BEGIN
    SELECT [Url], [PracticeEdi] FROM [Hiso].[DeliveryOptions] WHERE [PracticeId] = @pPracticeID;
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetFormView] @pFormInstanceID NVARCHAR(64) AS
BEGIN
    SELECT [ViewType], [View], [DataContainer] FROM [Hiso].[Acc45Forms] WHERE [FormInstanceId] = @pFormInstanceID;
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspSaveAcc45Definition]
    @pFormInstanceID NVARCHAR(64), @pPatientID NVARCHAR(64), @pAppointmentID NVARCHAR(64), @pPracticeID NVARCHAR(64),
    @pDataContainer NVARCHAR(MAX), @pView NVARCHAR(64) = NULL, @pViewType NVARCHAR(64) = NULL,
    @pCompleted BIT, @pDmsGuid NVARCHAR(64) AS
BEGIN
    IF EXISTS (SELECT 1 FROM [Hiso].[Acc45Forms] WHERE [FormInstanceId] = @pFormInstanceID)
        UPDATE [Hiso].[Acc45Forms] SET [ViewType] = @pViewType, [View] = @pView, [DataContainer] = @pDataContainer,
            [PatientId] = TRY_CAST(@pPatientID AS INT), [AppointmentId] = @pAppointmentID, [PracticeId] = @pPracticeID,
            [Completed] = @pCompleted, [DmsGuid] = @pDmsGuid
        WHERE [FormInstanceId] = @pFormInstanceID;
    ELSE
        INSERT INTO [Hiso].[Acc45Forms] ([FormInstanceId], [ViewType], [View], [DataContainer], [PatientId], [AppointmentId], [PracticeId], [Completed], [DmsGuid])
        VALUES (@pFormInstanceID, @pViewType, @pView, @pDataContainer, TRY_CAST(@pPatientID AS INT), @pAppointmentID, @pPracticeID, @pCompleted, @pDmsGuid);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspProcessAction_Save]
    @pSessionKey UNIQUEIDENTIFIER, @pPatientID NVARCHAR(64), @pAppointmentID NVARCHAR(64), @pPracticeID NVARCHAR(64), @pActionContainer NVARCHAR(MAX) = NULL AS
BEGIN
    SELECT 1 AS Processed; -- placeholder: real business logic TBD once real requirements exist
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspProcessAction_AddTask]
    @pSessionKey UNIQUEIDENTIFIER, @pPatientID NVARCHAR(64), @pAppointmentID NVARCHAR(64), @pPracticeID NVARCHAR(64), @pActionContainer NVARCHAR(MAX) = NULL AS
BEGIN
    INSERT INTO [Hiso].[Tasks] ([PatientId], [Subject], [StatusId]) VALUES (TRY_CAST(@pPatientID AS INT), N'Task from ACC45 action', N'1');
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspProcessAction_AddInvoice]
    @pSessionKey UNIQUEIDENTIFIER, @pPatientID NVARCHAR(64), @pAppointmentID NVARCHAR(64), @pPracticeID NVARCHAR(64), @pActionContainer NVARCHAR(MAX) = NULL AS
BEGIN
    INSERT INTO [HSS].[Invoices] ([PatientId], [ServiceCode], [AmountInclGST]) VALUES (TRY_CAST(@pPatientID AS INT), N'ACC45', 0);
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspProcessAction_LaunchForm]
    @pSessionKey UNIQUEIDENTIFIER, @pPatientID NVARCHAR(64), @pAppointmentID NVARCHAR(64), @pPracticeID NVARCHAR(64), @pActionContainer NVARCHAR(MAX) = NULL AS
BEGIN
    SELECT 1 AS Processed; -- placeholder: real business logic TBD once real requirements exist
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspGetConceptName] @pConceptCode NVARCHAR(32) AS
BEGIN
    SELECT [ConceptName] FROM [Hiso].[Concepts] WHERE [ConceptCode] = @pConceptCode;
END
GO

CREATE OR ALTER PROCEDURE [Hiso].[uspAddTask]
    @pPatientID NVARCHAR(64), @pSubject NVARCHAR(512), @pStatusID NVARCHAR(16), @pTaskIDOut NVARCHAR(64) OUTPUT AS
BEGIN
    DECLARE @NewId NVARCHAR(64) = CONVERT(NVARCHAR(64), NEWID());
    INSERT INTO [Hiso].[Tasks] ([TaskId], [PatientId], [Subject], [StatusId]) VALUES (@NewId, TRY_CAST(@pPatientID AS INT), @pSubject, @pStatusID);
    SET @pTaskIDOut = @NewId;
END
GO

-- ============================================================================
-- Section 3: Stored procedures — HSS schema (KARO/ERMS/COL)
-- ============================================================================

CREATE OR ALTER PROCEDURE [HSS].[uspGetDemographics] @pPatientID INT AS
BEGIN
    SELECT [FirstName], [LastName], [DateOfBirth], [DateOfEnrolment], [EndEnrolmentDate] FROM [HSS].[Patients] WHERE [PatientId] = @pPatientID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetPatientData] @pPatientID INT AS
BEGIN
    SELECT [EncounterId], [FirstName], [LastName], [Dob], [Nhi] FROM [HSS].[Patients] WHERE [PatientId] = @pPatientID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetCurrentPatientData] @pPatientID INT AS
BEGIN
    SELECT [FirstName], [LastName] FROM [HSS].[Patients] WHERE [PatientId] = @pPatientID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetClinicalNotes]
    @pPatientID INT, @pEncounterID INT, @pSinceDate DATETIME2 = NULL, @pUntilDate DATETIME2 = NULL, @pSortOrder NVARCHAR(16) = NULL AS
BEGIN
    SELECT [NoteId], [Author], [CreatedAt], [Content] FROM [HSS].[ClinicalNotes] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetConsultNotes]
    @pPatientID INT, @pEncounterID INT, @pSinceDate DATETIME2 = NULL, @pUntilDate DATETIME2 = NULL, @pSortOrder NVARCHAR(16) = NULL AS
BEGIN
    SELECT [NoteId], [Author], [CreatedAt], [Content] FROM [HSS].[ClinicalNotes] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveClinicalNotes]
    @pPatientID INT, @pEncounterID INT, @pContent NVARCHAR(MAX), @pNoteIDOut NVARCHAR(64) OUTPUT AS
BEGIN
    DECLARE @NewId NVARCHAR(64) = CONVERT(NVARCHAR(64), NEWID());
    INSERT INTO [HSS].[ClinicalNotes] ([NoteId], [PatientId], [EncounterId], [Content]) VALUES (@NewId, @pPatientID, @pEncounterID, @pContent);
    SET @pNoteIDOut = @NewId;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetConditions] @pPatientID INT, @pEncounterID INT AS
BEGIN
    SELECT [ConditionId], [DiagnosisCode], [Description], [IsLongTerm], [SideCode], [SideDescription]
    FROM [HSS].[Conditions] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetClassifications] @pPatientID INT, @pEncounterID INT AS
BEGIN
    SELECT [ConditionId], [DiagnosisCode], [Description], [IsLongTerm], [SideCode], [SideDescription]
    FROM [HSS].[Conditions] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetDiagnosisData] @pPatientID INT, @pEncounterID INT AS
BEGIN
    SELECT [ConditionId], [DiagnosisCode], [Description], [IsLongTerm], [SideCode], [SideDescription]
    FROM [HSS].[Conditions] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspFindConditionByNaturalKey] @pEncounterID INT, @pDiagnosisCode NVARCHAR(32) AS
BEGIN
    SELECT TOP 1 [ConditionId], [DiagnosisCode], [Description], [IsLongTerm], [SideCode], [SideDescription]
    FROM [HSS].[Conditions] WHERE [EncounterId] = @pEncounterID AND [DiagnosisCode] = @pDiagnosisCode;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveCondition]
    @pPatientID INT, @pEncounterID INT, @pDiagnosisCode NVARCHAR(32), @pDescription NVARCHAR(512) = NULL,
    @pIsLongTerm BIT = 0, @pSideCode NVARCHAR(32) = NULL, @pSideDescription NVARCHAR(256) = NULL,
    @pConditionIDOut NVARCHAR(64) OUTPUT AS
BEGIN
    DECLARE @NewId NVARCHAR(64) = CONVERT(NVARCHAR(64), NEWID());
    INSERT INTO [HSS].[Conditions] ([ConditionId], [PatientId], [EncounterId], [DiagnosisCode], [Description], [IsLongTerm], [SideCode], [SideDescription])
    VALUES (@NewId, @pPatientID, @pEncounterID, @pDiagnosisCode, @pDescription, @pIsLongTerm, @pSideCode, @pSideDescription);
    SET @pConditionIDOut = @NewId;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetMedications] @pPatientID INT, @pEncounterID INT AS
BEGIN
    SELECT [MedicationId], [Name], [PrescribedDate] FROM [HSS].[Medications] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetPrescribedMedications] @pPatientID INT, @pEncounterID INT AS
BEGIN
    SELECT [MedicationId], [Name], [PrescribedDate] FROM [HSS].[Medications] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Kind] = 'prescribed';
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetRegularMedications] @pPatientID INT, @pEncounterID INT AS
BEGIN
    SELECT [MedicationId], [Name], [PrescribedDate] FROM [HSS].[Medications] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Kind] = 'regular';
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetLabResults]
    @pPatientID INT, @pEncounterID INT, @pSinceDate DATETIME2 = NULL, @pUntilDate DATETIME2 = NULL, @pSortOrder NVARCHAR(16) = NULL AS
BEGIN
    SELECT [ReportId], [Type], [Date] FROM [HSS].[Reports] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Kind] = 'Lab';
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetLaboratoryReportList]
    @pPatientID INT, @pEncounterID INT, @pSinceDate DATETIME2 = NULL, @pUntilDate DATETIME2 = NULL, @pSortOrder NVARCHAR(16) = NULL AS
BEGIN
    SELECT [ReportId], [Type], [Date] FROM [HSS].[Reports] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Kind] = 'Lab';
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetRadiologyReportList]
    @pPatientID INT, @pEncounterID INT, @pSinceDate DATETIME2 = NULL, @pUntilDate DATETIME2 = NULL, @pSortOrder NVARCHAR(16) = NULL AS
BEGIN
    SELECT [ReportId], [Type], [Date] FROM [HSS].[Reports] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Kind] = 'Radiology';
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetLaboratoryReportDetails] @pReportID NVARCHAR(64) AS
BEGIN
    SELECT [Content] FROM [HSS].[Reports] WHERE [ReportId] = @pReportID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetRadiologyReportDetails] @pReportID NVARCHAR(64) AS
BEGIN
    SELECT [Content] FROM [HSS].[Reports] WHERE [ReportId] = @pReportID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetDocuments]
    @pPatientID INT, @pDirection NVARCHAR(8) = NULL, @pContentType NVARCHAR(128) = NULL, @pReferenceID NVARCHAR(128) = NULL,
    @pSubject NVARCHAR(256) = NULL, @pSinceDate DATETIME2 = NULL, @pUntilDate DATETIME2 = NULL, @pSortOrder NVARCHAR(16) = NULL AS
BEGIN
    SELECT [DocumentId], [PatientId], [Direction], [ContentType], [CreatedAt], [Subject], [ReferenceId]
    FROM [HSS].[Documents]
    WHERE [PatientId] = @pPatientID
      AND (@pDirection IS NULL OR [Direction] = @pDirection)
      AND (@pContentType IS NULL OR [ContentType] = @pContentType)
      AND (@pReferenceID IS NULL OR [ReferenceId] = @pReferenceID);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetScannedList]
    @pPatientID INT, @pDirection NVARCHAR(8) = NULL, @pContentType NVARCHAR(128) = NULL, @pReferenceID NVARCHAR(128) = NULL,
    @pSubject NVARCHAR(256) = NULL, @pSinceDate DATETIME2 = NULL, @pUntilDate DATETIME2 = NULL, @pSortOrder NVARCHAR(16) = NULL AS
BEGIN
    SELECT [DocumentId], [PatientId], [Direction], [ContentType], [CreatedAt], [Subject], [ReferenceId]
    FROM [HSS].[Documents] WHERE [PatientId] = @pPatientID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetScannedDetails] @pDocumentID NVARCHAR(64) AS
BEGIN
    SELECT [DocumentId], [PatientId], [Direction], [ContentType], [CreatedAt], [Subject], [ReferenceId], [Content]
    FROM [HSS].[Documents] WHERE [DocumentId] = @pDocumentID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspFindDocumentByReferenceId] @pReferenceID NVARCHAR(128) AS
BEGIN
    SELECT TOP 1 [DocumentId], [PatientId], [Direction], [ContentType], [CreatedAt], [Subject], [ReferenceId]
    FROM [HSS].[Documents] WHERE [ReferenceId] = @pReferenceID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveDocument]
    @pPatientID INT, @pDirection NVARCHAR(8), @pContentType NVARCHAR(128), @pSubject NVARCHAR(256) = NULL,
    @pReferenceID NVARCHAR(128) = NULL, @pContent VARBINARY(MAX), @pDocumentIDOut NVARCHAR(64) OUTPUT AS
BEGIN
    DECLARE @NewId NVARCHAR(64) = CONVERT(NVARCHAR(64), NEWID());
    INSERT INTO [HSS].[Documents] ([DocumentId], [PatientId], [Direction], [ContentType], [Subject], [ReferenceId], [Content])
    VALUES (@NewId, @pPatientID, @pDirection, @pContentType, @pSubject, @pReferenceID, @pContent);
    SET @pDocumentIDOut = @NewId;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetObservations] @pPatientID INT, @pEncounterID INT, @pConceptID NVARCHAR(64) = NULL AS
BEGIN
    SELECT [ObservationId], [ConceptId], [Value], [RecordedAt] FROM [HSS].[Observations]
    WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND (@pConceptID IS NULL OR [ConceptId] = @pConceptID);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetPatientMeasurement] @pPatientID INT, @pEncounterID INT, @pConceptID NVARCHAR(64) = NULL AS
BEGIN
    SELECT [ObservationId], [ConceptId], [Value], [RecordedAt] FROM [HSS].[Observations]
    WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND (@pConceptID IS NULL OR [ConceptId] = @pConceptID);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveObservations]
    @pPatientID INT, @pEncounterID INT, @pHeight FLOAT = NULL, @pWeight FLOAT = NULL, @pBMI FLOAT = NULL,
    @pBloodPressureSystolic FLOAT = NULL, @pBloodPressureDiastolic FLOAT = NULL, @pWaistCircumference FLOAT = NULL,
    @pSmokingStatus NVARCHAR(64) = NULL, @pHeartRate FLOAT = NULL, @pTemperature FLOAT = NULL,
    @pObservationIDOut NVARCHAR(64) OUTPUT AS
BEGIN
    DECLARE @NewId NVARCHAR(64) = CONVERT(NVARCHAR(64), NEWID());
    INSERT INTO [HSS].[Observations] ([ObservationId], [PatientId], [EncounterId], [Height], [Weight], [Bmi],
        [BloodPressureSystolic], [BloodPressureDiastolic], [WaistCircumference], [SmokingStatus], [HeartRate], [Temperature])
    VALUES (@NewId, @pPatientID, @pEncounterID, @pHeight, @pWeight, @pBMI, @pBloodPressureSystolic, @pBloodPressureDiastolic,
        @pWaistCircumference, @pSmokingStatus, @pHeartRate, @pTemperature);
    SET @pObservationIDOut = @NewId;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetTemplateSchema] @pIdentifier NVARCHAR(64) AS
BEGIN
    SELECT [Name], [Caption], [Type] FROM [HSS].[TemplateSchemas] WHERE [Identifier] = @pIdentifier;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetEncounterSummary] @pPatientID INT, @pEncounterID INT, @pIdentifier NVARCHAR(64) AS
BEGIN
    SELECT [Fields] FROM [HSS].[EncounterSummaries] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Identifier] = @pIdentifier;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveSummary] @pPatientID INT, @pEncounterID INT, @pIdentifier NVARCHAR(64), @pFields NVARCHAR(MAX) AS
BEGIN
    IF EXISTS (SELECT 1 FROM [HSS].[EncounterSummaries] WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Identifier] = @pIdentifier)
        UPDATE [HSS].[EncounterSummaries] SET [Fields] = @pFields WHERE [PatientId] = @pPatientID AND [EncounterId] = @pEncounterID AND [Identifier] = @pIdentifier;
    ELSE
        INSERT INTO [HSS].[EncounterSummaries] ([PatientId], [EncounterId], [Identifier], [Fields]) VALUES (@pPatientID, @pEncounterID, @pIdentifier, @pFields);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetRecallCategories] @pGroup NVARCHAR(64) = NULL AS
BEGIN
    SELECT [CategoryId], [Name] FROM [HSS].[RecallCategories] WHERE (@pGroup IS NULL OR [GroupName] = @pGroup);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetRecalls] @pPatientID INT AS
BEGIN
    SELECT [RecallId], [CategoryId], [DueDate] FROM [HSS].[Recalls] WHERE [PatientId] = @pPatientID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveRecall] @pPatientID INT, @pCategoryID NVARCHAR(64) = NULL, @pDueDate DATE, @pRecallIDOut NVARCHAR(64) OUTPUT AS
BEGIN
    DECLARE @NewId NVARCHAR(64) = CONVERT(NVARCHAR(64), NEWID());
    DECLARE @ResolvedCategory NVARCHAR(64) = @pCategoryID;
    IF @ResolvedCategory IS NULL
        SELECT TOP 1 @ResolvedCategory = [CategoryId] FROM [HSS].[RecallCategories]; -- KARO-BR-22 default-per-group placeholder
    INSERT INTO [HSS].[Recalls] ([RecallId], [PatientId], [CategoryId], [DueDate]) VALUES (@NewId, @pPatientID, @ResolvedCategory, @pDueDate);
    SET @pRecallIDOut = @NewId;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetScreeningCodes] AS
BEGIN
    SELECT [Code], [Description] FROM [HSS].[ScreeningCodes];
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveScreeningCode] @pPatientID INT, @pEncounterID INT, @pCode NVARCHAR(32), @pValue NVARCHAR(256) = NULL AS
BEGIN
    INSERT INTO [HSS].[ScreeningResults] ([PatientId], [EncounterId], [Code], [Value]) VALUES (@pPatientID, @pEncounterID, @pCode, @pValue);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetProvider] @pPracticeLocationID NVARCHAR(64) = NULL AS
BEGIN
    SELECT [ProviderId], [Name], [PracticeLocationId] FROM [HSS].[Providers] WHERE (@pPracticeLocationID IS NULL OR [PracticeLocationId] = @pPracticeLocationID);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetSurgeryData] @pPracticeID NVARCHAR(64) AS
BEGIN
    SELECT [Name], [Address] FROM [HSS].[SurgeryData] WHERE [PracticeId] = @pPracticeID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspGetSessionData] @pPracticeID NVARCHAR(64) AS
BEGIN
    SELECT [SessionInfo] FROM [HSS].[SessionData] WHERE [PracticeId] = @pPracticeID;
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspFindInvoiceByNaturalKey] @pPatientID INT, @pServiceCode NVARCHAR(32), @pServiceDate DATE = NULL AS
BEGIN
    SELECT TOP 1 [InvoiceId], [ServiceCode], [ServiceName], [AmountInclGST], [Payee], [ServiceProvider], [ServiceDate], [PegasusReference], [ClaimShortCode]
    FROM [HSS].[Invoices] WHERE [PatientId] = @pPatientID AND [ServiceCode] = @pServiceCode AND (@pServiceDate IS NULL OR [ServiceDate] = @pServiceDate);
END
GO

CREATE OR ALTER PROCEDURE [HSS].[uspSaveInvoice]
    @pPatientID INT, @pServiceCode NVARCHAR(32), @pServiceName NVARCHAR(256) = NULL, @pAmountInclGST DECIMAL(10,2),
    @pPayee NVARCHAR(256) = NULL, @pServiceProvider NVARCHAR(256) = NULL, @pServiceDate DATE = NULL,
    @pPegasusReference NVARCHAR(128) = NULL, @pClaimShortCode NVARCHAR(64) = NULL, @pInvoiceIDOut NVARCHAR(64) OUTPUT AS
BEGIN
    DECLARE @NewId NVARCHAR(64) = CONVERT(NVARCHAR(64), NEWID());
    INSERT INTO [HSS].[Invoices] ([InvoiceId], [PatientId], [ServiceCode], [ServiceName], [AmountInclGST], [Payee], [ServiceProvider], [ServiceDate], [PegasusReference], [ClaimShortCode])
    VALUES (@NewId, @pPatientID, @pServiceCode, @pServiceName, @pAmountInclGST, @pPayee, @pServiceProvider, @pServiceDate, @pPegasusReference, @pClaimShortCode);
    SET @pInvoiceIDOut = @NewId;
END
GO

PRINT 'HEK Core API legacy-shaped schema build complete.';
GO
