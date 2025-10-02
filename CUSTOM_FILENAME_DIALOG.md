# Custom File Name Dialog

## 📋 Overview

The **Custom File Name Dialog** is an advanced feature in ProSheets that allows users to create dynamic file naming templates using Revit parameters. This dialog provides a flexible way to configure custom file names based on sheet/view parameters with prefix, suffix, and separator options.

## 🎯 Features

### 1. **Parameter Selection**
- Browse and search through all available Revit parameters
- Categories include:
  - Identity Data (Sheet Number, Sheet Name, Current Revision, etc.)
  - Project Information (Project Name, Client Name, etc.)
  - IFC Parameters
  - Custom/Shared Parameters from the project

### 2. **Parameter Configuration**
- **Prefix**: Add text before parameter value
- **Suffix**: Add text after parameter value  
- **Separator**: Define character(s) to join multiple parameters (default: `-`)
- **Sample Value**: Preview of parameter value

### 3. **Parameter Management**
- **Add**: Add parameter to configuration (double-click or click + button)
- **Remove**: Remove parameter from configuration
- **Move Up/Down**: Reorder parameters in the naming template
- **Refresh**: Reload parameters from current document

### 4. **Live Preview**
- Real-time preview of the generated filename
- Updates automatically as you configure parameters
- Shows exactly how the filename will appear

### 5. **Default Configuration**
On opening, the dialog loads with a default template:
```
Sheet Number - Current Revision - Sheet Name
Example: A101-Rev A-Floor Plan
```

## 🖥️ User Interface

### Left Panel: Available Parameters
```
┌─────────────────────────────────┐
│  All                            │
│  🔍 Search parameters...        │
│                                 │
│  ┌─────────────────────────┐   │
│  │ Approved By             │   │
│  │ Identity Data           │   │
│  │                         │   │
│  │ Checked By              │   │
│  │ Identity Data           │   │
│  │                         │   │
│  │ Sheet Number            │   │
│  │ Identity Data           │   │
│  │ ...                     │   │
│  └─────────────────────────┘   │
└─────────────────────────────────┘
```

### Right Panel: Configuration
```
┌─────────────────────────────────────────────────────────┐
│  Selected Parameters Configuration                      │
│  ┌───────────┬────────┬──────────┬────────┬──────────┐ │
│  │ Parameter │ Prefix │  Sample  │ Suffix │Separator │ │
│  │   Name    │        │  Value   │        │          │ │
│  ├───────────┼────────┼──────────┼────────┼──────────┤ │
│  │Sheet      │        │  A101    │        │    -     │ │
│  │Number     │        │          │        │          │ │
│  ├───────────┼────────┼──────────┼────────┼──────────┤ │
│  │Current    │        │  Rev A   │        │    -     │ │
│  │Revision   │        │          │        │          │ │
│  ├───────────┼────────┼──────────┼────────┼──────────┤ │
│  │Sheet Name │        │Floor Plan│        │          │ │
│  └───────────┴────────┴──────────┴────────┴──────────┘ │
│                                                          │
│  [↑] [↓] [+] [-] [⟳]                                   │
│                                                          │
│  ┌─ Custom Name Preview: ────────────────────────────┐ │
│  │  A101-Rev A-Floor Plan                            │ │
│  └───────────────────────────────────────────────────┘ │
│                                                          │
│                              [Cancel]  [OK]             │
└─────────────────────────────────────────────────────────┘
```

## 📝 Usage Instructions

### Basic Usage

1. **Open Dialog**: Click the "Custom File Name" header in the Sheets/Views DataGrid
2. **Select Parameter**: Click a parameter in the left list
3. **Add Parameter**: Double-click or click the `+` button
4. **Configure**: 
   - Enter Prefix (optional)
   - Enter Suffix (optional)
   - Set Separator (default: `-`)
5. **Preview**: Check the preview at the bottom
6. **Apply**: Click **OK** to apply to selected sheets/views

### Advanced Examples

#### Example 1: Project-Sheet-Revision
```
Configuration:
- Project Number | Prefix: "" | Suffix: "" | Separator: "-"
- Sheet Number   | Prefix: "" | Suffix: "" | Separator: "-"  
- Current Revision | Prefix: "Rev" | Suffix: "" | Separator: ""

Result: 2025-001-A101-RevA
```

