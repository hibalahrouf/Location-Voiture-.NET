USE [aspnet-LocationVoiture.FrontOffice-636c94e8-11c7-4d7f-bfe5-101d67f8ce34]
GO

SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE, 
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Clients'
AND COLUMN_NAME IN ('IsEmailVerified', 'EmailVerificationToken')
GO
