# IFC Settings Tab Documentation

## Overview
Complete IFC (Industry Foundation Classes) export settings tab với 3-column layout: **Setup & General**, **Level of Details & Property Sets**, và **Advanced & Import/Export Settings**.

## Tab Header
```xaml
<TabItem Header="IFC Settings" IsEnabled="{Binding ExportSettings.IsIfcSelected}">
```
- **IsEnabled Binding**: Tab chỉ active khi IFC checkbox được chọn trong Format Icons

## Layout Structure

```
┌─────────────────────────────────────────────────────────────────────┐
│ Format Icons Row (7 formats: PDF, DWG, DGN, DWF, NWC, IFC*, IMG)   │
│ *IFC highlighted với border xanh lá                                 │
├─────────────────┬─────────────────┬───────────────────────────────┤
│ Setup & General │ Level of Details│ Advanced                       │
│                 │ & Property Sets │                                │
│ • Current Setup │                 │ • Export parts as building... │
│ • IFC Version   │ Detail Level:   │ • Allow mixed Solid Model...  │
│ • File Type     │ [Medium ▼]      │ • Use active view...          │
│ • Phase         │                 │ • Use family and type name✓   │
│ • Space bound.. │ Property Sets:  │ • Use 2D room boundaries...   │
│ • Project Orig..│ □ Export Revit..│ • Include IFCSITE elevation.. │
│                 │ ☑ Export IFC... │ • Store IFC GUID...           │
│ ☑ Split walls...│ □ Export base...│ • Export bounding box         │
│ ☑ Include Steel │ □ Export sched..│ • Keep tessellated...         │
│                 │ □ Export user...│ • Use Type name only...       │
│ [File Header...│ [ClassSettings] │ • Use visible Revit name...   │
│ [Address...]   │                 │                                │
│                 │                 │ Import/Export Settings:        │
│                 │                 │ [Import...] [Export...]       │
└─────────────────┴─────────────────┴───────────────────────────────┘
```

## Column 1: Setup & General (Left Column)

### GroupBox Header: "Setup & General"

### Controls:

| Control | Type | Values | Default | Description |
|---------|------|--------|---------|-------------|
| Current Setup | ComboBox | `<In-Session Setup>`, `Default` | In-Session | Current IFC setup configuration |
| IFC Version | ComboBox | `IFC 2x3 Coordination View 2.0`, `IFC 4 Reference View`, `IFC 2x3 Coordination View`, `IFC 4 Design Transfer View` | IFC 2x3 CV 2.0 | IFC schema version |
| File Type | ComboBox | `IFC`, `IFCZIP`, `IFCXML` | IFC | Output file format |
| Phase to export | ComboBox | `Default phase to export`, `Existing`, `New Construction` | Default | Revit phase to export |
| Space boundaries | ComboBox | `None`, `1st Level`, `2nd Level` | None | Space boundary export level |
| Project Origin | ComboBox | `Shared Coordinates`, `Site Survey Point`, `Project Base Point`, `Internal Origin` | Shared Coordinates | Coordinate system origin |
| Split walls... | CheckBox | Checked/Unchecked | Unchecked | Split walls and columns by level |
| Include Steel elements | CheckBox | Checked/Unchecked | Checked | Include steel structural elements |
| File Header... | Button | N/A | Disabled | Edit IFC file header information |
| Address... | Button | N/A | Disabled | Edit project address |

### XAML Structure:
```xaml
<GroupBox Header="Setup &amp; General" Grid.Column="0" Margin="0,0,10,0">
    <StackPanel Margin="10">
        <!-- 6 ComboBoxes with labels -->
        <!-- 2 CheckBoxes -->
        <!-- 2 Buttons (disabled) -->
    </StackPanel>
</GroupBox>
```

## Column 2: Level of Details & Property Sets (Middle Column)

### Top Section: Level of Details

| Control | Type | Values | Default | Description |
|---------|------|--------|---------|-------------|
| Detail Level | ComboBox | `Coarse`, `Medium`, `Fine` | Medium | Geometry detail level |

### Bottom Section: Property Sets

| Control | Type | Default | Description |
|---------|------|---------|-------------|
| Export Revit property sets | CheckBox | Unchecked | Export Revit-specific properties |
| Export IFC Common property sets | CheckBox | **Checked** | Export standard IFC properties (Pset_*) |
| Export base quantities | CheckBox | Unchecked | Export quantity takeoff data (Qto_*) |
| Export schedules as property sets | CheckBox | Unchecked | Convert Revit schedules to IFC property sets |
| Export only schedules containing IFC... | CheckBox | Disabled | Filter schedules by title (requires parent checked) |
| Export user defined property sets | CheckBox | Unchecked | Load custom property sets from file |
| [TextBox + ... Button] | TextBox + Button | Both Disabled | Browse for user-defined Pset file (.txt) |
| Export parameter mapping table | CheckBox | Unchecked | Load parameter mapping configuration |
| [TextBox + ... Button] | TextBox + Button | Both Disabled | Browse for parameter mapping file (.txt) |
| Classification Settings... | Button | Enabled | Open classification system settings dialog |

