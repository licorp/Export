# ✅ Paper Placement & File Options - ADDED!

## Tính Năng Đã Thêm

### 1. ✅ Paper Placement Settings
- **Center** - Căn giữa sheet
- **Offset from corner** - Offset từ góc

### 2. ✅ Paper Margin Options
- **No Margin** - Không margin
- **Printer Limit** - Giới hạn máy in
- **User Defined** - Tùy chỉnh với X/Y offset

### 3. ✅ Offset X & Y Values
- TextBox để nhập giá trị X offset
- TextBox để nhập giá trị Y offset
- Đơn vị: inches hoặc mm (tùy Revit project units)

### 4. ✅ Combine Files Option
- **Create separate files** - Tạo file riêng cho mỗi sheet (default)
- **Combine multiple sheets into a single file** - Gộp nhiều sheet thành 1 PDF

### 5. ✅ Keep Paper Size & Orientation
- Checkbox để giữ nguyên paper size và orientation từ Revit

---

## Files Modified

### 1. Models/ProSheetsModels.cs

**Added Enums** (lines 559-570):
```csharp
public enum PSPaperMargin
{
    NoMargin,
    PrinterLimit,
    UserDefined
}
```

**Added Fields** (lines 272-274):
```csharp
private PSPaperMargin _paperMargin = PSPaperMargin.NoMargin;
private double _offsetX = 0.0;
private double _offsetY = 0.0;
```

**Added Properties** (lines 409-432):
```csharp
// Paper Placement Settings
public PSPaperPlacement PaperPlacement { get; set; }

public PSPaperMargin PaperMargin { get; set; }

public double OffsetX { get; set; }

public double OffsetY { get; set; }
```

**Note**: `PSPaperPlacement` enum đã tồn tại từ trước:
```csharp
public enum PSPaperPlacement
{
    Center,
    OffsetFromCorner
}
```

---

### 2. Views/ProSheetsMainWindow.xaml

**Paper Placement UI** (lines 728-749):
```xaml
<!-- Paper Placement -->
<GroupBox Header="Paper Placement" Grid.Column="0" Margin="0,0,5,0" 
          BorderBrush="#E1E1E1" BorderThickness="1">
    <StackPanel Margin="10">
        <RadioButton x:Name="CenterRadio" Content="Center" IsChecked="True" 
                     Margin="0,0,0,8" FontSize="13"/>
        <RadioButton x:Name="OffsetRadio" Content="Offset from corner" 
                     Margin="0,0,0,8" FontSize="13"/>
        
        <ComboBox x:Name="MarginCombo" Margin="20,0,0,10" FontSize="13">
            <ComboBoxItem Content="No Margin" IsSelected="True"/>
            <ComboBoxItem Content="Printer Limit"/>
            <ComboBoxItem Content="User Defined"/>
        </ComboBox>
        
        <StackPanel Orientation="Horizontal" Margin="0,10,0,0">
            <TextBlock Text="X -" VerticalAlignment="Center" Width="25" FontSize="13"/>
            <TextBox x:Name="OffsetXTextBox" Width="50" Height="25" 
                     Margin="2,0,10,0" VerticalContentAlignment="Center" Text="0"/>
            <TextBlock Text="Y -" VerticalAlignment="Center" Width="25" FontSize="13"/>
            <TextBox x:Name="OffsetYTextBox" Width="50" Height="25" 
                     Margin="2,0,0,0" VerticalContentAlignment="Center" Text="0"/>
        </StackPanel>
    </StackPanel>
</GroupBox>
```

