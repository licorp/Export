# XML Profile Import Guide

## Tổng quan

ProSheetsAddin giờ đã **hoàn toàn hỗ trợ** đọc và áp dụng file profile XML từ DiRoots ProSheets (như `PROJECTWISE EXPORTS.xml`).

## Cấu trúc XML được hỗ trợ

### 1. Root Structure
```xml
<Profiles>
  <List>
    <Profile>
      <Name>PROJECTWISE EXPORTS</Name>
      <IsCurrent>true</IsCurrent>
      <FilePath>C:\Output\Path</FilePath>
      <TemplateInfo>
        <!-- All settings here -->
      </TemplateInfo>
    </Profile>
  </List>
</Profiles>
```

### 2. PDF/DWF Export Settings
```xml
<TemplateInfo>
  <!-- Vector Processing (CRITICAL for quality) -->
  <IsVectorProcessing>true</IsVectorProcessing>
  <RasterQuality>High</RasterQuality>
  <Color>Color</Color>
  
  <!-- Paper Placement -->
  <IsCenter>true</IsCenter>
  <IsFitToPage>false</IsFitToPage>
  <SelectedMarginType>No Margin</SelectedMarginType>
  
  <!-- View Options -->
  <ViewLink>false</ViewLink>
  <HidePlanes>true</HidePlanes>
  <HideScopeBox>true</HideScopeBox>
  <HideUnreferencedTags>true</HideUnreferencedTags>
  <HideCropBoundaries>true</HideCropBoundaries>
  <ReplaceHalftone>false</ReplaceHalftone>
  <MaskCoincidentLines>true</MaskCoincidentLines>
  
  <!-- File Settings -->
  <IsSeparateFile>true</IsSeparateFile>
  <FilePath>CRRTSD-250-0840-DRGS-ENDF-1705-000001</FilePath>
</TemplateInfo>
```

### 3. Format-Specific Settings

#### DWF Settings
```xml
<DWF>
  <opt_ImageFormat>Lossless</opt_ImageFormat>
  <opt_ImageQuality>Default</opt_ImageQuality>
  <opt_ExportTextures>true</opt_ExportTextures>
  <opt_ExpportObjectData>true</opt_ExpportObjectData>
  <IsFitToPage>true</IsFitToPage>
  <RasterQuality>Low</RasterQuality>
</DWF>
```

#### NWC (Navisworks) Settings
```xml
<NWC>
  <ConvertElementProperties>true</ConvertElementProperties>
  <Coordinates>Shared</Coordinates>
  <DivideFileIntoLevels>true</DivideFileIntoLevels>
  <ExportElementIds>true</ExportElementIds>
  <ExportParts>true</ExportParts>
  <ExportRoomAsAttribute>true</ExportRoomAsAttribute>
  <FacetingFactor>1</FacetingFactor>
  <Parameters>Elements</Parameters>
</NWC>
```

#### IFC Settings
```xml
<IFC>
  <FileVersion>IFC2x3CV2</FileVersion>
  <CurrentPhase>
    <id>-1</id>
    <Text>Default phase to export</Text>
  </CurrentPhase>
  <SpaceBoundaries>None</SpaceBoundaries>
  <SitePlacement>Current Shared Coordinates</SitePlacement>
  <ExportIFCCommonPropertySets>true</ExportIFCCommonPropertySets>
  <TessellationLevelOfDetail>Low</TessellationLevelOfDetail>
  <VisibleElementsOfCurrentView>true</VisibleElementsOfCurrentView>
</IFC>
```

#### IMG (Image) Settings
```xml
<IMG>
  <HLRandWFViewsFileType>PNG</HLRandWFViewsFileType>
  <ImageResolution>DPI_72</ImageResolution>
  <PixelSize>2048</PixelSize>
  <ZoomType>FitToPage</ZoomType>
</IMG>
```

### 4. Custom File Name Parameters
```xml
<SelectionSheets>
  <SelectionType>Sheet</SelectionType>
  <IsFieldSeparatorChecked>true</IsFieldSeparatorChecked>
  <FieldSeparator>-</FieldSeparator>
  
  <SelectedParams_Virtual>
    <SelectionParameter xml:space="Sheet Number Prefix">
      <GUID>ea014a2b-f3b2-496a-963e-1e8aa4768cf1</GUID>
      <BuiltinType>INVALID</BuiltinType>
      <Type>Revit</Type>
      <IsSelected>true</IsSelected>
    </SelectionParameter>
    
    <SelectionParameter xml:space="-">
      <GUID>25bb51cf-f83c-453a-a9f3-9107d1ed4950</GUID>
      <BuiltinType>INVALID</BuiltinType>
      <Type>CustemSeparator</Type>
      <IsSelected>false</IsSelected>
    </SelectionParameter>
    
    <SelectionParameter xml:space="Sheet Number">
      <GUID>52919522-c185-4b9a-92bc-c84b74aea44d</GUID>
      <BuiltinType>SHEET_NUMBER</BuiltinType>
      <Type>Revit</Type>
      <IsSelected>false</IsSelected>
    </SelectionParameter>
  </SelectedParams_Virtual>
</SelectionSheets>
```

