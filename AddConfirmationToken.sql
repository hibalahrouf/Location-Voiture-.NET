-- Migration: Add ConfirmationToken column to Locations table
-- Date: 2025-12-17
-- Purpose: Fix database schema mismatch for ConfirmationToken in Locations

-- Check if column already exists before adding it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Locations]') AND name = 'ConfirmationToken')
BEGIN
    ALTER TABLE [dbo].[Locations]
    ADD [ConfirmationToken] nvarchar(max) NULL;
    PRINT 'Added column ConfirmationToken';
END
ELSE
BEGIN
    PRINT 'Column ConfirmationToken already exists';
END
GO

PRINT 'Migration completed successfully!';
GO