### XAML Structure:
```xaml
<StackPanel Grid.Column="1" Margin="0,0,10,0">
    <GroupBox Header="Level of details">
        <ComboBox Name="DetailLevelComboIFC"/>
    </GroupBox>
    
    <GroupBox Header="Property Sets">
        <StackPanel>
            <!-- 6 CheckBoxes -->
            <!-- 2 TextBox + Button pairs (disabled) -->
            <!-- 1 Button (enabled) -->
        </StackPanel>
    </GroupBox>
</StackPanel>
```

## Column 3: Advanced & Import/Export Settings (Right Column)

### Top Section: Advanced Options

| Control | Type | Default | Description |
|---------|------|---------|-------------|
| Export parts as building elements | CheckBox | Unchecked | Export parts as separate IFC entities |
| Allow use of mixed 'Solid Model' representation | CheckBox | Unchecked | Allow mixed geometry representations |
| Use active view when creating geometry | CheckBox | Unchecked | Use current view settings for geometry |
| Use family and type name for reference | CheckBox | **Checked** | Use Revit family/type names as IFC names |
| Use 2D room boundaries for room volume | CheckBox | Unchecked | Calculate room volumes from 2D boundaries |
| Include IFCSITE elevation in the site local placement origin | CheckBox | Unchecked | Include site elevation in placement |
| Store IFC GUID in element after export | CheckBox | Unchecked | Store IFC GUID as Revit parameter |
| Export bounding box | CheckBox | Unchecked | Include bounding box geometry |
| Keep tessellated geometry as triangulation | CheckBox | Unchecked | Preserve triangulated mesh |
| Use Type name only for IFCType name | CheckBox | Unchecked | Use only type name (not family+type) |
| Use visible Revit name as IFCEntity name | CheckBox | Unchecked | Use visible UI name instead of internal name |

### Bottom Section: Import/Export Settings

| Control | Type | Description |
|---------|------|-------------|
| Import... | Button | Import IFC configuration from file |
| Export... | Button | Export current IFC configuration to file |

### XAML Structure:
```xaml
<StackPanel Grid.Column="2">
    <GroupBox Header="Advanced">
        <StackPanel>
            <!-- 11 CheckBoxes -->
        </StackPanel>
    </GroupBox>
    
    <GroupBox Header="Import/Export Settings">
        <StackPanel Orientation="Horizontal">
            <Button Content="Import..." Width="100"/>
            <Button Content="Export..." Width="100"/>
        </StackPanel>
    </GroupBox>
</StackPanel>
```

## Format Icons Row

Identical to NWC Settings tab, but **IFC highlighted**:

| Format | Background | Border | Icon | Foreground | Highlighted |
|--------|-----------|--------|------|------------|-------------|
| PDF | #FFEBEE | None | 📄 | #D32F2F | No |
| DWG | #E3F2FD | None | 📐 | #1976D2 | No |
| DGN | #F3E5F5 | None | 📋 | #7B1FA2 | No |
| DWF | #E8F5E9 | None | 📁 | #388E3C | No |
| NWC | #FFF9C4 | None | 🔧 | #F57F17 | No |
| **IFC** | **#E0F2F1** | **#00796B (2px)** | **🏗️** | **#00796B** | **Yes** |
| IMG | #FCE4EC | None | 🖼️ | #C2185B | No |

## Data Binding Requirements

### ExportSettings Properties Needed:

```csharp
// Format selection
public bool IsIfcSelected { get; set; }

// Setup & General
public string CurrentSetup { get; set; } = "<In-Session Setup>";
public string IFCVersion { get; set; } = "IFC 2x3 Coordination View 2.0";
public string IFCFileType { get; set; } = "IFC";
public string PhaseToExport { get; set; } = "Default phase to export";
public string SpaceBoundaries { get; set; } = "None";
public string ProjectOrigin { get; set; } = "Shared Coordinates";
public bool SplitWallsColumnsByLevel { get; set; } = false;
public bool IncludeSteelElements { get; set; } = true;

// Level of Details
public string DetailLevel { get; set; } = "Medium";

// Property Sets
public bool ExportRevitPropertySets { get; set; } = false;
public bool ExportIFCCommonPropertySets { get; set; } = true;
public bool ExportBaseQuantities { get; set; } = false;
public bool ExportSchedulesAsPsets { get; set; } = false;
public bool ExportOnlyIFCSchedules { get; set; } = false;
public bool ExportUserDefinedPsets { get; set; } = false;
public string UserDefinedPsetsPath { get; set; } = "";
public bool ExportParameterMapping { get; set; } = false;
public string ParameterMappingPath { get; set; } = "";

// Advanced
public bool ExportPartsAsBuildingElements { get; set; } = false;
public bool AllowMixedSolidModel { get; set; } = false;
public bool UseActiveView { get; set; } = false;
public bool UseFamilyAndTypeName { get; set; } = true;
public bool Use2DRoomBoundaries { get; set; } = false;
public bool IncludeIFCSITEElevation { get; set; } = false;
public bool StoreIFCGUID { get; set; } = false;
public bool ExportBoundingBox { get; set; } = false;
public bool KeepTessellatedGeometry { get; set; } = false;
public bool UseTypeNameOnly { get; set; } = false;
public bool UseVisibleRevitName { get; set; } = false;
```