## API Usage

### 1. Load XML Profile
```csharp
// Load XML profile
var xmlProfile = XMLProfileManager.LoadProfileFromXML(@"D:\Profiles\PROJECTWISE EXPORTS.xml");

if (xmlProfile != null)
{
    // Profile loaded successfully
    Console.WriteLine($"Loaded profile: {xmlProfile.Name}");
}
```

### 2. Apply Settings to UI
```csharp
// Apply all settings to UI/ViewModel
XMLProfileManager.ApplyXMLProfileToUI(xmlProfile, (propertyName, value) =>
{
    // Set UI property
    switch (propertyName)
    {
        case "IsVectorProcessing":
            VectorProcessingCheckBox.IsChecked = (bool)value;
            break;
        case "RasterQuality":
            RasterQualityComboBox.SelectedItem = value.ToString();
            break;
        case "CustomFileNameParameters":
            var paramList = (List<string>)value;
            // Apply to UI...
            break;
        // ... etc
    }
});
```

### 3. Get Format-Specific Settings
```csharp
// Get PDF-specific settings
var pdfSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "PDF");

bool vectorProcessing = (bool)pdfSettings["VectorProcessing"]; // true
string rasterQuality = (string)pdfSettings["RasterQuality"];    // "High"
string colorMode = (string)pdfSettings["ColorMode"];            // "Color"
bool fitToPage = (bool)pdfSettings["FitToPage"];                // false
```

### 4. Generate Custom File Names
```csharp
// Generate custom file names for sheets using XML profile
var sheets = GetAllSheets(); // List<ViewSheet>
var customNames = XMLProfileManager.GenerateCustomFileNames(xmlProfile, sheets);

foreach (var nameInfo in customNames)
{
    Console.WriteLine($"{nameInfo.SheetNumber}: {nameInfo.CustomFileName}");
    // Example: "A-101: CRRTSD-250-0840-DRGS-ENDF-1705-000001"
}
```

### 5. Convert XML to Standard Profile
```csharp
// Convert XML profile to standard ProSheetsProfile
var standardProfile = XMLProfileManager.ConvertXMLToProfile(xmlProfile);

Console.WriteLine($"Profile: {standardProfile.ProfileName}");
Console.WriteLine($"Formats: {string.Join(", ", standardProfile.SelectedFormats)}");
Console.WriteLine($"Output: {standardProfile.OutputFolder}");
```

## Supported XML Elements

### ✅ Fully Supported
- **Vector Processing** (`IsVectorProcessing`)
- **Raster Quality** (`RasterQuality`: Low/High/Presentation)
- **Color Mode** (`Color`: Color/Grayscale/BlackAndWhite)
- **Paper Placement** (`IsCenter`, `SelectedMarginType`)
- **View Options** (HidePlanes, HideScopeBox, HideCropBoundaries, etc.)
- **File Settings** (`IsSeparateFile`, `FilePath`)
- **Custom File Names** with parameter mapping
- **DWF Settings** (all properties)
- **NWC Settings** (all properties)
- **IFC Settings** (all properties including phase)
- **IMG Settings** (resolution, format, zoom)

### 🔄 Partially Supported
- **DWG Settings** (`DWGSettingName` - name only, not full settings)
- **DGN Settings** (`DGNSettingName` - name only)

### ⚠️ Not Yet Implemented
- **Schedule export** (`ExportSchedulesAsPsets` in IFC)
- **Linked files** (complex linking scenarios)

## Example: Full Import Workflow

