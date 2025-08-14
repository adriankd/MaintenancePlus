# Azure SQL Database Schema
## Vehicle Maintenance Invoice Processing System

### Database: VehicleMaintenance
### Version: 1.0
### Date: August 14, 2025

---

## Complete Database Setup Script

```sql
-- =============================================
-- Vehicle Maintenance Invoice Processing System
-- Database Schema Setup Script
-- =============================================

-- Create Database (if running on SQL Server, not Azure SQL)
-- Note: Azure SQL Database is created through Azure portal/CLI
-- USE master;
-- GO
-- CREATE DATABASE VehicleMaintenance;
-- GO

-- Switch to the VehicleMaintenance database
USE VehicleMaintenance;
GO

-- =============================================
-- Drop existing objects (for clean reinstall)
-- =============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceLines')
    DROP TABLE InvoiceLines;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceHeader')
    DROP TABLE InvoiceHeader;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessingLog')
    DROP TABLE ProcessingLog;
GO

-- =============================================
-- Create Tables
-- =============================================

-- Invoice Header Table
CREATE TABLE InvoiceHeader (
    InvoiceID INT IDENTITY(1,1) PRIMARY KEY,
    VehicleID NVARCHAR(50) NOT NULL,
    Odometer INT NULL,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    InvoiceDate DATE NOT NULL,
    TotalCost DECIMAL(18,2) NOT NULL,
    TotalPartsCost DECIMAL(18,2) NOT NULL,
    TotalLaborCost DECIMAL(18,2) NOT NULL,
    BlobFileUrl NVARCHAR(255) NOT NULL,
    ExtractedData NVARCHAR(MAX) NULL, -- JSON representation of extracted data
    ConfidenceScore DECIMAL(5,2) NULL, -- Overall confidence score 0-100
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Invoice Lines Table
-- Note: Column types updated to DECIMAL for proper financial calculations
-- Previously: UnitCost, Quantity, TotalLineCost were incorrectly set as INT
-- Fixed: 2025-08-14 - Changed to DECIMAL types to prevent casting errors
CREATE TABLE InvoiceLines (
    LineID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceID INT NOT NULL,
    LineNumber INT NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,        -- Changed from INT to DECIMAL for currency
    Quantity DECIMAL(10,2) NOT NULL,        -- Changed from INT to DECIMAL for fractional quantities
    TotalLineCost DECIMAL(18,2) NOT NULL,   -- Changed from INT to DECIMAL for currency
    PartNumber NVARCHAR(100) NULL,
    Category NVARCHAR(100) NULL, -- Parts, Labor, Tax, etc.
    ConfidenceScore DECIMAL(5,2) NULL, -- Line-level confidence score
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_InvoiceLines_InvoiceHeader 
        FOREIGN KEY (InvoiceID) REFERENCES InvoiceHeader(InvoiceID) ON DELETE CASCADE
);
GO

-- Processing Log Table (for audit and troubleshooting)
CREATE TABLE ProcessingLog (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceID INT NULL,
    ProcessingStep NVARCHAR(100) NOT NULL, -- Upload, OCR, Extraction, Validation, Storage
    Status NVARCHAR(20) NOT NULL, -- Started, Completed, Failed
    Message NVARCHAR(MAX) NULL,
    ErrorDetails NVARCHAR(MAX) NULL,
    ProcessingTime INT NULL, -- Processing time in milliseconds
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ProcessingLog_InvoiceHeader 
        FOREIGN KEY (InvoiceID) REFERENCES InvoiceHeader(InvoiceID) ON DELETE CASCADE
);
GO

-- =============================================
-- Create Indexes for Performance
-- =============================================

-- Invoice Header Indexes
CREATE NONCLUSTERED INDEX IX_InvoiceHeader_VehicleID 
    ON InvoiceHeader (VehicleID);
GO

CREATE NONCLUSTERED INDEX IX_InvoiceHeader_InvoiceDate 
    ON InvoiceHeader (InvoiceDate);
GO

CREATE NONCLUSTERED INDEX IX_InvoiceHeader_InvoiceNumber 
    ON InvoiceHeader (InvoiceNumber);
GO

-- Invoice Lines Indexes
CREATE NONCLUSTERED INDEX IX_InvoiceLines_InvoiceID 
    ON InvoiceLines (InvoiceID);
GO

CREATE NONCLUSTERED INDEX IX_InvoiceLines_Category 
    ON InvoiceLines (Category);
GO

-- Processing Log Indexes
CREATE NONCLUSTERED INDEX IX_ProcessingLog_InvoiceID 
    ON ProcessingLog (InvoiceID);
GO

CREATE NONCLUSTERED INDEX IX_ProcessingLog_CreatedAt 
    ON ProcessingLog (CreatedAt);
GO

-- =============================================
-- Create Constraints
-- =============================================

-- Unique constraint on invoice number (business rule)
ALTER TABLE InvoiceHeader 
ADD CONSTRAINT UQ_InvoiceHeader_InvoiceNumber UNIQUE (InvoiceNumber);
GO

-- Check constraints for data validation
ALTER TABLE InvoiceHeader 
ADD CONSTRAINT CK_InvoiceHeader_TotalCost CHECK (TotalCost >= 0);
GO

ALTER TABLE InvoiceHeader 
ADD CONSTRAINT CK_InvoiceHeader_TotalPartsCost CHECK (TotalPartsCost >= 0);
GO

ALTER TABLE InvoiceHeader 
ADD CONSTRAINT CK_InvoiceHeader_TotalLaborCost CHECK (TotalLaborCost >= 0);
GO

ALTER TABLE InvoiceHeader 
ADD CONSTRAINT CK_InvoiceHeader_Odometer CHECK (Odometer >= 0);
GO

ALTER TABLE InvoiceLines 
ADD CONSTRAINT CK_InvoiceLines_UnitCost CHECK (UnitCost >= 0);
GO

ALTER TABLE InvoiceLines 
ADD CONSTRAINT CK_InvoiceLines_Quantity CHECK (Quantity > 0);
GO

ALTER TABLE InvoiceLines 
ADD CONSTRAINT CK_InvoiceLines_TotalLineCost CHECK (TotalLineCost >= 0);
GO

-- =============================================
-- Schema Migration Notes
-- =============================================

-- Schema Fix Applied: 2025-08-14
-- Issue: InvoiceLines table columns were initially created as INT instead of DECIMAL
-- This caused InvalidCastException errors when Entity Framework tried to cast INT to DECIMAL
-- 
-- Fixed columns:
-- - Quantity: INT → DECIMAL(10,2) 
-- - UnitCost: INT → DECIMAL(18,2)
-- - TotalLineCost: INT → DECIMAL(18,2)
--
-- Migration SQL (already applied):
-- ALTER TABLE InvoiceLines ALTER COLUMN Quantity DECIMAL(10,2);
-- ALTER TABLE InvoiceLines ALTER COLUMN UnitCost DECIMAL(18,2);
-- ALTER TABLE InvoiceLines ALTER COLUMN TotalLineCost DECIMAL(18,2);

-- =============================================
-- Create Application User and Permissions
-- =============================================

-- Create login (only needed if not using Azure SQL Authentication)
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'vehicle-maintenance-app')
BEGIN
    CREATE LOGIN [vehicle-maintenance-app] WITH PASSWORD = 'AppPassword123!';
END
GO

-- Create user in database
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'vehicle-maintenance-app')
BEGIN
    CREATE USER [vehicle-maintenance-app] FOR LOGIN [vehicle-maintenance-app];
END
GO

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER [vehicle-maintenance-app];
ALTER ROLE db_datawriter ADD MEMBER [vehicle-maintenance-app];
GRANT EXECUTE ON SCHEMA::dbo TO [vehicle-maintenance-app];
GO

-- =============================================
-- Create Views for Common Queries
-- =============================================

-- View for complete invoice details with line items
CREATE VIEW vw_InvoiceDetails AS
SELECT 
    ih.InvoiceID,
    ih.VehicleID,
    ih.Odometer,
    ih.InvoiceNumber,
    ih.InvoiceDate,
    ih.TotalCost,
    ih.TotalPartsCost,
    ih.TotalLaborCost,
    ih.BlobFileUrl,
    ih.ExtractedData,
    ih.ConfidenceScore AS HeaderConfidenceScore,
    ih.CreatedAt,
    il.LineID,
    il.LineNumber,
    il.Description,
    il.UnitCost,
    il.Quantity,
    il.TotalLineCost,
    il.PartNumber,
    il.Category,
    il.ConfidenceScore AS LineConfidenceScore
FROM InvoiceHeader ih
LEFT JOIN InvoiceLines il ON ih.InvoiceID = il.InvoiceID;
GO

-- View for invoice summary (header only)
CREATE VIEW vw_InvoiceSummary AS
SELECT 
    InvoiceID,
    VehicleID,
    InvoiceNumber,
    InvoiceDate,
    TotalCost,
    TotalPartsCost,
    TotalLaborCost,
    ConfidenceScore,
    CreatedAt,
    (SELECT COUNT(*) FROM InvoiceLines WHERE InvoiceID = ih.InvoiceID) AS LineItemCount
FROM InvoiceHeader ih;
GO

-- =============================================
-- Create Stored Procedures
-- =============================================

-- Procedure to get invoice by ID with line items
CREATE PROCEDURE sp_GetInvoiceById
    @InvoiceID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get header information
    SELECT 
        InvoiceID,
        VehicleID,
        Odometer,
        InvoiceNumber,
        InvoiceDate,
        TotalCost,
        TotalPartsCost,
        TotalLaborCost,
        BlobFileUrl,
        ExtractedData,
        ConfidenceScore,
        CreatedAt
    FROM InvoiceHeader 
    WHERE InvoiceID = @InvoiceID;
    
    -- Get line items
    SELECT 
        LineID,
        LineNumber,
        Description,
        UnitCost,
        Quantity,
        TotalLineCost,
        PartNumber,
        Category,
        ConfidenceScore
    FROM InvoiceLines 
    WHERE InvoiceID = @InvoiceID
    ORDER BY LineNumber;
END
GO

-- Procedure to get invoices by vehicle ID
CREATE PROCEDURE sp_GetInvoicesByVehicle
    @VehicleID NVARCHAR(50),
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    -- Get total count
    SELECT COUNT(*) AS TotalCount
    FROM InvoiceHeader 
    WHERE VehicleID = @VehicleID;
    
    -- Get paged results
    SELECT 
        InvoiceID,
        VehicleID,
        Odometer,
        InvoiceNumber,
        InvoiceDate,
        TotalCost,
        TotalPartsCost,
        TotalLaborCost,
        ConfidenceScore,
        CreatedAt
    FROM InvoiceHeader 
    WHERE VehicleID = @VehicleID
    ORDER BY InvoiceDate DESC, CreatedAt DESC
    OFFSET @Offset ROWS 
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Procedure to get invoices by date
CREATE PROCEDURE sp_GetInvoicesByDate
    @InvoiceDate DATE,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    -- Get total count
    SELECT COUNT(*) AS TotalCount
    FROM InvoiceHeader 
    WHERE InvoiceDate = @InvoiceDate;
    
    -- Get paged results
    SELECT 
        InvoiceID,
        VehicleID,
        Odometer,
        InvoiceNumber,
        InvoiceDate,
        TotalCost,
        TotalPartsCost,
        TotalLaborCost,
        ConfidenceScore,
        CreatedAt
    FROM InvoiceHeader 
    WHERE InvoiceDate = @InvoiceDate
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS 
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Procedure to log processing steps
CREATE PROCEDURE sp_LogProcessingStep
    @InvoiceID INT = NULL,
    @ProcessingStep NVARCHAR(100),
    @Status NVARCHAR(20),
    @Message NVARCHAR(MAX) = NULL,
    @ErrorDetails NVARCHAR(MAX) = NULL,
    @ProcessingTime INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO ProcessingLog (
        InvoiceID,
        ProcessingStep,
        Status,
        Message,
        ErrorDetails,
        ProcessingTime
    )
    VALUES (
        @InvoiceID,
        @ProcessingStep,
        @Status,
        @Message,
        @ErrorDetails,
        @ProcessingTime
    );
END
GO

-- =============================================
-- Create Functions
-- =============================================

-- Function to calculate total cost validation
CREATE FUNCTION fn_ValidateTotalCost(@InvoiceID INT)
RETURNS BIT
AS
BEGIN
    DECLARE @IsValid BIT = 1;
    DECLARE @HeaderTotal DECIMAL(18,2);
    DECLARE @LineTotal DECIMAL(18,2);
    
    SELECT @HeaderTotal = TotalCost
    FROM InvoiceHeader 
    WHERE InvoiceID = @InvoiceID;
    
    SELECT @LineTotal = SUM(TotalLineCost)
    FROM InvoiceLines 
    WHERE InvoiceID = @InvoiceID;
    
    -- Allow for small rounding differences (within $0.01)
    IF ABS(@HeaderTotal - ISNULL(@LineTotal, 0)) > 0.01
        SET @IsValid = 0;
    
    RETURN @IsValid;
END
GO

-- =============================================
-- Create Triggers
-- =============================================

-- Trigger to update UpdatedAt timestamp
-- Note: Removed as UpdatedAt field not needed per user requirements

-- =============================================
-- Insert Sample Data (for testing)
-- =============================================

-- Sample invoice header
INSERT INTO InvoiceHeader (
    VehicleID, Odometer, InvoiceNumber, InvoiceDate, 
    TotalCost, TotalPartsCost, TotalLaborCost, 
    BlobFileUrl, ExtractedData, ConfidenceScore
) VALUES (
    'VEH-001', 45000, 'INV-2025-001', '2025-08-01',
    350.00, 200.00, 150.00,
    'https://storage.blob.core.windows.net/invoices/sample1.pdf',
    '{"vendor":"Sample Auto Shop","items":5,"ocrVersion":"1.0"}', 95.5
);

-- Sample invoice lines
DECLARE @SampleInvoiceID INT = SCOPE_IDENTITY();

INSERT INTO InvoiceLines (InvoiceID, LineNumber, Description, UnitCost, Quantity, TotalLineCost, Category, ConfidenceScore) VALUES
(@SampleInvoiceID, 1, 'Oil Filter Replacement', 25.00, 1, 25.00, 'Parts', 98.0),
(@SampleInvoiceID, 2, 'Engine Oil (5W-30)', 35.00, 1, 35.00, 'Parts', 97.5),
(@SampleInvoiceID, 3, 'Labor - Oil Change', 75.00, 2, 150.00, 'Labor', 96.0),
(@SampleInvoiceID, 4, 'Air Filter', 28.00, 1, 28.00, 'Parts', 95.0),
(@SampleInvoiceID, 5, 'Cabin Filter', 32.00, 1, 32.00, 'Parts', 94.5),
(@SampleInvoiceID, 6, 'Shop Supplies', 15.00, 1, 15.00, 'Supplies', 90.0);

-- =============================================
-- Create Database Maintenance Jobs (Optional)
-- =============================================

-- Note: These would typically be created as SQL Agent jobs
-- For Azure SQL Database, use Azure Automation or Logic Apps

-- Sample cleanup procedure for old processing logs
CREATE PROCEDURE sp_CleanupOldLogs
    @DaysToKeep INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@DaysToKeep, GETUTCDATE());
    
    DELETE FROM ProcessingLog 
    WHERE CreatedAt < @CutoffDate;
    
    SELECT @@ROWCOUNT AS RecordsDeleted;
END
GO

-- =============================================
-- Grant Execute Permissions on Procedures
-- =============================================
GRANT EXECUTE ON sp_GetInvoiceById TO [vehicle-maintenance-app];
GRANT EXECUTE ON sp_GetInvoicesByVehicle TO [vehicle-maintenance-app];
GRANT EXECUTE ON sp_GetInvoicesByDate TO [vehicle-maintenance-app];
GRANT EXECUTE ON sp_LogProcessingStep TO [vehicle-maintenance-app];
GRANT EXECUTE ON sp_CleanupOldLogs TO [vehicle-maintenance-app];
GO

-- =============================================
-- Database Setup Complete
-- =============================================

PRINT 'Database schema setup completed successfully.';
PRINT 'Tables created: InvoiceHeader, InvoiceLines, ProcessingLog';
PRINT 'Views created: vw_InvoiceDetails, vw_InvoiceSummary';
PRINT 'Stored procedures created: sp_GetInvoiceById, sp_GetInvoicesByVehicle, sp_GetInvoicesByDate, sp_LogProcessingStep';
PRINT 'Sample data inserted for testing purposes.';

-- Check sample data
SELECT 'Sample Invoice Count' AS Info, COUNT(*) AS Value FROM InvoiceHeader
UNION ALL
SELECT 'Sample Line Items Count', COUNT(*) FROM InvoiceLines;
```