## Control Naming Convention

All controls suffix with `IFC` để tránh conflict với các tab khác:

```xaml
<!-- Format Icons -->
x:Name="PDFCheckIFC"
x:Name="DWGCheckIFC"
x:Name="IFCCheckIFC"

<!-- ComboBoxes -->
x:Name="CurrentSetupComboIFC"
x:Name="IFCVersionComboIFC"
x:Name="IFCFileTypeComboIFC"
x:Name="IFCPhaseComboIFC"
x:Name="SpaceBoundariesComboIFC"
x:Name="ProjectOriginComboIFC"
x:Name="DetailLevelComboIFC"

<!-- CheckBoxes -->
x:Name="ExportSchedulesCheckIFC"
x:Name="ExportUserPsetsCheckIFC"
x:Name="ExportParamMappingCheckIFC"
```

## Event Handlers (Code-Behind)

### ComboBox SelectionChanged Events:

```csharp
private void CurrentSetupComboIFC_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (CurrentSetupComboIFC.SelectedItem is ComboBoxItem selected)
    {
        ExportSettings.CurrentSetup = selected.Content.ToString();
        // Load setup configuration
    }
}

private void IFCVersionComboIFC_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (IFCVersionComboIFC.SelectedItem is ComboBoxItem selected)
    {
        ExportSettings.IFCVersion = selected.Content.ToString();
        // Update available options based on IFC version
    }
}

private void DetailLevelComboIFC_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (DetailLevelComboIFC.SelectedItem is ComboBoxItem selected)
    {
        ExportSettings.DetailLevel = selected.Content.ToString();
    }
}
```

### CheckBox Checked/Unchecked Events:

```csharp
private void ExportSchedulesCheckIFC_CheckedChanged(object sender, RoutedEventArgs e)
{
    // Enable/disable child checkbox "Export only schedules containing IFC..."
    bool isChecked = ExportSchedulesCheckIFC.IsChecked == true;
    // Find and enable/disable dependent checkbox
}

private void ExportUserPsetsCheckIFC_CheckedChanged(object sender, RoutedEventArgs e)
{
    // Enable/disable TextBox and Browse button
    bool isChecked = ExportUserPsetsCheckIFC.IsChecked == true;
    // Enable/disable user-defined Psets path controls
}

private void ExportParamMappingCheckIFC_CheckedChanged(object sender, RoutedEventArgs e)
{
    // Enable/disable TextBox and Browse button for parameter mapping
    bool isChecked = ExportParamMappingCheckIFC.IsChecked == true;
    // Enable/disable parameter mapping path controls
}
```

### Button Click Events:

```csharp
private void ClassificationSettingsButton_Click(object sender, RoutedEventArgs e)
{
    // Open Classification Settings dialog
    // Configure classification systems (e.g., Uniclass, Omniclass)
}

private void ImportIFCSettings_Click(object sender, RoutedEventArgs e)
{
    // Open file dialog to import IFC configuration (.ifcexportconfig)
    OpenFileDialog dialog = new OpenFileDialog();
    dialog.Filter = "IFC Export Config (*.ifcexportconfig)|*.ifcexportconfig";
    if (dialog.ShowDialog() == true)
    {
        // Load configuration
    }
}

private void ExportIFCSettings_Click(object sender, RoutedEventArgs e)
{
    // Open file dialog to export IFC configuration
    SaveFileDialog dialog = new SaveFileDialog();
    dialog.Filter = "IFC Export Config (*.ifcexportconfig)|*.ifcexportconfig";
    if (dialog.ShowDialog() == true)
    {
        // Save configuration
    }
}
```

## Testing Checklist