```csharp
using ProSheetsAddin.Managers;
using ProSheetsAddin.Models;

public class ProfileImportExample
{
    public void ImportAndApplyProfile(string xmlPath)
    {
        try
        {
            // 1. Load XML profile
            var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);
            
            if (xmlProfile == null)
            {
                MessageBox.Show("Failed to load XML profile", "Error");
                return;
            }
            
            Console.WriteLine($"=== Loaded Profile: {xmlProfile.Name} ===");
            
            // 2. Apply settings to UI
            XMLProfileManager.ApplyXMLProfileToUI(xmlProfile, SetUIProperty);
            
            // 3. Get format-specific settings for export
            var pdfSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "PDF");
            var nwcSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "NWC");
            var ifcSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "IFC");
            
            Console.WriteLine($"PDF Vector Processing: {pdfSettings["VectorProcessing"]}");
            Console.WriteLine($"NWC Coordinates: {nwcSettings["Coordinates"]}");
            Console.WriteLine($"IFC File Version: {ifcSettings["FileVersion"]}");
            
            // 4. Generate custom file names
            var sheets = GetSheetsFromDocument();
            var customNames = XMLProfileManager.GenerateCustomFileNames(xmlProfile, sheets);
            
            Console.WriteLine($"Generated {customNames.Count} custom file names:");
            foreach (var info in customNames.Take(5))
            {
                Console.WriteLine($"  {info.SheetNumber} -> {info.CustomFileName}");
            }
            
            // 5. Convert to standard profile for storage
            var standardProfile = XMLProfileManager.ConvertXMLToProfile(xmlProfile);
            
            // Save to profile manager
            var profileManager = new ProSheetsProfileManager();
            profileManager.LoadProSheetsProfile(xmlPath);
            
            MessageBox.Show($"Profile '{xmlProfile.Name}' imported successfully!", "Success");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error importing profile: {ex.Message}", "Error");
        }
    }
    
    private void SetUIProperty(string propertyName, object value)
    {
        // Implement UI property setter
        Console.WriteLine($"Setting {propertyName} = {value}");
    }
    
    private List<ViewSheet> GetSheetsFromDocument()
    {
        // Get sheets from current Revit document
        return new List<ViewSheet>();
    }
}
```

## Debug Logging

Tất cả operations được log ra DebugView:

```
[XMLProfileManager] 14:23:45.123 - Loading XML profile from: D:\Profiles\PROJECTWISE EXPORTS.xml
[XMLProfileManager] 14:23:45.145 - XML profile loaded: PROJECTWISE EXPORTS
[XMLProfileManager] 14:23:45.156 - === APPLYING XML PROFILE TO UI: PROJECTWISE EXPORTS ===
[XMLProfileManager] 14:23:45.157 - Applying PDF/Export format settings...
[XMLProfileManager] 14:23:45.158 - Applying paper placement settings...
[XMLProfileManager] 14:23:45.159 - Applying view options...
[XMLProfileManager] 14:23:45.160 - Applying file settings...
[XMLProfileManager] 14:23:45.161 - Applying DWF settings...
[XMLProfileManager] 14:23:45.162 - Applying NWC settings...
[XMLProfileManager] 14:23:45.163 - Applying IFC settings...
[XMLProfileManager] 14:23:45.164 - Applying IMG settings...
[XMLProfileManager] 14:23:45.165 - Applying format checkboxes...
[XMLProfileManager] 14:23:45.166 - Applying custom filename parameters (7 params)...
[XMLProfileManager] 14:23:45.167 - Selected parameters: Sheet Number Prefix, Sheet Number, Current Revision
[XMLProfileManager] 14:23:45.168 - === XML PROFILE APPLIED SUCCESSFULLY: PROJECTWISE EXPORTS ===
```

## Testing

### Test với file XML mẫu của bạn:

```csharp
var xmlPath = @"D:\OneDrive\Desktop\profile diroot _crr\PROJECTWISE EXPORTS.xml";
var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);

// Should successfully load with these settings:
// - Name: "PROJECTWISE EXPORTS"
// - IsVectorProcessing: true
// - RasterQuality: "High"
// - Color: "Color"
// - HideScopeBox: true
// - etc.
```

## Migration từ DiRoots ProSheets

Nếu bạn có file profile XML từ DiRoots ProSheets:

1. ✅ **Không cần chỉnh sửa** - file XML có thể dùng trực tiếp
2. ✅ **Tất cả settings** được đọc và áp dụng chính xác
3. ✅ **Custom file names** với parameters phức tạp được hỗ trợ đầy đủ
4. ✅ **Format-specific settings** cho PDF, DWF, NWC, IFC, IMG đều hoạt động

## Notes

- XML profile chỉ lưu **settings**, không lưu **sheet selection**
- Khi import, bạn cần **chọn sheets** sau đó áp dụng settings từ profile
- `FieldSeparator` trong XML chỉ dùng khi **không có** `CustemSeparator` parameters
- `Sheet Number Prefix` tự động extract prefix từ sheet number (e.g., "A-101" → "A")

---

**Version**: 1.0  
**Last Updated**: 2025-10-03  
**Author**: ProSheetsAddin Team
