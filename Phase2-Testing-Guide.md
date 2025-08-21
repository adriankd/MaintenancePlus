# Phase 2 Intelligence Features Testing Guide

## 🎯 Overview
This guide shows you how to test the newly implemented Phase 2 intelligence features:
- **Line Item Classification** (Part vs Labor)
- **Field Label Normalization** (Invoice #, RO, Unit # etc.)
- **Feedback Collection System**
- **Intelligence API Endpoints**

---

## 🌐 Method 1: Web Browser Testing

**Step 1:** Open your browser and go to: `http://localhost:5001`

**Step 2:** You should see the Vehicle Maintenance Invoice System homepage with existing invoices listed.

**Step 3:** Click on any invoice to view details. Look for:
- Intelligence fields in the invoice header
- Classification results in line items
- New confidence scores

---

## 🔌 Method 2: API Testing with curl/PowerShell

### Test 1: Get Classification Accuracy Metrics
```powershell
curl -X GET "http://localhost:5001/api/intelligence/classification/accuracy" -H "accept: application/json"
```

### Test 2: Process Invoice Intelligence (replace {invoiceId} with actual ID)
```powershell
curl -X POST "http://localhost:5001/api/intelligence/invoices/24/process" -H "Content-Type: application/json"
```

### Test 3: Submit Classification Feedback
```powershell
$body = @{
    originalClassification = "Part"
    correctCategory = "Labor"
    originalConfidence = 85
    userId = "test-user"
    comments = "This is actually labor for installation"
} | ConvertTo-Json

curl -X POST "http://localhost:5001/api/intelligence/invoices/24/lines/1/classification-feedback" -H "Content-Type: application/json" -d $body
```

### Test 4: Submit Field Normalization Feedback
```powershell
$body = @{
    fieldName = "VehicleLabel"
    originalValue = "Unit #"
    normalizedValue = "VehicleID"
    expectedValue = "VehicleID"
    userId = "test-user"
    comments = "Normalization is correct"
} | ConvertTo-Json

curl -X POST "http://localhost:5001/api/intelligence/invoices/24/field-normalization-feedback" -H "Content-Type: application/json" -d $body
```

---

## 🗄️ Method 3: Database Testing

### Check Intelligence Fields Added
```sql
-- Check InvoiceHeader new fields
SELECT TOP 5 InvoiceID, InvoiceNumber, OriginalVehicleLabel, OriginalOdometerLabel, NormalizationVersion 
FROM InvoiceHeader ORDER BY CreatedAt DESC;

-- Check InvoiceLines new fields  
SELECT TOP 5 LineID, Description, ClassifiedCategory, ClassificationConfidence, ClassificationMethod
FROM InvoiceLines ORDER BY CreatedAt DESC;

-- Check new feedback tables
SELECT COUNT(*) FROM ClassificationFeedback;
SELECT COUNT(*) FROM FieldNormalizationFeedback;
SELECT COUNT(*) FROM ClassificationAccuracyLog;
```

---

## 🧪 Method 4: Service Logic Testing

### Test Field Normalization Examples:
- `"Invoice #"` should normalize to `"InvoiceNumber"` (95% confidence)
- `"RO"` should normalize to `"InvoiceNumber"` (95% confidence)
- `"Unit #"` should normalize to `"VehicleID"` (95% confidence)
- `"Miles"` should normalize to `"Odometer"` (95% confidence)

### Test Line Item Classification Examples:
- `"Oil filter replacement"` should classify as `"Part"` (90% confidence)
- `"Brake pad installation"` should classify as `"Labor"` (85% confidence)  
- `"Engine diagnostic service"` should classify as `"Labor"` (95% confidence)
- `"Transmission fluid"` should classify as `"Part"` (80% confidence)

---

## 🛠️ Method 5: Swagger API Testing

**Step 1:** Open your browser and go to: `http://localhost:5001/swagger`

**Step 2:** You should see the Swagger UI with all API endpoints

**Step 3:** Look for the new `/api/intelligence/*` endpoints:
- `GET /api/intelligence/classification/accuracy`
- `POST /api/intelligence/invoices/{invoiceId}/process`
- `POST /api/intelligence/invoices/{invoiceId}/lines/{lineId}/classification-feedback`
- `POST /api/intelligence/invoices/{invoiceId}/field-normalization-feedback`

**Step 4:** Test each endpoint directly from Swagger UI

---

## 📊 Expected Results

### ✅ What You Should See:

1. **Web Interface:** Invoice listings with intelligence data
2. **API Responses:** JSON data with classification and normalization results
3. **Database:** New columns populated with intelligence values
4. **Logs:** Intelligence processing messages in application logs
5. **Performance:** Processing times under 500ms for most operations

### 🚨 Troubleshooting:

- **Connection refused:** Make sure application is running on localhost:5001
- **404 errors:** Verify API endpoints are properly registered
- **Database errors:** Ensure migration was applied successfully
- **Classification issues:** Check that services are registered in DI container

---

## 🎯 Success Criteria

Phase 2 testing is successful if:
- ✅ Application starts without errors
- ✅ Database contains new intelligence fields
- ✅ API endpoints return valid responses
- ✅ Classification logic works for sample data
- ✅ Normalization logic handles field variations
- ✅ Feedback collection stores user corrections
- ✅ Performance meets target response times

---

**Ready to test!** The application is running at `http://localhost:5001` 🚀
