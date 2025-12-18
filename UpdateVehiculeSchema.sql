-- Migration: Add missing columns to Vehicules table
-- Date: 2025-12-17
-- Purpose: Fix database schema mismatch for DateProchainEntretien, QuantiteTotal, QuantiteDisponible
-- Status: APPLIED SUCCESSFULLY

-- NOTE: This script was successfully executed on 2025-12-17
-- Database: aspnet-LocationVoiture.FrontOffice-636c94e8-11c7-4d7f-bfe5-101d67f8ce34
-- Results:
--   ✅ Added column DateProchainEntretien
--   ✅ Added column QuantiteTotal
--   ✅ Added column QuantiteDisponible
--   ✅ Dropped column Disponible (now computed property)

USE [LocationVoitureDB]
GO

-- Check if columns already exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Vehicules]') AND name = 'DateProchainEntretien')
BEGIN
    ALTER TABLE [dbo].[Vehicules]
    ADD [DateProchainEntretien] datetime2(7) NULL;
    PRINT 'Added column DateProchainEntretien';
END
ELSE
BEGIN
    PRINT 'Column DateProchainEntretien already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Vehicules]') AND name = 'QuantiteTotal')
BEGIN
    ALTER TABLE [dbo].[Vehicules]
    ADD [QuantiteTotal] int NOT NULL DEFAULT 1;
    PRINT 'Added column QuantiteTotal';
END
ELSE
BEGIN
    PRINT 'Column QuantiteTotal already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Vehicules]') AND name = 'QuantiteDisponible')
BEGIN
    ALTER TABLE [dbo].[Vehicules]
    ADD [QuantiteDisponible] int NOT NULL DEFAULT 1;
    PRINT 'Added column QuantiteDisponible';
END
ELSE
BEGIN
    PRINT 'Column QuantiteDisponible already exists';
END
GO

-- Drop the old Disponible column if it exists (it's now a computed property)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Vehicules]') AND name = 'Disponible')
BEGIN
    ALTER TABLE [dbo].[Vehicules]
    DROP COLUMN [Disponible];
    PRINT 'Dropped column Disponible (now computed property)';
END
ELSE
BEGIN
    PRINT 'Column Disponible does not exist';
END
GO

PRINT 'Migration completed successfully!';
GO