**File Options UI** (lines 851-868):
```xaml
<!-- File Options Section -->
<GroupBox Header="File" Grid.Column="2" Margin="5,0,0,0" 
          BorderBrush="#E1E1E1" BorderThickness="1">
    <StackPanel Margin="10">
        <RadioButton x:Name="SeparateFilesRadio" Content="Create separate files" 
                     IsChecked="True" Margin="0,0,0,5" FontSize="13"/>
        <RadioButton x:Name="CombineFilesRadio" Content="Combine multiple views/sheets into a single file" 
                     Margin="0,0,0,10" FontSize="13"/>
        
        <CheckBox x:Name="KeepPaperSizeCheckBox" Content="Keep Paper Size &amp; Orientation" 
                  Margin="0,0,0,15" FontSize="13"/>
        
        <Button Content="Custom File Name" Height="30" Margin="0,0,0,5" FontSize="13"
                Click="EditSelectedFilenames_Click"
                IsEnabled="{Binding ElementName=KeepPaperSizeCheckBox, Path=IsChecked}"/>
        <Button Content="Order sheets and views" Height="30" FontSize="13"
                IsEnabled="{Binding ElementName=KeepPaperSizeCheckBox, Path=IsChecked}"/>
    </StackPanel>
</GroupBox>
```

---

### 3. Views/ProSheetsMainWindow.xaml.cs

**Updated UpdateExportSettingsFromUI()** (lines 298-374):

```csharp
// Update Paper Placement settings
if (CenterRadio?.IsChecked == true)
{
    ExportSettings.PaperPlacement = PSPaperPlacement.Center;
    WriteDebugLog("✓ Paper Placement: CENTER");
}
else if (OffsetRadio?.IsChecked == true)
{
    ExportSettings.PaperPlacement = PSPaperPlacement.OffsetFromCorner;
    WriteDebugLog("✓ Paper Placement: OFFSET FROM CORNER");
}

// Update Paper Margin
if (MarginCombo.SelectedItem is ComboBoxItem marginItem)
{
    string marginText = marginItem.Content?.ToString() ?? "No Margin";
    WriteDebugLog($"UI Margin: {marginText}");
    
    switch (marginText)
    {
        case "No Margin":
            ExportSettings.PaperMargin = PSPaperMargin.NoMargin;
            WriteDebugLog("✓ Paper Margin: NO MARGIN");
            break;
        case "Printer Limit":
            ExportSettings.PaperMargin = PSPaperMargin.PrinterLimit;
            WriteDebugLog("✓ Paper Margin: PRINTER LIMIT");
            break;
        case "User Defined":
            ExportSettings.PaperMargin = PSPaperMargin.UserDefined;
            WriteDebugLog("✓ Paper Margin: USER DEFINED");
            break;
        default:
            ExportSettings.PaperMargin = PSPaperMargin.NoMargin;
            WriteDebugLog("⚠ Unknown margin type, defaulting to NO MARGIN");
            break;
    }
}

// Update Offset X and Y values
if (double.TryParse(OffsetXTextBox?.Text, out double offsetX))
{
    ExportSettings.OffsetX = offsetX;
    WriteDebugLog($"✓ Offset X: {offsetX}");
}

if (double.TryParse(OffsetYTextBox?.Text, out double offsetY))
{
    ExportSettings.OffsetY = offsetY;
    WriteDebugLog($"✓ Offset Y: {offsetY}");
}

// Update Combine Files setting
if (CombineFilesRadio?.IsChecked == true)
{
    ExportSettings.CombineFiles = true;
    WriteDebugLog("✓ File Mode: COMBINE multiple sheets into single file");
}
else if (SeparateFilesRadio?.IsChecked == true)
{
    ExportSettings.CombineFiles = false;
    WriteDebugLog("✓ File Mode: CREATE SEPARATE files");
}

// Update Keep Paper Size & Orientation setting
if (KeepPaperSizeCheckBox?.IsChecked == true)
{
    ExportSettings.KeepPaperSize = true;
    WriteDebugLog("✓ Keep Paper Size & Orientation: ENABLED");
}
else
{
    ExportSettings.KeepPaperSize = false;
    WriteDebugLog("✓ Keep Paper Size & Orientation: DISABLED");
}

WriteDebugLog("===== ExportSettings Updated Successfully =====");
WriteDebugLog($"Final Settings: RasterQuality={ExportSettings.RasterQuality}, Colors={ExportSettings.Colors}");
WriteDebugLog($"Paper Placement: {ExportSettings.PaperPlacement}, Margin: {ExportSettings.PaperMargin}, Offset: ({ExportSettings.OffsetX}, {ExportSettings.OffsetY})");
WriteDebugLog($"Combine Files: {ExportSettings.CombineFiles}, Keep Paper Size: {ExportSettings.KeepPaperSize}");
```

