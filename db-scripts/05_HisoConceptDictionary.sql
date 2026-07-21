-- ============================================================================
-- HEK Core API — HISO real concept dictionary (confirmed from real legacy source,
-- legacy-reference/Hiso/DAL/DBMessages.cs's GetHisoConceptDetail ->
-- [Hiso].[UspGetHisoConcepts]). Not part of the original inferred schema build
-- (db-scripts/03_LegacySchema_Build.sql) - added once the real HISO source was
-- supplied and the real concept-mapping engine was ported.
-- Target: HekLegacyPmsDev (dev/test only). Idempotent: safe to re-run.
-- ============================================================================

-- Real column confirmed from legacy Mapper.cs's HealthLinkSession.GetByGUID (dt.Rows[0]["EDIAccount"]) -
-- needed for getDeliveryOptions's real senderAccount-from-session logic.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Appointment].[tblHealthLinkSession]') AND name = 'EDIAccount')
ALTER TABLE [Appointment].[tblHealthLinkSession] ADD EDIAccount NVARCHAR(64) NULL;
GO

IF OBJECT_ID(N'[Hiso].[HisoConceptDictionary]') IS NULL
CREATE TABLE [Hiso].[HisoConceptDictionary] (
    [HisoConceptID] NVARCHAR(64) NOT NULL,
    [ConceptName] NVARCHAR(128) NOT NULL,
    [HisoGroupID] NVARCHAR(64) NULL,
    [HisoConceptDetailName] NVARCHAR(128) NOT NULL,
    [HisoCategory] NVARCHAR(128) NULL,
    [HisoID] NVARCHAR(64) NULL,
    [Defination] NVARCHAR(256) NULL,
    [HisoDataType] NVARCHAR(32) NULL,
    [TableName] NVARCHAR(128) NULL,
    [TableAlias] NVARCHAR(64) NULL,
    [TableColumn] NVARCHAR(128) NULL,
    [ColumnAlias] NVARCHAR(128) NULL,
    [IsActive] NVARCHAR(8) NULL,
    [CodingSystem] NVARCHAR(64) NULL,
    [QualifierID] NVARCHAR(64) NULL,
    [QualifierName] NVARCHAR(128) NULL,
    [ProcedureName] NVARCHAR(128) NOT NULL,
    [IsFixed] BIT NOT NULL DEFAULT 0,
    [Sort] NVARCHAR(16) NULL
);
GO

CREATE OR ALTER PROCEDURE [Hiso].[UspGetHisoConcepts]
AS
BEGIN
    SELECT HisoConceptID, ConceptName, HisoGroupID, HisoConceptDetailName, HisoCategory, HisoID,
           Defination, HisoDataType, TableName, TableAlias, TableColumn, ColumnAlias, IsActive,
           CodingSystem, QualifierID, QualifierName, ProcedureName, IsFixed, Sort
    FROM [Hiso].[HisoConceptDictionary];
END
GO

-- Task.uspAddTaskExternal and Billing.usptblConcept_GetByCode - real procedure names confirmed from
-- legacy-reference/Hiso/Task.cs, needed by processAction's "addTask" branch.
IF SCHEMA_ID(N'Billing') IS NULL EXEC('CREATE SCHEMA [Billing]');
GO

