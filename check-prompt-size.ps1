# Quick script to test GPT-4o prompt size
Write-Host "Testing GPT-4o Prompt Size Analysis" -ForegroundColor Green

# Test the actual prompt being sent
$testUrl = "http://localhost:5000/api/llm/test-prompt-size"

try {
    Write-Host "Making request to check prompt size..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri $testUrl -Method Post -ContentType "application/json" -Body '{"testData": "sample"}' -TimeoutSec 30
    
    if ($response) {
        Write-Host "Response received:" -ForegroundColor Cyan
        $response | ConvertTo-Json -Depth 3
    }
}
catch {
    Write-Host "Error testing prompt size: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Full error: $($_.Exception)" -ForegroundColor DarkRed
}

Write-Host "`nTest completed." -ForegroundColor Green
