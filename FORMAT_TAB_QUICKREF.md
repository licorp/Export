# FORMAT TAB - QUICK REFERENCE

## 🎨 Layout Overview

```
┌─────────────────────────────────────────────────────────────────┐
│ 📄 PDF  📐 DWG  📋 DGN  📁 DWF  🔧 NWC  🏗️ IFC  🖼️ IMG          │ ← Format Icons
├─────────────────────────────────────────────────────────────────┤
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐            │
│ │Paper         │ │Hidden Line   │ │Options       │            │
│ │Placement     │ │Views         │ │              │            │
│ │              │ │              │ │ ☑️ Hide ref   │            │
│ │⚪ Center     │ │⚪ Vector     │ │ ☑️ Hide tags  │            │
│ │⚪ Offset     │ │⚪ Raster     │ │ ☑️ Hide boxes │            │
│ │              │ │              │ │ ☑️ Hide crop  │            │
│ └──────────────┘ └──────────────┘ └──────────────┘            │
│                                                                 │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐            │
│ │Zoom          │ │Printer       │ │File          │            │
│ │              │ │              │ │              │            │
│ │⚪ Fit Page   │ │ [PDF24 ▼]    │ │⚪ Separate   │            │
│ │⚪ 100%       │ │              │ │⚪ Combine     │            │
│ │              │ │              │ │              │            │
│ │              │ │              │ │🔘 Custom     │            │
│ │              │ │              │ │🔘 Order      │            │
│ └──────────────┘ └──────────────┘ └──────────────┘            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Access Features

### Format Icons (Top Row)
| Icon | Format | Binding Property |
|------|--------|------------------|
| 📄 | PDF | `IsPdfSelected` |
| 📐 | DWG | `IsDwgSelected` |
| 📋 | DGN | - |
| 📁 | DWF | - |
| 🔧 | NWC | - |
| 🏗️ | IFC | `IsIfcSelected` |
| 🖼️ | IMG | `IsImageSelected` |

---

## 📐 Paper Placement

**Options:**
- ⚪ **Center** (default)
- ⚪ **Offset from corner**
  - Margin: No Margin / Small / Large
  - X offset: `___` (number)
  - Y offset: `___` (number)

---

## 🔧 Hidden Line Views

**Remove Lines Using:**
- ⚪ **Vector Processing** (default)
- ⚪ Raster Processing

**Appearance:**
- **Raster Quality:** Low / Medium / **High**
- **Colors:** **Color** / Black and White / Grayscale

---

## ✅ Options (CheckBoxes)

| Status | Option |
|--------|--------|
| ☐ | View links in blue (Color prints only) |
| ☑️ | Hide ref/work planes |
| ☑️ | Hide unreferenced view tags |
| ☑️ | Hide scope boxes |
| ☑️ | Hide crop boundaries |
| ☐ | Replace halftone with thin lines |
| ☑️ | Region edges mask coincident lines |

---

## 🔍 Zoom

**Options:**
- ⚪ Fit to Page
- ⚪ **Zoom:** `100` **% Size**

---

## 🖨️ Printer

**Available Printers:**
- **PDF24** (default)
- Adobe PDF
- Bluebeam PDF
- Microsoft Print to PDF

---

## 📁 File Options

**File Creation:**
- ⚪ **Create separate files** (default)
- ⚪ Combine multiple views/sheets into a single file

**Additional Options:**
- ☐ Keep Paper Size & Orientation

**Buttons (enabled when checkbox checked):**
1. 🔘 **Custom File Name** → Opens parameter dialog
2. 🔘 **Order sheets and views** → Opens ordering dialog

---

## 🔗 Key Bindings

### Code-Behind Events

```csharp
// Button Click Events
EditSelectedFilenames_Click        // Custom File Name
BrowseOutputFolder_Click           // Browse button

// CheckBox Bindings
PDFCheck.IsChecked                 // → ExportSettings.IsPdfSelected
DWGCheck.IsChecked                 // → ExportSettings.IsDwgSelected
IFCCheck.IsChecked                 // → ExportSettings.IsIfcSelected
IMGCheck.IsChecked                 // → ExportSettings.IsImageSelected

// Option Bindings
HideCropBoundaries                 // → ExportSettings.HideCropBoundaries
```

---

## 📊 Control Sizes

| Control | Size |
|---------|------|
| CheckBox/RadioButton Font | 13 |
| TextBox Height | 25 |
| Button Height | 30 |
| ComboBox Width | 120 |
| GroupBox Padding | 10 |

---

## 🎯 Common Use Cases

### 1️⃣ Export Single PDF per Sheet
```
✅ PDF format
⚪ Create separate files
🔘 Click "Create" tab → Export
```

### 2️⃣ Export Combined PDF
```
✅ PDF format
⚪ Combine multiple views/sheets
☑️ Keep Paper Size & Orientation
🔘 Click "Create" tab → Export
```

### 3️⃣ Custom File Naming
```
✅ PDF format
☑️ Keep Paper Size & Orientation
🔘 Click "Custom File Name"
   → Select parameters (Sheet Number, Name, etc.)
   → Click OK
🔘 Click "Create" tab → Export
```

### 4️⃣ High Quality Color PDF
```
✅ PDF format
⚪ Vector Processing
🎚️ Raster Quality: High
🎨 Colors: Color
🔘 Click "Create" tab → Export
```

### 5️⃣ Multiple Formats at Once
```
✅ PDF + DWG + IFC
⚪ Create separate files
🔘 Click "Create" tab → Export
   (creates 3 files per sheet)
```

---

## ⚠️ Important Notes

### Button Enable Logic
```xaml
<!-- Buttons only enabled when checkbox is checked -->
IsEnabled="{Binding ElementName=KeepPaperSizeCheckBox, Path=IsChecked}"
```

### Format Icon States
- **Checked + Orange background:** PDF active
- **Checked + Blue background:** DWG active
- **Unchecked + Gray:** Format disabled

---

## 🐛 Troubleshooting

### Issue: Buttons are disabled
**Solution:** Check "Keep Paper Size & Orientation" checkbox

### Issue: Format icon not working
**Solution:** Verify binding to `ExportSettings.Is[Format]Selected` property

### Issue: ComboBox empty
**Solution:** Check ComboBoxItem elements in XAML

---

## 📦 Build Status

```
✅ Build: Success
✅ Errors: 0
⚠️ Warnings: 8 (unused variables)
✅ DLL: bin\Debug\ProSheetsAddin.dll
✅ Deploy: Copy to %AppData%\Autodesk\Revit\Addins\2023\
```

---

## 🔗 Related Files

- **XAML:** `Views/ProSheetsMainWindow.xaml` (line 639+)
- **Code-Behind:** `Views/ProSheetsMainWindow.xaml.cs`
- **Full Doc:** `FORMAT_TAB_DOCUMENTATION.md`
- **Custom Dialog:** `CUSTOM_FILENAME_DIALOG.md`

---

**Quick Links:**
- [Full Documentation](FORMAT_TAB_DOCUMENTATION.md)
- [Custom File Name Guide](CUSTOM_FILENAME_QUICKSTART.md)
- [Troubleshooting](CUSTOM_FILENAME_TROUBLESHOOTING.md)

---

**Version:** 1.0.0  
**Last Updated:** October 2, 2025  
**Status:** ✅ Ready for Testing