---

## Database Schema Documentation

### Table Relationships
```
InvoiceHeader (1) -----> (Many) InvoiceLines
InvoiceHeader (1) -----> (Many) ProcessingLog
```

### Data Types and Constraints

#### InvoiceHeader Table
- **InvoiceID**: Primary key, auto-incrementing
- **VehicleID**: Required, indexed for fast vehicle searches
- **InvoiceNumber**: Unique constraint, required for business rules
- **ProcessingStatus**: Enumerated values for workflow tracking
- **ConfidenceScore**: OCR confidence rating (0-100)

#### InvoiceLines Table
- **LineID**: Primary key, auto-incrementing
- **InvoiceID**: Foreign key with cascade delete
- **LineNumber**: Order within invoice
- **Category**: Classification for reporting (Parts, Labor, Tax, etc.)

#### ProcessingLog Table
- **LogID**: Primary key for audit trail
- **ProcessingStep**: Workflow stage tracking
- **ProcessingTime**: Performance monitoring in milliseconds

### Performance Considerations

1. **Indexing Strategy**:
   - VehicleID for vehicle-based searches
   - InvoiceDate for date-range queries
   - ProcessedAt for recent activity queries
   - ProcessingStatus for workflow filtering

2. **Query Optimization**:
   - Views for common query patterns
   - Stored procedures for complex operations
   - Pagination support for large result sets

3. **Data Validation**:
   - Check constraints for business rules
   - Triggers for automatic timestamp updates
   - Functions for complex validation logic

### Maintenance and Monitoring

1. **Regular Maintenance**:
   - Index defragmentation
   - Statistics updates
   - Log cleanup procedures

2. **Monitoring Queries**:
   ```sql
   -- Monitor processing performance
   SELECT 
       ProcessingStep,
       AVG(ProcessingTime) AS AvgTime,
       COUNT(*) AS Count
   FROM ProcessingLog 
   WHERE CreatedAt >= DATEADD(DAY, -7, GETUTCDATE())
   GROUP BY ProcessingStep;
   
   -- Check data quality
   SELECT 
       ProcessingStatus,
       COUNT(*) AS Count,
       AVG(ConfidenceScore) AS AvgConfidence
   FROM InvoiceHeader 
   GROUP BY ProcessingStatus;
   ```

This database schema provides a robust foundation for the Vehicle Maintenance Invoice Processing System with proper indexing, constraints, and stored procedures for optimal performance.