IF OBJECT_ID(N'[Billing].[Concepts]') IS NULL
CREATE TABLE [Billing].[Concepts] (
    [ReadCode] NVARCHAR(32) NOT NULL PRIMARY KEY,
    [FullySpecifiedName] NVARCHAR(256) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE [Billing].[usptblConcept_GetByCode]
    @SNOMEDID NVARCHAR(32) = NULL,
    @ReadCode NVARCHAR(32) = NULL
AS
BEGIN
    SELECT TOP 1 FullySpecifiedName FROM [Billing].[Concepts] WHERE ReadCode = @ReadCode;
END
GO

IF NOT EXISTS (SELECT 1 FROM [Billing].[Concepts] WHERE ReadCode = N'TESTCODE1')
INSERT INTO [Billing].[Concepts] (ReadCode, FullySpecifiedName) VALUES (N'TESTCODE1', N'Test Follow-up Task');
GO

IF OBJECT_ID(N'[Task].[Tasks]') IS NULL
CREATE TABLE [Task].[Tasks] (
    [TaskId] INT IDENTITY(1,1) PRIMARY KEY,
    [TaskSubject] NVARCHAR(512) NOT NULL,
    [ProviderId] NVARCHAR(64) NULL,
    [PracticeId] NVARCHAR(64) NULL,
    [PatientId] NVARCHAR(64) NULL,
    [LastStatusId] NVARCHAR(32) NULL,
    [CreatedAtUtc] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE OR ALTER PROCEDURE [Task].[uspAddTaskExternal]
    @pTaskSubject NVARCHAR(512), @pProviderID NVARCHAR(64), @pStartDate DATETIME, @pEndDate DATETIME,
    @pPracticeID NVARCHAR(64), @pIsShowOnTimeline BIT, @pACC45ID NVARCHAR(64) = NULL, @pTaskTypeID NVARCHAR(32) = NULL,
    @pIsConfidential BIT, @pUpdatedBy NVARCHAR(64) = NULL, @pProirtyID NVARCHAR(32) = NULL, @pLastStatusID NVARCHAR(32) = NULL,
    @pPatientID NVARCHAR(64), @pUserLoggingID INT, @pAddtoPrompt BIT, @pTaskMedicationIds NVARCHAR(256) = NULL,
    @pDataSourceID INT, @pPharmacyID NVARCHAR(64) = NULL, @pOtherPharmacy NVARCHAR(64) = NULL, @pIsSelfPickup BIT,
    @pPrescriberProviderID NVARCHAR(64) = NULL, @pPrimaryAssignFrom NVARCHAR(64) = NULL, @pMasterServiceSubServiceID NVARCHAR(64) = NULL,
    @pPrice DECIMAL(18,2) = NULL, @pNoCharged BIT
AS
BEGIN
    INSERT INTO [Task].[Tasks] (TaskSubject, ProviderId, PracticeId, PatientId, LastStatusId)
    VALUES (@pTaskSubject, @pProviderID, @pPracticeID, @pPatientID, @pLastStatusID);
END
GO

-- Minimal real seed: one row per real result COLUMN of Hiso.uspGetPatient_Demographics (matching how
-- the real concept-mapping engine actually resolves fields - one <field conceptName="X"/> per column,
-- not one field per "concept" umbrella name), enough to prove the real engine (conceptName match ->
-- real procedure -> real execution -> real field fill) end-to-end against already-verified data.
IF NOT EXISTS (SELECT 1 FROM [Hiso].[HisoConceptDictionary] WHERE HisoConceptDetailName = N'FirstName')
BEGIN
    INSERT INTO [Hiso].[HisoConceptDictionary]
        (HisoConceptID, ConceptName, HisoGroupID, HisoConceptDetailName, HisoCategory, HisoID, Defination,
         HisoDataType, TableName, TableAlias, TableColumn, ColumnAlias, IsActive, CodingSystem,
         QualifierID, QualifierName, ProcedureName, IsFixed, Sort)
    VALUES
        (N'1', N'Demographics', NULL, N'FirstName', N'Patient', N'1', N'Patient first name', N'String', NULL, NULL, NULL, NULL, N'1', NULL, NULL, NULL, N'Hiso.uspGetPatient_Demographics', 0, N'1'),
        (N'2', N'Demographics', NULL, N'LastName', N'Patient', N'2', N'Patient last name', N'String', NULL, NULL, NULL, NULL, N'1', NULL, NULL, NULL, N'Hiso.uspGetPatient_Demographics', 0, N'2'),
        (N'3', N'Demographics', NULL, N'DateOfBirth', N'Patient', N'3', N'Patient date of birth', N'Date', NULL, NULL, NULL, NULL, N'1', NULL, NULL, NULL, N'Hiso.uspGetPatient_Demographics', 0, N'3');
END
GO
