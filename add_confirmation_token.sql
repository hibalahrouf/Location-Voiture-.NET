-- Migration: AddLocationConfirmationToken
-- Adds ConfirmationToken column to Locations table for email verification

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = N'ConfirmationToken' 
    AND Object_ID = Object_ID(N'Locations')
)
BEGIN
    ALTER TABLE [Locations] ADD [ConfirmationToken] NVARCHAR(32) NULL;
    PRINT 'Column ConfirmationToken added to Locations table.';
END
ELSE
BEGIN
    PRINT 'Column ConfirmationToken already exists.';
END
GO
