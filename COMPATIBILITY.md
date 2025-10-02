# ProSheets Addin - Revit Compatibility

## Supported Revit Versions

This addin is compatible with:
- ✅ **Revit 2023**
- ✅ **Revit 2024**
- ✅ **Revit 2025**
- ✅ **Revit 2026**

## API Compatibility Notes

### Unit Conversion (Revit 2021+)
The addin uses `UnitTypeId` for unit conversions, which is the modern API available from Revit 2021 onwards:

```csharp
double widthMM = UnitUtils.ConvertFromInternalUnits(widthFeet, UnitTypeId.Millimeters);
```

This API is stable across Revit 2023-2026.

### FilteredElementCollector
All collector methods used are compatible:
- `OfClass(typeof(ViewSheet))`
- `OfCategory(BuiltInCategory.OST_TitleBlocks)`
- `WhereElementIsNotElementType()`

### Parameters
The addin uses standard BuiltInParameter enums which remain stable:
- `BuiltInParameter.SHEET_CURRENT_REVISION`
- `BuiltInParameter.SHEET_NUMBER`
- `BuiltInParameter.SHEET_NAME`
- `BuiltInParameter.SHEET_WIDTH`
- `BuiltInParameter.SHEET_HEIGHT`

## Installation for Multiple Versions

### Method 1: Single Installation (Recommended for Testing)
Copy the `.addin` file to the version-specific folder:

**Revit 2023:**
```
C:\ProgramData\Autodesk\Revit\Addins\2023\
```

**Revit 2024:**
```
C:\ProgramData\Autodesk\Revit\Addins\2024\
```

**Revit 2025:**
```
C:\ProgramData\Autodesk\Revit\Addins\2025\
```

**Revit 2026:**
```
C:\ProgramData\Autodesk\Revit\Addins\2026\
```

### Method 2: Multiple Version Support
Create version-specific `.addin` files:

**ProSheetsAddin2023.addin:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>ProSheets Exporter 2023</Name>
    <Assembly>ProSheetsAddin.dll</Assembly>
    <AddInId>B2A8A7B4-C2E4-4F2A-9B7C-8D1E3F4A5B6C</AddInId>
    <FullClassName>ProSheetsAddin.Application</FullClassName>
    <VendorId>LICORP</VendorId>
    <VendorDescription>Licorp Vietnam</VendorDescription>
  </AddIn>
</RevitAddIns>
```

Repeat for each version (2024, 2025, 2026) with corresponding names.

## Build Configuration

### Target Framework
- **.NET Framework 4.8** (compatible with all Revit 2023-2026)

### Platform Target
- **x64** (required for Revit)

### Revit API References
The addin references:
- `RevitAPI.dll`
- `RevitAPIUI.dll`

These DLLs are version-specific but the API signatures used remain stable.

## Testing Checklist

When testing on new Revit versions:

- [ ] Addin loads without errors
- [ ] Sheets list loads correctly
- [ ] Views list loads correctly
- [ ] Paper Size detection works from Title Blocks
- [ ] Custom File Name editing functions
- [ ] Search/Filter functionality works
- [ ] Profile save/load works
- [ ] Export functions work (PDF, DWG, IFC, etc.)
- [ ] Navigation (Back/Next) buttons work
- [ ] Statistics display correctly

## Known Limitations

### Revit 2020 and Earlier
- **Not Supported**: Uses `UnitTypeId` which is not available in Revit 2020 and earlier
- **Alternative**: Would require conditional compilation with `DisplayUnitType`

### Revit 2027+
- **To Be Tested**: API may change in future versions
- **Monitor**: Autodesk deprecated API announcements

## Migration Notes

### From Revit 2023 → 2024+
No changes required. All APIs are forward compatible.

### API Changes Tracked
1. **Unit Conversion**: Already using modern `UnitTypeId` API
2. **Element Filtering**: Using stable collector methods
3. **Parameter Access**: Using standard BuiltInParameter enums
4. **ViewSheet/View Access**: Using stable classes

## Future Compatibility

To maintain compatibility with future Revit versions:

1. **Avoid Deprecated APIs**: Monitor Revit API documentation for deprecation notices
2. **Use ForgeTypeId**: Already implemented for unit conversions
3. **Standard Collections**: Use LINQ and standard .NET collections
4. **WPF UI**: Uses standard WPF which is stable across versions

## Support

For version-specific issues:
- Check Revit API version documentation
- Review Autodesk Developer Network forums
- Test on specific Revit version before deployment

## Version History

- **v1.0.0** (2024): Initial release with Revit 2023-2026 support
  - Modern API usage (UnitTypeId)
  - .NET Framework 4.8
  - WPF UI with full feature set
