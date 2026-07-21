IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719151541_InitialCreate'
)
BEGIN
    CREATE TABLE [Practices] (
        [PracticeId] nvarchar(64) NOT NULL,
        [PracticeName] nvarchar(256) NOT NULL,
        [SourceSystem] nvarchar(16) NOT NULL,
        [DbServerHost] nvarchar(256) NOT NULL,
        [DbName] nvarchar(128) NOT NULL,
        [RowLevelSecurityEnabled] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Practices] PRIMARY KEY ([PracticeId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719151541_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719151541_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