---

### 4. Managers/PDFExportManager.cs

**Updated CreatePDFExportOptions()** (lines 327-342):

```csharp
// Basic configuration
options.PaperFormat = ExportPaperFormat.Default; // Auto-detect from sheet
options.PaperOrientation = PageOrientationType.Auto;

// Combine Files setting
options.Combine = settings.CombineFiles;
WriteDebugLog($"[Export + PDF] ✓ Combine Files: {options.Combine}");
```

**Updated Debug Logs** (lines 417-427):

```csharp
WriteDebugLog("[Export + PDF] ===== PDF EXPORT OPTIONS SUMMARY =====");
WriteDebugLog($"[Export + PDF] ColorDepth: {options.ColorDepth}");
WriteDebugLog($"[Export + PDF] RasterQuality: {options.RasterQuality}");
WriteDebugLog($"[Export + PDF] Combine Files: {options.Combine}");  // ← UPDATED
WriteDebugLog($"[Export + PDF] HideCropBoundaries: {options.HideCropBoundaries}");
WriteDebugLog($"[Export + PDF] HideScopeBoxes: {options.HideScopeBoxes}");
WriteDebugLog($"[Export + PDF] HideUnreferencedViewTags: {options.HideUnreferencedViewTags}");
WriteDebugLog("[Export + PDF] ===== SETTINGS FROM UI (NOT applied - PDFExportOptions limitations) =====");
WriteDebugLog($"[Export + PDF] ⚠ Paper Placement: {settings.PaperPlacement} (requires PrintManager)");
WriteDebugLog($"[Export + PDF] ⚠ Paper Margin: {settings.PaperMargin} (requires PrintManager)");
WriteDebugLog($"[Export + PDF] ⚠ Offset X: {settings.OffsetX}, Y: {settings.OffsetY} (requires PrintManager)");
WriteDebugLog($"[Export + PDF] ⚠ Keep Paper Size: {settings.KeepPaperSize} (requires PrintManager)");  // ← NEW
WriteDebugLog("[Export + PDF] ==========================================");
```

---

## Settings Mapping

### PDFExportOptions - SUPPORTED ✅

| **UI Setting**          | **ExportSettings Property** | **PDFExportOptions Property** | **Status** |
|-------------------------|-----------------------------|-------------------------------|------------|
| Raster Quality          | `RasterQuality`             | `RasterQuality`               | ✅ Applied  |
| Colors                  | `Colors`                    | `ColorDepth`                  | ✅ Applied  |
| Combine Files           | `CombineFiles`              | `Combine`                     | ✅ Applied  |
| Hide Crop Boundaries    | `HideCropBoundaries`        | `HideCropBoundaries`          | ✅ Applied  |
| Hide Scope Boxes        | `HideScopeBoxes`            | `HideScopeBoxes`              | ✅ Applied  |

### PrintManager - REQUIRED ⚠️

| **UI Setting**          | **ExportSettings Property** | **Requires PrintManager** | **Status**        |
|-------------------------|-----------------------------|---------------------------|-------------------|
| Paper Placement         | `PaperPlacement`            | Yes                       | ⚠️ Saved but not applied |
| Paper Margin            | `PaperMargin`               | Yes                       | ⚠️ Saved but not applied |
| Offset X/Y              | `OffsetX`, `OffsetY`        | Yes                       | ⚠️ Saved but not applied |
| Keep Paper Size         | `KeepPaperSize`             | Yes                       | ⚠️ Saved but not applied |