#### Example 2: Discipline-Level-Number
```
Configuration:
- Sheet Number | Prefix: "" | Suffix: "" | Separator: "_"
  (Extract first letter as discipline)

Result: A_101_FloorPlan
```

#### Example 3: Date-Sheet-Name
```
Configuration:
- Sheet Issue Date | Prefix: "" | Suffix: "" | Separator: "_"
- Sheet Number     | Prefix: "" | Suffix: "" | Separator: "_"
- Sheet Name       | Prefix: "" | Suffix: "" | Separator: ""

Result: 2025-10-02_A101_Floor Plan
```

## 🔧 Technical Details

### Architecture

#### Models (`Models/ParameterInfo.cs`)
```csharp
public class ParameterInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Category { get; set; }
    public bool IsBuiltIn { get; set; }
}

public class SelectedParameterInfo
{
    public string ParameterName { get; set; }
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public string Separator { get; set; }
    public string SampleValue { get; set; }
}
```

#### Dialog (`Views/CustomFileNameDialog.xaml.cs`)
- **LoadAvailableParameters()**: Loads built-in and project parameters
- **ApplyParameterFilter()**: Filters parameters by search text
- **UpdatePreview()**: Generates live preview
- **GetSampleValue()**: Provides sample values for preview

#### Integration (`Views/ProSheetsMainWindow.xaml.cs`)
- **EditSelectedFilenames_Click()**: Opens dialog for selected items
- **ApplyCustomFileNameToSheets()**: Applies config to sheets
- **GenerateCustomFileName()**: Generates filename from parameters
- **GetSheetParameterValue()**: Retrieves actual parameter values

### Parameter Value Resolution

The dialog resolves parameter values in this order:

1. **Built-in Parameters**: Direct mapping (Sheet Number, Sheet Name, etc.)
2. **Element Parameters**: Searched by name in sheet/view parameters
3. **Project Information**: Checked in ProjectInformation element
4. **Fallback**: Empty string if not found

### Supported Parameter Types

| Storage Type | Conversion |
|-------------|------------|
| String      | AsString() |
| Integer     | AsInteger().ToString() |
| Double      | AsValueString() or AsDouble() |
| ElementId   | Element name lookup |

## 🎨 UI Customization

### DiRoots Branding
- Custom title bar with "DiRoots" logo
- Minimize and Close buttons
- Draggable title bar

### Color Scheme
```css
Primary: #0078D4 (Blue)
Background: #F8F8F8 (Light Gray)
Border: #CCCCCC (Gray)
Hover: #E0E0E0
Pressed: #D0D0D0
```

## ⚠️ Known Limitations

1. **Parameter Availability**: Only parameters present in the Revit document are loaded
2. **Empty Values**: If a parameter has no value, it will be skipped in the filename
3. **Special Characters**: Some characters may not be valid in filenames (\ / : * ? " < > |)
4. **Max Length**: Windows has a 260-character path limit

## 🔮 Future Enhancements

- [ ] Parameter categories filtering (Identity Data, Project Info, etc.)
- [ ] Save/Load filename templates
- [ ] Text case transformation (UPPER, lower, Title Case)
- [ ] Conditional parameter inclusion (only if not empty)
- [ ] Formula support (e.g., extract first N characters)
- [ ] Preview with multiple sheets simultaneously
- [ ] Export template to JSON

## 📚 Code Files

| File | Purpose |
|------|---------|
| `Models/ParameterInfo.cs` | Data models for parameters |
| `Views/CustomFileNameDialog.xaml` | Dialog UI definition |
| `Views/CustomFileNameDialog.xaml.cs` | Dialog logic and event handlers |
| `Views/ProSheetsMainWindow.xaml.cs` | Integration with main window |

## 🐛 Troubleshooting

### Issue: Parameters not showing
**Solution**: Click the Refresh button (⟳) to reload parameters from document

### Issue: Preview shows wrong values
**Solution**: Sample values are placeholders. Real values are applied when you click OK.

### Issue: Custom filename not applied
**Solution**: Ensure you have selected sheets/views before opening the dialog

### Issue: Dialog freezes when loading parameters
**Solution**: Large projects may take a few seconds to load all parameters

## 📖 See Also

- [ProSheets Main Documentation](README.md)
- [Revit 2024-2026 Compatibility](COMPATIBILITY.md)
- [Parameter Utils](Utils/ParameterUtils.cs)

---

**Version**: 1.0  
**Last Updated**: October 2, 2025  
**Compatible with**: Revit 2023-2026
