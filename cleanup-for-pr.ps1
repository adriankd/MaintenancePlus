#!/usr/bin/env pwsh

# Cleanup script to remove test files and development artifacts before creating PR
Write-Host "🧹 Cleaning up test files and development artifacts before PR creation..." -ForegroundColor Green

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

# Test PowerShell Scripts to remove
$testPowerShellFiles = @(
    "Test-AI-Enhanced-Upload.ps1",
    "test-app-status.ps1", 
    "test-classification-fix.ps1",
    "test-error-details.ps1",
    "Test-GPT4o-Direct.ps1",
    "Test-GPT4o-Setup.ps1",
    "Test-Intelligence.ps1",
    "test-invoices.ps1",
    "test-prompt-size.ps1",
    "Test-PartNumberExtraction.ps1",
    "Test-UpdatedPartNumberExtraction-Fixed.ps1",
    "Test-UpdatedPartNumberExtraction.ps1",
    "test-upload-with-gpt4o.ps1",
    "test-upload.ps1",
    "test-security.ps1"
)

# Test C# Files to remove  
$testCSharpFiles = @(
    "test-linq.cs",
    "test-linq-fixed.cs",
    "TestIntelligence.cs",
    "TestLinqCount.cs",
    "TestMileageFix.cs",
    "TestOdometerFix.cs",
    "TestPartNumberExtraction.cs"
)

# Temporary files to remove
$tempFiles = @(
    "temp.txt"
)

# Archive files to remove
$archiveFiles = @(
    "app-logs.zip",
    "webapp-logs.zip", 
    "deploy-clean.zip",
    "deploy.zip",
    "file-access-deploy.zip"
)

# Test directories to remove
$testDirectories = @(
    "app-logs",
    "logs",
    "MileageTest", 
    "TestInvoices"
)

$removedCount = 0
$errorCount = 0

# Function to safely remove file
function Remove-SafeFile {
    param($filePath, $description)
    
    if (Test-Path $filePath) {
        try {
            Remove-Item $filePath -Force
            Write-Host "  ✅ Removed: $description" -ForegroundColor DarkGreen
            return $true
        }
        catch {
            Write-Host "  ❌ Failed to remove: $description - $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
    else {
        Write-Host "  ⚪ Not found: $description" -ForegroundColor DarkGray
        return $false
    }
}

# Function to safely remove directory
function Remove-SafeDirectory {
    param($dirPath, $description)
    
    if (Test-Path $dirPath) {
        try {
            Remove-Item $dirPath -Recurse -Force
            Write-Host "  ✅ Removed directory: $description" -ForegroundColor DarkGreen
            return $true
        }
        catch {
            Write-Host "  ❌ Failed to remove directory: $description - $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
    else {
        Write-Host "  ⚪ Directory not found: $description" -ForegroundColor DarkGray
        return $false
    }
}

Write-Host "`n📁 Removing test PowerShell scripts..." -ForegroundColor Yellow
foreach ($file in $testPowerShellFiles) {
    $fullPath = Join-Path $projectRoot $file
    if (Remove-SafeFile $fullPath $file) { $removedCount++ }
    else { $errorCount++ }
}

Write-Host "`n🔧 Removing test C# files..." -ForegroundColor Yellow
foreach ($file in $testCSharpFiles) {
    $fullPath = Join-Path $projectRoot $file
    if (Remove-SafeFile $fullPath $file) { $removedCount++ }
    else { $errorCount++ }
}

Write-Host "`n🗂️ Removing temporary files..." -ForegroundColor Yellow
foreach ($file in $tempFiles) {
    $fullPath = Join-Path $projectRoot $file
    if (Remove-SafeFile $fullPath $file) { $removedCount++ }
    else { $errorCount++ }
}

Write-Host "`n📦 Removing archive files..." -ForegroundColor Yellow
foreach ($file in $archiveFiles) {
    $fullPath = Join-Path $projectRoot $file
    if (Remove-SafeFile $fullPath $file) { $removedCount++ }
    else { $errorCount++ }
}

Write-Host "`n📂 Removing test directories..." -ForegroundColor Yellow
foreach ($dir in $testDirectories) {
    $fullPath = Join-Path $projectRoot $dir
    if (Remove-SafeDirectory $fullPath $dir) { $removedCount++ }
    else { $errorCount++ }
}

Write-Host "`n📊 Cleanup Summary:" -ForegroundColor Cyan
Write-Host "  ✅ Successfully removed: $removedCount items" -ForegroundColor Green
Write-Host "  ❌ Failed to remove: $errorCount items" -ForegroundColor Red

if ($removedCount -gt 0) {
    Write-Host "`n🎯 Repository cleaned up! Ready for PR creation." -ForegroundColor Green
    Write-Host "`n📋 Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Review the changes: git status" -ForegroundColor White
    Write-Host "  2. Commit cleanup if needed: git add -A && git commit -m 'Clean up test files and development artifacts'" -ForegroundColor White
    Write-Host "  3. Push changes: git push origin feature/api-security-and-fixes" -ForegroundColor White
    Write-Host "  4. Create PR on GitHub" -ForegroundColor White
} else {
    Write-Host "`n✨ Repository was already clean!" -ForegroundColor Green
}

Write-Host "`n🏁 Cleanup completed." -ForegroundColor Green
