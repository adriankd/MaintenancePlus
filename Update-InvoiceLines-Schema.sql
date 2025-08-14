-- =============================================
-- InvoiceLines Table Schema Update Script
-- Vehicle Maintenance Invoice Processing System
-- =============================================
-- Date: August 14, 2025
-- Purpose: Add PartNumber, Category, ConfidenceScore, and LineNumber fields to existing InvoiceLines table

USE VehicleMaintenance;
GO

-- =============================================
-- Check if columns already exist before adding
-- =============================================

PRINT 'Starting InvoiceLines table schema update...';

-- Check and add LineNumber column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'LineNumber'
)
BEGIN
    PRINT 'Adding LineNumber column...';
    ALTER TABLE dbo.InvoiceLines 
    ADD LineNumber INT NOT NULL DEFAULT 1;
    PRINT 'LineNumber column added successfully.';
END
ELSE
BEGIN
    PRINT 'LineNumber column already exists, skipping...';
END

-- Check and add PartNumber column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'PartNumber'
)
BEGIN
    PRINT 'Adding PartNumber column...';
    ALTER TABLE dbo.InvoiceLines 
    ADD PartNumber NVARCHAR(100) NULL;
    PRINT 'PartNumber column added successfully.';
END
ELSE
BEGIN
    PRINT 'PartNumber column already exists, skipping...';
END

-- Check and add Category column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'Category'
)
BEGIN
    PRINT 'Adding Category column...';
    ALTER TABLE dbo.InvoiceLines 
    ADD Category NVARCHAR(100) NULL;
    PRINT 'Category column added successfully.';
END
ELSE
BEGIN
    PRINT 'Category column already exists, skipping...';
END

-- Check and add ConfidenceScore column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'ConfidenceScore'
)
BEGIN
    PRINT 'Adding ConfidenceScore column...';
    ALTER TABLE dbo.InvoiceLines 
    ADD ConfidenceScore DECIMAL(5,2) NULL;
    PRINT 'ConfidenceScore column added successfully.';
END
ELSE
BEGIN
    PRINT 'ConfidenceScore column already exists, skipping...';
END

-- Check and add CreatedAt column (for consistency with InvoiceHeader)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'CreatedAt'
)
BEGIN
    PRINT 'Adding CreatedAt column...';
    ALTER TABLE dbo.InvoiceLines 
    ADD CreatedAt DATETIME NULL DEFAULT GETDATE();
    PRINT 'CreatedAt column added successfully.';
END
ELSE
BEGIN
    PRINT 'CreatedAt column already exists, skipping...';
END

-- =============================================
-- Update existing records with default LineNumber values
-- =============================================

PRINT 'Updating existing records with sequential LineNumber values...';

-- Update LineNumber with row numbers for existing records (grouped by InvoiceID)
WITH NumberedLines AS (
    SELECT 
        LineID,
        ROW_NUMBER() OVER (PARTITION BY InvoiceID ORDER BY LineID) AS RowNum
    FROM dbo.InvoiceLines
    WHERE LineNumber = 1  -- Only update records with default value
)
UPDATE il
SET LineNumber = nl.RowNum
FROM dbo.InvoiceLines il
INNER JOIN NumberedLines nl ON il.LineID = nl.LineID;

PRINT 'LineNumber values updated for existing records.';

-- =============================================
-- Add constraints and validation
-- =============================================

-- Add check constraint for ConfidenceScore range (0-100)
IF NOT EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE parent_object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'CK_InvoiceLines_ConfidenceScore'
)
BEGIN
    PRINT 'Adding check constraint for ConfidenceScore...';
    ALTER TABLE dbo.InvoiceLines 
    ADD CONSTRAINT CK_InvoiceLines_ConfidenceScore 
    CHECK (ConfidenceScore IS NULL OR (ConfidenceScore >= 0 AND ConfidenceScore <= 100));
    PRINT 'Check constraint for ConfidenceScore added successfully.';
END
ELSE
BEGIN
    PRINT 'Check constraint for ConfidenceScore already exists, skipping...';
END

-- Add check constraint for LineNumber (must be positive)
IF NOT EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE parent_object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'CK_InvoiceLines_LineNumber'
)
BEGIN
    PRINT 'Adding check constraint for LineNumber...';
    ALTER TABLE dbo.InvoiceLines 
    ADD CONSTRAINT CK_InvoiceLines_LineNumber 
    CHECK (LineNumber > 0);
    PRINT 'Check constraint for LineNumber added successfully.';
END
ELSE
BEGIN
    PRINT 'Check constraint for LineNumber already exists, skipping...';
END

-- =============================================
-- Add column descriptions
-- =============================================

-- Add extended properties for documentation
IF NOT EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.InvoiceLines') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoiceLines') AND name = 'LineNumber')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description',
        @value = N'Sequential line number within the invoice for ordering',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE', @level1name = N'InvoiceLines',
        @level2type = N'COLUMN', @level2name = N'LineNumber';
    PRINT 'Added description for LineNumber column.';
END

IF NOT EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.InvoiceLines') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoiceLines') AND name = 'PartNumber')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description',
        @value = N'Manufacturer part number (extracted from invoice or description)',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE', @level1name = N'InvoiceLines',
        @level2type = N'COLUMN', @level2name = N'PartNumber';
    PRINT 'Added description for PartNumber column.';
