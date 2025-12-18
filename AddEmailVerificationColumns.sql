-- Migration: Add EmailVerificationToken and IsEmailVerified columns to Clients table
-- This script adds the missing columns that are causing the errors in BackOffice

USE [aspnet-LocationVoiture.FrontOffice-636c94e8-11c7-4d7f-bfe5-101d67f8ce34]
GO

-- Check if IsEmailVerified column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Clients]') AND name = 'IsEmailVerified')
BEGIN
    ALTER TABLE [dbo].[Clients]
    ADD [IsEmailVerified] BIT NOT NULL DEFAULT 0;
    PRINT 'Column IsEmailVerified added successfully';
END
ELSE
BEGIN
    PRINT 'Column IsEmailVerified already exists';
END
GO

-- Check if EmailVerificationToken column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Clients]') AND name = 'EmailVerificationToken')
BEGIN
    ALTER TABLE [dbo].[Clients]
    ADD [EmailVerificationToken] NVARCHAR(MAX) NULL;
    PRINT 'Column EmailVerificationToken added successfully';
END
ELSE
BEGIN
    PRINT 'Column EmailVerificationToken already exists';
END
GO

-- Insert migration history record
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251213001433_AddEmailVerificationToClient')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251213001433_AddEmailVerificationToClient', N'9.0.10');
    PRINT 'Migration history record added';
END
GO

PRINT 'Migration completed successfully!';
GO
