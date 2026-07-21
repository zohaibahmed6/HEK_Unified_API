-- Run this AFTER 01_TenantRegistry_InitialCreate.sql has been applied to the TenantRegistry database.
-- Registers one practice pointing at the "other server" (dbserver-local.fff / PMS_NZ_V2), so
-- ILegacyPracticeConnectionResolver can route requests scoped to this practiceId there.
-- The DbServerHost value here MUST match the suffix used in the Legacy:DbCredentials:{DbServerHost}
-- config key (see src/Api/appsettings.Development.local.json).

IF NOT EXISTS (SELECT 1 FROM [Practices] WHERE [PracticeId] = N'TEST-PRACTICE-001')
BEGIN
    INSERT INTO [Practices]
        ([PracticeId], [PracticeName], [SourceSystem], [DbServerHost], [DbName], [RowLevelSecurityEnabled], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES
        (N'TEST-PRACTICE-001', N'Local Test Practice', N'Karo', N'dbserver-local.fff', N'PMS_NZ_V2', 0, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
END;
GO
