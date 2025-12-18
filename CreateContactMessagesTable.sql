-- Migration: Create ContactMessages table
-- Date: 2025-12-17
-- Purpose: Add missing ContactMessages table for contact form feature

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContactMessages')
BEGIN
    CREATE TABLE [dbo].[ContactMessages] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [DateSent] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    PRINT 'Table ContactMessages created successfully';
END
ELSE
BEGIN
    PRINT 'Table ContactMessages already exists';
END
GO

PRINT 'Migration completed successfully!';
GO