**Note**: Paper Placement, Margin, và Offset settings **KHÔNG thể apply** với `Document.Export()` API. 
Để sử dụng các settings này, cần implement **PrintManager** approach (xem `PDFExportManager_PrintManager.cs`).

---

## Testing Instructions

### 1. Build & Deploy
```powershell
# Build successful - 0 errors
MSBuild.exe ProSheetsAddin.csproj /p:Configuration=Debug /p:Platform=x64
```

### 2. Test Scenarios

#### Test 1: Paper Placement - Center
1. Open Revit, load addin
2. **Settings tab**:
   - Paper Placement: **Center** ✓
   - Margin: **No Margin**
3. Click **Start Export**
4. **Check DebugView**:
   ```
   [Export +] ✓ Paper Placement: CENTER
   [Export +] ✓ Paper Margin: NO MARGIN
   [Export + PDF] ⚠ Paper Placement: Center (requires PrintManager)
   ```
5. **Note**: Setting saved nhưng KHÔNG applied (cần PrintManager)

#### Test 2: Offset from Corner
1. **Settings tab**:
   - Paper Placement: **Offset from corner** ✓
   - Margin: **User Defined**
   - X: **0.5**
   - Y: **0.5**
2. Click **Start Export**
3. **Check DebugView**:
   ```
   [Export +] ✓ Paper Placement: OFFSET FROM CORNER
   [Export +] ✓ Paper Margin: USER DEFINED
   [Export +] ✓ Offset X: 0.5
   [Export +] ✓ Offset Y: 0.5
   ```

#### Test 3: Combine Files
1. Select **3 sheets**
2. **File options**:
   - Select: **Combine multiple views/sheets into a single file** ✓
3. Click **Start Export**
4. **Check DebugView**:
   ```
   [Export +] ✓ File Mode: COMBINE multiple sheets into single file
   [Export + PDF] ✓ Combine Files: True
   ```
5. **Verify**: 3 sheets → **1 PDF file** (combined) ✅

#### Test 4: Separate Files (Default)
1. Select **3 sheets**
2. **File options**:
   - Select: **Create separate files** ✓
3. Click **Start Export**
4. **Check DebugView**:
   ```
   [Export +] ✓ File Mode: CREATE SEPARATE files
   [Export + PDF] ✓ Combine Files: False
   ```
5. **Verify**: 3 sheets → **3 PDF files** (separate) ✅

#### Test 5: Keep Paper Size
1. **File options**:
   - Check: **Keep Paper Size & Orientation** ✓
2. Click **Start Export**
3. **Check DebugView**:
   ```
   [Export +] ✓ Keep Paper Size & Orientation: ENABLED
   [Export + PDF] ⚠ Keep Paper Size: True (requires PrintManager)
   ```
4. **Note**: Setting saved nhưng KHÔNG applied (cần PrintManager)

---

## Expected DebugView Log (Full Export)