END

IF NOT EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.InvoiceLines') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoiceLines') AND name = 'Category')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description',
        @value = N'Line item category: Parts, Labor, Tax, Supplies, Misc, etc.',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE', @level1name = N'InvoiceLines',
        @level2type = N'COLUMN', @level2name = N'Category';
    PRINT 'Added description for Category column.';
END

IF NOT EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.InvoiceLines') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoiceLines') AND name = 'ConfidenceScore')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description',
        @value = N'Line-level confidence score (0-100) for OCR data extraction accuracy',
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE', @level1name = N'InvoiceLines',
        @level2type = N'COLUMN', @level2name = N'ConfidenceScore';
    PRINT 'Added description for ConfidenceScore column.';
END

-- =============================================
-- Create indexes for performance
-- =============================================

-- Index on InvoiceID and LineNumber for ordered retrieval
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'IX_InvoiceLines_InvoiceID_LineNumber'
)
BEGIN
    PRINT 'Creating composite index on InvoiceID and LineNumber...';
    CREATE NONCLUSTERED INDEX IX_InvoiceLines_InvoiceID_LineNumber 
        ON dbo.InvoiceLines (InvoiceID, LineNumber);
    PRINT 'Composite index created successfully.';
END
ELSE
BEGIN
    PRINT 'Composite index on InvoiceID and LineNumber already exists, skipping...';
END

-- Index on Category for filtering and reporting
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'IX_InvoiceLines_Category'
)
BEGIN
    PRINT 'Creating index on Category...';
    CREATE NONCLUSTERED INDEX IX_InvoiceLines_Category 
        ON dbo.InvoiceLines (Category)
        WHERE Category IS NOT NULL;
    PRINT 'Category index created successfully.';
END
ELSE
BEGIN
    PRINT 'Category index already exists, skipping...';
END

-- Index on PartNumber for part lookups
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.InvoiceLines') 
    AND name = 'IX_InvoiceLines_PartNumber'
)
BEGIN
    PRINT 'Creating index on PartNumber...';
    CREATE NONCLUSTERED INDEX IX_InvoiceLines_PartNumber 
        ON dbo.InvoiceLines (PartNumber)
        WHERE PartNumber IS NOT NULL;
    PRINT 'PartNumber index created successfully.';
END
ELSE
BEGIN
    PRINT 'PartNumber index already exists, skipping...';
END

-- =============================================
-- Update Quantity data type to match schema
-- =============================================

-- Check if Quantity column needs to be changed from INT to DECIMAL
DECLARE @QuantityDataType NVARCHAR(50);
SELECT @QuantityDataType = DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'InvoiceLines' AND COLUMN_NAME = 'Quantity';

IF @QuantityDataType = 'int'
BEGIN
    PRINT 'Converting Quantity column from INT to DECIMAL(10,2)...';
    -- Note: This requires careful handling if there's existing data
    -- For safety, we'll add a comment suggesting manual review
    PRINT 'WARNING: Quantity column is currently INT but schema expects DECIMAL(10,2)';
    PRINT 'Consider manually converting: ALTER TABLE dbo.InvoiceLines ALTER COLUMN Quantity DECIMAL(10,2) NOT NULL;';
    PRINT 'Review existing data first to ensure no precision loss.';
END
ELSE
BEGIN
    PRINT 'Quantity column data type is already correct.';
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
WHERE TABLE_NAME = 'InvoiceLines' 
ORDER BY ORDINAL_POSITION;

-- Count existing records
DECLARE @RecordCount INT;
SELECT @RecordCount = COUNT(*) FROM dbo.InvoiceLines;
PRINT '';
PRINT 'Current record count: ' + CAST(@RecordCount AS NVARCHAR(10));

-- Show sample of updated data
IF @RecordCount > 0
BEGIN
    PRINT '';
    PRINT 'Sample of updated records:';
    SELECT TOP 5 
        LineID, 
        InvoiceID, 
        LineNumber, 
        Description, 
        Category, 
        PartNumber,
        ConfidenceScore
    FROM dbo.InvoiceLines 
    ORDER BY InvoiceID, LineNumber;
END

-- =============================================
-- Sample usage examples
-- =============================================

PRINT '';
PRINT 'Sample usage for updating new fields:';
PRINT '-- INSERT new line with all fields:';
PRINT '-- INSERT INTO dbo.InvoiceLines (InvoiceID, LineNumber, Description, UnitCost, Quantity, TotalLineCost, PartNumber, Category, ConfidenceScore)';
PRINT '-- VALUES (1, 1, ''Oil Filter'', 25.00, 1, 25.00, ''PF-52'', ''Parts'', 98.5);';
PRINT '';
PRINT '-- UPDATE existing lines with categories:';
PRINT '-- UPDATE dbo.InvoiceLines SET Category = ''Parts'', ConfidenceScore = 95.0 WHERE Description LIKE ''%filter%'';';
PRINT '-- UPDATE dbo.InvoiceLines SET Category = ''Labor'', ConfidenceScore = 90.0 WHERE Description LIKE ''%labor%'';';

PRINT '';
PRINT '=============================================';
PRINT 'InvoiceLines table schema update completed!';
PRINT '=============================================';
