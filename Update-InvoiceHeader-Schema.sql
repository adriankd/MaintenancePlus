-- =============================================
-- InvoiceHeader Table Schema Update Script
-- Vehicle Maintenance Invoice Processing System
-- =============================================
-- Date: August 14, 2025
-- Purpose: Add ExtractedData and ConfidenceScore fields to existing InvoiceHeader table

USE VehicleMaintenance;
GO

-- =============================================
-- Check if columns already exist before adding
-- =============================================

PRINT 'Starting InvoiceHeader table schema update...';

-- Check and add ExtractedData column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InvoiceHeader') 
    AND name = 'ExtractedData'
)
BEGIN
    PRINT 'Adding ExtractedData column...';
    ALTER TABLE dbo.InvoiceHeader 
    ADD ExtractedData NVARCHAR(MAX) NULL;
    PRINT 'ExtractedData column added successfully.';
END
ELSE
BEGIN
    PRINT 'ExtractedData column already exists, skipping...';
END

-- Check and add ConfidenceScore column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InvoiceHeader') 
    AND name = 'ConfidenceScore'
)
BEGIN
    PRINT 'Adding ConfidenceScore column...';
    ALTER TABLE dbo.InvoiceHeader 
    ADD ConfidenceScore DECIMAL(5,2) NULL;
    PRINT 'ConfidenceScore column added successfully.';
END
ELSE
BEGIN
    PRINT 'ConfidenceScore column already exists, skipping...';
END

-- =============================================
-- Add comments/descriptions to new columns
-- =============================================

-- Add extended properties for documentation
IF NOT EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.InvoiceHeader') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoiceHeader') AND name = 'ExtractedData')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description',
        @value = N'JSON representation of raw data extracted from invoice by OCR service',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE', @level1name = N'InvoiceHeader',
        @level2type = N'COLUMN', @level2name = N'ExtractedData';
    PRINT 'Added description for ExtractedData column.';
END

IF NOT EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.InvoiceHeader') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoiceHeader') AND name = 'ConfidenceScore')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description',
        @value = N'Overall confidence score (0-100) for OCR data extraction accuracy',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE', @level1name = N'InvoiceHeader',
        @level2type = N'COLUMN', @level2name = N'ConfidenceScore';
    PRINT 'Added description for ConfidenceScore column.';
END

-- =============================================
-- Add constraints for data validation
-- =============================================

-- Add check constraint for ConfidenceScore range (0-100)
IF NOT EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE parent_object_id = OBJECT_ID('dbo.InvoiceHeader') 
    AND name = 'CK_InvoiceHeader_ConfidenceScore'
)
BEGIN
    PRINT 'Adding check constraint for ConfidenceScore...';
    ALTER TABLE dbo.InvoiceHeader 
    ADD CONSTRAINT CK_InvoiceHeader_ConfidenceScore 
    CHECK (ConfidenceScore IS NULL OR (ConfidenceScore >= 0 AND ConfidenceScore <= 100));
    PRINT 'Check constraint for ConfidenceScore added successfully.';
END
ELSE
BEGIN
    PRINT 'Check constraint for ConfidenceScore already exists, skipping...';
END

-- =============================================
-- Create index for ConfidenceScore (for filtering low confidence records)
-- =============================================

IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.InvoiceHeader') 
    AND name = 'IX_InvoiceHeader_ConfidenceScore'
)
BEGIN
    PRINT 'Creating index on ConfidenceScore...';
    CREATE NONCLUSTERED INDEX IX_InvoiceHeader_ConfidenceScore 
        ON dbo.InvoiceHeader (ConfidenceScore)
        WHERE ConfidenceScore IS NOT NULL;
    PRINT 'Index on ConfidenceScore created successfully.';
END
ELSE
BEGIN
    PRINT 'Index on ConfidenceScore already exists, skipping...';
END

-- =============================================
-- Verify the schema update
-- =============================================

PRINT '';
PRINT 'Schema update completed. Verifying table structure...';

-- Display current table schema
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    NUMERIC_PRECISION,
    NUMERIC_SCALE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'InvoiceHeader' 
ORDER BY ORDINAL_POSITION;

-- Count existing records
DECLARE @RecordCount INT;
SELECT @RecordCount = COUNT(*) FROM dbo.InvoiceHeader;
PRINT '';
PRINT 'Current record count: ' + CAST(@RecordCount AS NVARCHAR(10));

-- =============================================
-- Sample update statement for existing records
-- =============================================

PRINT '';
PRINT 'Sample usage for updating ExtractedData and ConfidenceScore:';
PRINT '-- UPDATE dbo.InvoiceHeader';
PRINT '-- SET ExtractedData = ''{"vendor":"Auto Shop","extractedFields":["total","date","vehicle"]}'',';
PRINT '--     ConfidenceScore = 95.5';
PRINT '-- WHERE InvoiceID = 1;';

PRINT '';
PRINT '=============================================';
PRINT 'InvoiceHeader table schema update completed!';
PRINT '=============================================';