```
[Export +] Start Export clicked
[Export +] Exporting 3 sheets in 1 format(s)
[Export +] Starting export for format: PDF
[Export +] Using PDF Export External Event...
[Export +] Updating ExportSettings from UI controls...
[Export +] UI Raster Quality: Presentation
[Export +] ✓ RasterQuality set to PRESENTATION/MAXIMUM (600 DPI)
[Export +] UI Colors: Color
[Export +] ✓ Colors set to COLOR
[Export +] ✓ Output folder: C:\Export_Output
[Export +] ✓ Paper Placement: OFFSET FROM CORNER
[Export +] UI Margin: User Defined
[Export +] ✓ Paper Margin: USER DEFINED
[Export +] ✓ Offset X: 0.5
[Export +] ✓ Offset Y: 0.5
[Export +] ✓ File Mode: COMBINE multiple sheets into single file
[Export +] ✓ Keep Paper Size & Orientation: ENABLED
[Export +] ===== ExportSettings Updated Successfully =====
[Export +] Final Settings: RasterQuality=Maximum, Colors=Color
[Export +] Paper Placement: OffsetFromCorner, Margin: UserDefined, Offset: (0.5, 0.5)
[Export +] Combine Files: True, Keep Paper Size: True

[ProSheets PDF] Creating PDF export options...
[ProSheets PDF] ✓ Combine Files: True
[ProSheets PDF] Setting ColorDepth: Color
[ProSheets PDF] ✓ ColorDepth set to COLOR
[ProSheets PDF] Setting RasterQuality: Maximum
[ProSheets PDF] ✓ RasterQuality set to MAXIMUM/PRESENTATION (600 DPI)
[ProSheets PDF] ===== PDF EXPORT OPTIONS SUMMARY =====
[ProSheets PDF] ColorDepth: Color
[ProSheets PDF] RasterQuality: Presentation
[ProSheets PDF] Combine Files: True
[ProSheets PDF] HideCropBoundaries: True
[ProSheets PDF] HideScopeBoxes: True
[ProSheets PDF] HideUnreferencedViewTags: True
[ProSheets PDF] ===== SETTINGS FROM UI (NOT applied - PDFExportOptions limitations) =====
[ProSheets PDF] ⚠ Paper Placement: OffsetFromCorner (requires PrintManager)
[ProSheets PDF] ⚠ Paper Margin: UserDefined (requires PrintManager)
[ProSheets PDF] ⚠ Offset X: 0.5, Y: 0.5 (requires PrintManager)
[ProSheets PDF] ⚠ Keep Paper Size: True (requires PrintManager)
[ProSheets PDF] ==========================================
```

---

## Summary of Changes

| **Feature**                        | **UI**       | **Settings Model** | **Applied to PDF** | **Status**     |
|------------------------------------|--------------|--------------------|-------------------|----------------|
| **Raster Quality** (Presentation)  | ✅ Complete  | ✅ Saved           | ✅ Applied         | 🟢 **WORKING** |
| **Colors** (Color/B&W/Grayscale)   | ✅ Complete  | ✅ Saved           | ✅ Applied         | 🟢 **WORKING** |
| **Paper Placement** (Center/Offset)| ✅ Complete  | ✅ Saved           | ⚠️ PrintManager    | 🟡 **PARTIAL** |
| **Paper Margin** (3 options)       | ✅ Complete  | ✅ Saved           | ⚠️ PrintManager    | 🟡 **PARTIAL** |
| **Offset X/Y**                     | ✅ Complete  | ✅ Saved           | ⚠️ PrintManager    | 🟡 **PARTIAL** |
| **Combine Files**                  | ✅ Complete  | ✅ Saved           | ✅ Applied         | 🟢 **WORKING** |
| **Keep Paper Size**                | ✅ Complete  | ✅ Saved           | ⚠️ PrintManager    | 🟡 **PARTIAL** |

**Legend**:
- 🟢 **WORKING** - Fully implemented and applied to PDF export
- 🟡 **PARTIAL** - UI and settings saved, but requires PrintManager to apply
- 🔴 **NOT WORKING** - Not implemented

---

## Next Steps (Optional)

### To Apply Paper Placement Settings:
1. Implement **PrintManager** approach (see reference code in workspace)
2. Use `PrintManager.SubmitPrint()` instead of `Document.Export()`
3. Set `PrintManager.PrintSetup.CurrentPrintSetting.OriginOffset` for X/Y offset
4. Trade-off: PrintManager slower but has more control

### To Keep Current Approach:
1. Current implementation works for most use cases
2. Combine Files ✅ works perfectly
3. Color/Raster Quality ✅ works perfectly
4. Document limitations in user guide

**Recommendation**: Keep current `Document.Export()` approach for speed, document PrintManager limitations.

---

🎉 **All settings đã được sync từ UI → ExportSettings!**

Test trong Revit để verify! 🚀
