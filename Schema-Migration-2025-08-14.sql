-- =============================================
-- Schema Migration Script
-- Vehicle Maintenance Invoice Processing System
-- =============================================
-- Date: August 14, 2025
-- Issue: InvalidCastException when viewing invoice details
-- Cause: InvoiceLines table columns created as INT instead of DECIMAL
-- Effect: Entity Framework unable to cast INT values to DECIMAL properties
-- 
-- Error Details:
-- "Unable to cast object of type 'System.Int32' to type 'System.Decimal'"
-- Occurred when accessing invoice details for invoices 3, 4, 5
-- 
-- Root Cause Analysis:
-- Entity Framework model defined decimal properties:
-- - public decimal Quantity { get; set; }
-- - public decimal UnitCost { get; set; }  
-- - public decimal TotalLineCost { get; set; }
-- 
-- But database schema had INT columns instead of DECIMAL
-- =============================================

USE VehicleMaintenance;
GO

-- Check current column types before migration
PRINT '=== BEFORE MIGRATION ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    NUMERIC_PRECISION,
    NUMERIC_SCALE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'InvoiceLines' 
AND COLUMN_NAME IN ('Quantity', 'UnitCost', 'TotalLineCost')
ORDER BY COLUMN_NAME;

-- Apply schema fixes
PRINT '=== APPLYING SCHEMA FIXES ===';

-- Fix Quantity column (allow fractional quantities like 2.5 hours)
PRINT 'Fixing Quantity column: INT → DECIMAL(10,2)';
ALTER TABLE InvoiceLines ALTER COLUMN Quantity DECIMAL(10,2);

-- Fix UnitCost column (currency values with 2 decimal places)
PRINT 'Fixing UnitCost column: INT → DECIMAL(18,2)';
ALTER TABLE InvoiceLines ALTER COLUMN UnitCost DECIMAL(18,2);

-- Fix TotalLineCost column (currency values with 2 decimal places)
PRINT 'Fixing TotalLineCost column: INT → DECIMAL(18,2)';
ALTER TABLE InvoiceLines ALTER COLUMN TotalLineCost DECIMAL(18,2);

-- Verify changes
PRINT '=== AFTER MIGRATION ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    NUMERIC_PRECISION,
    NUMERIC_SCALE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'InvoiceLines' 
AND COLUMN_NAME IN ('Quantity', 'UnitCost', 'TotalLineCost')
ORDER BY COLUMN_NAME;

-- Test data integrity
PRINT '=== DATA INTEGRITY CHECK ===';
SELECT 
    COUNT(*) AS TotalRecords,
    MIN(Quantity) AS MinQuantity,
    MAX(Quantity) AS MaxQuantity,
    MIN(UnitCost) AS MinUnitCost,
    MAX(UnitCost) AS MaxUnitCost,
    SUM(TotalLineCost) AS TotalValue
FROM InvoiceLines;

PRINT '=== MIGRATION COMPLETED SUCCESSFULLY ===';
PRINT 'Invoice details pages should now work without casting errors.';
PRINT 'Test by accessing: http://localhost:5000/Home/Details/3';

-- =============================================
-- Migration Verification Steps:
-- 1. Restart the application
-- 2. Test invoice details pages that previously failed
-- 3. Verify decimal values display correctly
-- 4. Test new invoice uploads to ensure compatibility
-- =============================================
