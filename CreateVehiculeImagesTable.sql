-- Migration: Create VehiculeImages table
-- Date: 2025-12-17
-- Purpose: Add missing VehiculeImages table for vehicle image gallery feature

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VehiculeImages')
BEGIN
    CREATE TABLE [dbo].[VehiculeImages] (
        [ImageID] int IDENTITY(1,1) NOT NULL,
        [ImagePath] nvarchar(max) NOT NULL,
        [IsPrimary] bit NOT NULL DEFAULT 0,
        [VehiculeID] int NOT NULL,
        CONSTRAINT [PK_VehiculeImages] PRIMARY KEY CLUSTERED ([ImageID] ASC),
        CONSTRAINT [FK_VehiculeImages_Vehicules_VehiculeID] FOREIGN KEY([VehiculeID])
            REFERENCES [dbo].[Vehicules] ([VehiculeID])
            ON DELETE CASCADE
    );
    
    -- Create index on VehiculeID for better query performance
    CREATE NONCLUSTERED INDEX [IX_VehiculeImages_VehiculeID] 
    ON [dbo].[VehiculeImages]([VehiculeID] ASC);
    
    PRINT 'Table VehiculeImages created successfully';
END
ELSE
BEGIN
    PRINT 'Table VehiculeImages already exists';
END
GO

PRINT 'Migration completed successfully!';
GO
