# Test IFC Configurations Reading

## Bước 1: Kiểm tra file tồn tại
Mở PowerShell và chạy:

```powershell
# Check if Match Offset v.6.0 config file exists
$matchOffsetFile = "C:\Users\quoc.nguyen\AppData\Roaming\Autodesk\REVIT\IFC\2025\Match Offset v.6.0.json"
if (Test-Path $matchOffsetFile) {
    Write-Host "✓ Match Offset v.6.0 config file EXISTS" -ForegroundColor Green
    Get-Content $matchOffsetFile | ConvertFrom-Json | Format-List
} else {
    Write-Host "✗ Match Offset v.6.0 config file NOT FOUND" -ForegroundColor Red
}

# List all IFC config files
Write-Host "`nAll IFC config files:" -ForegroundColor Cyan
Get-ChildItem "$env:APPDATA\Autodesk\REVIT\IFC\2025\*.json" | Select-Object Name, LastWriteTime
```

## Bước 2: Kiểm tra Revit Document ExtensibleStorage

Trong Revit, mở file model và chạy command sau qua RevitPythonShell hoặc Macro:

```python
import clr
clr.AddReference('RevitAPI')
from Autodesk.Revit.DB import *
from Autodesk.Revit.DB.ExtensibleStorage import *

doc = __revit__.ActiveUIDocument.Document

# Check for IFC configuration schemas
jsonSchemaId = System.Guid("C2A3E6FE-CE51-4F35-8FF1-20C34567B687")
oldSchemaId = System.Guid("DCB88B13-594F-44F6-8F5D-AE9477305AC3")

jsonSchema = Schema.Lookup(jsonSchemaId)
oldSchema = Schema.Lookup(oldSchemaId)

print("JSON Schema found: {}".format(jsonSchema is not None))
print("Old Schema found: {}".format(oldSchema is not None))

# Find DataStorage elements with IFC configs
collector = FilteredElementCollector(doc).OfClass(DataStorage)
for storage in collector:
    if jsonSchema:
        entity = storage.GetEntity(jsonSchema)
        if entity.IsValid():
            print("\nFound IFC config in DataStorage:")
            print("  Element ID: {}".format(storage.Id))
    
    if oldSchema:
        entity = storage.GetEntity(oldSchema)
        if entity.IsValid():
            print("\nFound OLD IFC config in DataStorage:")
            print("  Element ID: {}".format(storage.Id))
```

## Bước 3: Test ProSheets Addin

1. **Mở Revit 2025**
2. **Mở file model có IFC configurations**
3. **Open ProSheets dialog** (External Command)
4. **Click vào IFC tab**
5. **Check dropdown list** - should see:
   - `<In-Session Setup>`
   - `Match Bottom Offset`
   - `Match Bottom Offset Multiple Selection Mode`
   - Built-in setups (IFC 2x3 Coordination View, etc.)

## Bước 4: Kiểm tra Debug Log

Sau khi mở ProSheets dialog, check file:

```powershell
# View debug log
$debugLog = "$env:USERPROFILE\Desktop\ProSheets_IFC_Debug.txt"
if (Test-Path $debugLog) {
    Write-Host "`n=== DEBUG LOG CONTENT ===" -ForegroundColor Cyan
    Get-Content $debugLog -Tail 50
} else {
    Write-Host "✗ Debug log file not found!" -ForegroundColor Red
}
```

Debug log sẽ show:
- ✓ JSON Schema found: True/False
- ✓ Old Schema found: True/False
- ✓ Found X custom configurations in document
- ✓ List of all configurations loaded

## Expected Results:

### ✅ SUCCESS - Nếu thấy:
```
========== READING FROM EXTENSIBLE STORAGE ==========
JSON Schema found: True
Using JSON Schema...
  [1] Match Bottom Offset (from JSON)
  [2] Match Bottom Offset Multiple Selection Mode (from JSON)
✓ Found 2 custom configurations in document
========== ADDING BUILT-IN CONFIGURATIONS ==========
✓ Added 9 built-in setups
========== FINAL TOTAL: 12 setups ==========
```

### ❌ FAIL - Nếu thấy:
```
JSON Schema found: False
Old Schema found: False
✓ Found 0 custom configurations in document
```

Nghĩa là configurations chưa được save vào document.

## Giải pháp nếu FAIL:

### Option 1: Save configurations vào Document
1. Mở Revit IFC Exporter UI
2. Select "Match Bottom Offset" setup
3. Click **"Save Setup"** hoặc **"Modify Setup"** 
4. Click OK để close dialog
5. **SAVE REVIT FILE** (.rvt)
6. Test lại ProSheets

### Option 2: Export/Import IFC configurations
Revit IFC Exporter có thể export configurations ra file JSON/XML, sau đó import vào document khác.

## Notes:

- **Match Offset v.6.0** trong hình là file config bên ngoài (JSON file)
- **ProSheets đọc từ Document ExtensibleStorage** (embedded trong .rvt file)
- Cần **save setup vào document** thì mới đọc được!
