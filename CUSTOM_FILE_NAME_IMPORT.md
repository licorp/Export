# ✅ Custom File Name Import from XML - COMPLETED

## 🎯 Feature Added

**Custom File Name parameters** từ XML profile (`SelectionSheets.SelectedParamsVirtual.SelectionParameters`) giờ được **tự động apply** cho tất cả sheets khi import XML profile.

## 📝 What Was Added

### 1. New Method: `ApplyCustomFileNamesFromXML()`

**Location**: `Views/ProSheetsMainWindow.Profiles.cs`

```csharp
private void ApplyCustomFileNamesFromXML(ProSheetsXMLProfile xmlProfile)
{
    // Check if XML has custom file name parameters
    if (template.SelectionSheets?.SelectedParamsVirtual?.SelectionParameters == null)
    {
        WriteDebugLog("No custom file name parameters found in XML");
        return;
    }
    
    // Get all sheets from document
    var allSheets = new FilteredElementCollector(_document)
        .OfClass(typeof(ViewSheet))
        .Cast<ViewSheet>()
        .Where(s => !s.IsTemplate)
        .ToList();
    
    // Generate custom file names using XML parameters
    var customNames = XMLProfileManager.GenerateCustomFileNames(xmlProfile, allSheets);
    
    // Apply to all sheets in UI
    foreach (var customNameInfo in customNames)
    {
        var sheet = Sheets.FirstOrDefault(s => s.Number == customNameInfo.SheetNumber);
        if (sheet != null)
        {
            sheet.CustomFileName = customNameInfo.CustomFileName;
        }
    }
}
```

### 2. Integration Point

**File**: `Views/ProSheetsMainWindow.Profiles.cs` (ImportFile case)

```csharp
case ProfileNameDialog.ProfileCreationMode.ImportFile:
    // ... load XML profile ...
    
    // Apply settings to UI
    ApplyProfileToUI(newProfile);
    
    // ✅ NEW: Apply custom file names from XML
    ApplyCustomFileNamesFromXML(xmlProfile);  // ← ADDED
    
    break;
```

## 🧪 How to Test

### Test Scenario: Import XML with Custom File Names

```
1. Open ProSheetsAddin in Revit
2. Click "Add Profile"
3. Click "..." button
4. Select XML file (e.g., "crr_1.xml" or "PROJECTWISE EXPORTS.xml")
5. Click "Create"
6. ✅ Check DebugView logs for:
   === APPLYING CUSTOM FILE NAME PARAMETERS FROM XML ===
   Found X selection parameters in XML
   Generated Y custom file names from XML parameters
   Applied custom name to sheet P100: 'P-P100-PLAN - L0 SANITARY'
   Applied custom name to sheet P101: 'P-P101-PLAN - L1 SANITARY'
   ...
   Custom file names applied to 27/27 sheets
7. ✅ Check "Custom File Name" column in sheet list
8. ✅ Verify each sheet has custom name (NOT just sheet number)
```

## 📊 Expected Debug Logs

```
[Export +] === IMPORTING XML PROFILE SETTINGS ===
[Export +] Format flags set - PDF:True, DWG:False, ...
[Export +] PDF settings set - Vector:True, Quality:High, ...
[Export +] === SAVING PROFILE WITH ALL SETTINGS ===
[Export +] === APPLYING SETTINGS TO UI ===
[Export +] Settings applied to UI successfully
[Export +] === APPLYING CUSTOM FILE NAME PARAMETERS FROM XML ===  ← NEW
[Export +] Found 3 selection parameters in XML  ← NEW
[Export +] Found 27 sheets in document  ← NEW
[XMLProfileManager] Generating custom file names for 27 sheets  ← NEW
[XMLProfileManager] Found 3 selected parameters, separator: '-'  ← NEW
[Export +] Generated 27 custom file names from XML parameters  ← NEW
[Export +] Applied custom name to sheet P100: 'P-P100-PLAN - L0 SANITARY'  ← NEW
[Export +] Applied custom name to sheet P101: 'P-P101-PLAN - L1 SANITARY'  ← NEW
...
[Export +] Custom file names applied to 27/27 sheets  ← NEW
[Export +] New profile 'crr_1' created and selected
```

## 🔍 XML Structure Reference

**XML Profile with Custom File Names**:
```xml
<Profile>
  <Name>CRR Profile</Name>
  <TemplateInfo>
    <SelectionSheets>
      <SelectedParamsVirtual>
        <SelectionParameter xml:space="Sheet Number Prefix" Type="Revit" IsSelected="true"/>
        <SelectionParameter xml:space="Sheet Number" Type="Revit" IsSelected="true"/>
        <SelectionParameter xml:space="Sheet Name" Type="Revit" IsSelected="true"/>
        <FieldSeparator>-</FieldSeparator>
      </SelectedParamsVirtual>
    </SelectionSheets>
  </TemplateInfo>
</Profile>
```

**Result**: Custom file names like `P-P100-PLAN - L0 SANITARY`
- "P" = Sheet Number Prefix (extracted from "P100")
- "P100" = Sheet Number
- "PLAN - L0 SANITARY" = Sheet Name
- "-" = Field Separator

## ✅ Verification Checklist

### After Importing XML Profile:

1. **Custom File Name Column Visible**: ✅
   - Look at sheet list DataGrid
   - "Custom File Name" column should show custom names (not just numbers)

2. **All Sheets Have Custom Names**: ✅
   - Scroll through all sheets
   - Each should have pattern like: `{Prefix}-{Number}-{Name}`

3. **Separator Correct**: ✅
   - Check separator between fields
   - Should match `FieldSeparator` from XML (default: `-`)

4. **Parameters Match XML**: ✅
   - Custom names should include only parameters where `IsSelected="true"`
   - Order should match XML order

5. **Special Characters Handled**: ✅
   - Spaces in sheet names preserved
   - Special characters (/, \, :) removed or replaced

## 🐛 Troubleshooting

### Issue 1: Custom names not appearing
```
Check DebugView logs:
- "No custom file name parameters found in XML" → XML missing SelectionSheets
- "Found 0 selection parameters in XML" → All parameters have IsSelected="false"
```

### Issue 2: Wrong custom names
```
Check:
- FieldSeparator in XML (should be "-" or "_")
- SelectionParameter order (top to bottom = left to right in name)
- IsSelected="true" for parameters you want to include
```

### Issue 3: Errors in log
```
"ERROR applying custom file names from XML: ..." → Check:
- Sheets collection is loaded (Sheets.Count > 0)
- _document is valid
- XML structure matches ProSheetsXMLProfile model
```

## 📁 Files Changed

1. ✅ `Views/ProSheetsMainWindow.Profiles.cs`
   - Added `ApplyCustomFileNamesFromXML()` method
   - Called from ImportFile case after `ApplyProfileToUI()`

2. ✅ `Managers/XMLProfileManager.cs`
   - Already has `GenerateCustomFileNames()` method (existing)

## ✅ Build Status

```
Build: SUCCESS
Errors: 0
Warnings: 10 (unrelated: CS0168, CS1998)
Output: ProSheetsAddin.dll → bin\Debug\
```

---
**Status**: ✅ COMPLETED  
**Date**: 2025-10-03  
**Feature**: Custom file name import from XML profile  
**Integration**: Automatic (no user action required after import)