### Visual Testing:
- [ ] Format Icons row hiển thị đúng 7 formats
- [ ] IFC icon highlighted với border xanh lá (#00796B, 2px)
- [ ] 3 columns layout đều đặn (equal width)
- [ ] GroupBox borders render đúng (#E1E1E1)
- [ ] All controls align properly
- [ ] ScrollViewer hoạt động khi nội dung dài

### Functional Testing:
- [ ] Tab enable khi IFC checkbox được tick
- [ ] Tab disable khi IFC checkbox bị untick
- [ ] All ComboBoxes có đủ options
- [ ] Default values được set đúng
- [ ] CheckBoxes toggle correctly
- [ ] Dependent controls enable/disable theo parent checkbox
- [ ] Buttons respond to clicks

### Integration Testing:
- [ ] Data binding với ExportSettings
- [ ] Property changes trigger INotifyPropertyChanged
- [ ] Configuration import/export functionality
- [ ] Integration với IFCExportManager
- [ ] Actual IFC export với selected settings

## Styling

### Colors:
- GroupBox Border: `#E1E1E1`
- IFC Highlighted Border: `#00796B` (2px thickness)
- IFC Background: `#E0F2F1`
- IFC Foreground: `#00796B`

### Fonts:
- Labels: `FontSize="13"`
- ComboBox/TextBox: `FontSize="13"`
- CheckBox: `FontSize="13"` (or `FontSize="12"` for long text)
- Buttons: `FontSize="13"`

### Spacing:
- StackPanel Margin: `10`
- Control Margin: `0,0,0,8` (bottom spacing between controls)
- Section Margin: `0,0,0,15` (larger gap between sections)
- Button Height: `28`
- ComboBox Width: `160` (Column 1), `140` (Column 2)
- TextBox Width: `200` (with Browse button)

## Build Status

✅ **Build Succeeded**
- Configuration: Debug
- Errors: 0
- Warnings: 8 (existing warnings, not related to IFC tab)
- BAML: Generated successfully
- DLL: `bin\Debug\ProSheetsAddin.dll`

## IFC Version Comparison

| Version | Full Name | Use Case | MVD |
|---------|-----------|----------|-----|
| IFC 2x3 | IFC 2x3 Coordination View 2.0 | Coordination, BIM collaboration | CV 2.0 |
| IFC 4 | IFC 4 Reference View | Reference models, viewing | RV |
| IFC 2x3 | IFC 2x3 Coordination View | Legacy coordination | CV 1.0 |
| IFC 4 | IFC 4 Design Transfer View | Detailed design transfer | DTV |

## File Type Comparison

| Type | Extension | Compression | Use Case |
|------|-----------|-------------|----------|
| IFC | .ifc | No | Standard, widely supported |
| IFCZIP | .ifczip | Yes (ZIP) | Large files, faster transfer |
| IFCXML | .ifcxml | No | XML-based, human-readable |

## Space Boundaries Explanation

| Level | Description | Use Case |
|-------|-------------|----------|
| None | No space boundaries | Simple geometry export |
| 1st Level | Physical boundaries | Energy analysis, basic space definition |
| 2nd Level | Logical boundaries | Advanced energy analysis, HVAC design |

## Related Files

- `Views/ProSheetsMainWindow.xaml` - Lines 1007-1297 (IFC Settings tab definition)
- `Models/IFCExportSettings.cs` - IFC export settings model
- `Managers/IFCExportManager.cs` - IFC export logic
- `Views/ProSheetsMainWindow.xaml.cs` - Event handlers and data binding
- `IFC_TAB_QUICKREF.md` - Quick reference guide (to be created)

## Usage Example

1. User opens ProSheets → Tab **Formats**
2. User ticks **🏗️ IFC** checkbox in Format Icons row
3. Tab **"IFC Settings"** becomes enabled and clickable
4. User clicks **"IFC Settings"** tab
5. User configures:
   - IFC Version: `IFC 4 Reference View`
   - File Type: `IFCZIP`
   - Project Origin: `Site Survey Point`
   - Check: `Export IFC Common property sets`
   - Check: `Export base quantities`
   - Check: `Use family and type name for reference`
6. User clicks **Export...** button to save configuration
7. Settings are applied to actual IFC export

## Next Steps

1. ✅ **UI Layout**: Complete 3-column layout implemented
2. ⏳ **Data Binding**: Add all ExportSettings properties
3. ⏳ **Event Handlers**: Implement ComboBox/CheckBox/Button events
4. ⏳ **Dependent Controls**: Enable/disable logic for child controls
5. ⏳ **Configuration I/O**: Import/Export IFC settings files
6. ⏳ **Integration**: Connect with IFCExportManager
7. ⏳ **Testing**: Full functional testing in Revit
8. ⏳ **Advanced Features**: Classification settings dialog, file header editor

---
**Date**: October 2, 2025  
**Build**: Debug Configuration, .NET Framework 4.8, Revit 2023 API  
**Status**: ✅ XAML complete, ready for data binding and event handlers  
**Total Lines**: 290+ lines of XAML for complete IFC Settings tab
