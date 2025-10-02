# Format Tab - PDF Export Settings Documentation

## Tổng Quan

Tab **Format** trong ProSheets Addin đã được redesign hoàn toàn với giao diện chuyên nghiệp, tổ chức rõ ràng theo các nhóm chức năng giống Revit gốc.

## Cấu Trúc Giao Diện

### 1. **Format Icons Row** (Hàng Icon Định Dạng)

Hiển thị các định dạng xuất file với checkbox và icon trực quan:

```
📄 PDF  📐 DWG  📋 DGN  📁 DWF  🔧 NWC  🏗️ IFC  🖼️ IMG
```

**Đặc điểm:**
- Mỗi format có border màu riêng (PDF: cam, DWG: xanh dương, DGN: tím, etc.)
- CheckBox để bật/tắt định dạng
- Icon emoji sinh động
- Text màu tương ứng với border

**Binding:**
```xaml
PDF:  IsChecked="{Binding ExportSettings.IsPdfSelected}"
DWG:  IsChecked="{Binding ExportSettings.IsDwgSelected}"
IFC:  IsChecked="{Binding ExportSettings.IsIfcSelected}"
IMG:  IsChecked="{Binding ExportSettings.IsImageSelected}"
```

---

### 2. **PDF Options Panel** (3 Cột)

#### **Column 1: Paper Placement**

**Chức năng:** Điều chỉnh vị trí bản vẽ trên trang giấy

**Options:**
- ⚪ **Center** (mặc định) - Căn giữa
- ⚪ **Offset from corner** - Lệch từ góc
  - ComboBox: No Margin / Small / Large
  - TextBox: X offset (số)
  - TextBox: Y offset (số)

**XAML Structure:**
```xaml
<GroupBox Header="Paper Placement">
    <RadioButton Content="Center" IsChecked="True"/>
    <RadioButton Content="Offset from corner"/>
    <ComboBox Name="MarginCombo">
        <ComboBoxItem Content="No Margin" IsSelected="True"/>
        <ComboBoxItem Content="Small"/>
        <ComboBoxItem Content="Large"/>
    </ComboBox>
    <TextBox Width="50" Height="25"/> <!-- X -->
    <TextBox Width="50" Height="25"/> <!-- Y -->
</GroupBox>
```

---

#### **Column 2: Hidden Line Views + Appearance**

**Section 1: Hidden Line Views**

**Chức năng:** Xử lý đường ẩn trong views

**Options:**
- ⚪ **Vector Processing** (mặc định) - Xử lý vector
- ⚪ **Raster Processing** - Xử lý raster

**Section 2: Appearance**

**Chức năng:** Cài đặt chất lượng và màu sắc

**Controls:**
1. **Raster Quality** (ComboBox)
   - Low
   - Medium
   - **High** (mặc định)

2. **Colors** (ComboBox)
   - **Color** (mặc định)
   - Black and White
   - Grayscale

**XAML Structure:**
```xaml
<GroupBox Header="Hidden Line Views">
    <!-- Remove Lines Using -->
    <RadioButton Content="Vector Processing" IsChecked="True"/>
    <RadioButton Content="Raster Processing"/>
    
    <!-- Appearance -->
    <ComboBox Name="RasterQualityCombo">
        <ComboBoxItem Content="High" IsSelected="True"/>
    </ComboBox>
    <ComboBox Name="ColorsCombo">
        <ComboBoxItem Content="Color" IsSelected="True"/>
    </ComboBox>
</GroupBox>
```

---

#### **Column 3: Options**

**Chức năng:** Các tùy chọn hiển thị bổ sung

**CheckBoxes (7 options):**

1. ☐ View links in blue (Color prints only)
2. ☑️ Hide ref/work planes
3. ☑️ Hide unreferenced view tags
4. ☑️ Hide scope boxes
5. ☑️ **Hide crop boundaries** - Binding: `{Binding ExportSettings.HideCropBoundaries}`
6. ☐ Replace halftone with thin lines
7. ☑️ Region edges mask coincident lines

**XAML Structure:**
```xaml
<GroupBox Header="Options">
    <CheckBox Content="View links in blue (Color prints only)"/>
    <CheckBox Content="Hide ref/work planes" IsChecked="True"/>
    <CheckBox Content="Hide unreferenced view tags" IsChecked="True"/>
    <CheckBox Content="Hide scope boxes" IsChecked="True"/>
    <CheckBox Content="Hide crop boundaries" 
              IsChecked="{Binding ExportSettings.HideCropBoundaries}"/>
    <CheckBox Content="Replace halftone with thin lines"/>
    <CheckBox Content="Region edges mask coincident lines" IsChecked="True"/>
</GroupBox>
```

---

### 3. **Bottom Section** (3 Cột)

#### **Column 1: Zoom**

**Chức năng:** Điều chỉnh tỷ lệ xuất

**Options:**
- ⚪ Fit to Page
- ⚪ **Zoom** (mặc định)
  - TextBox: `100` %
  - Label: "% Size"

**XAML:**
```xaml
<GroupBox Header="Zoom">
    <RadioButton Content="Fit to Page"/>
    <RadioButton Content="Zoom" IsChecked="True"/>
    <TextBox Width="60" Text="100"/>
    <TextBlock Text="% Size"/>
</GroupBox>
```

---

#### **Column 2: Printer**

**Chức năng:** Chọn printer/PDF driver

**ComboBox Options:**
- **PDF24** (mặc định)
- Adobe PDF
- Bluebeam PDF
- Microsoft Print to PDF

**XAML:**
```xaml
<GroupBox Header="Printer">
    <ComboBox Name="PrinterCombo">
        <ComboBoxItem Content="PDF24" IsSelected="True"/>
        <ComboBoxItem Content="Adobe PDF"/>
        <ComboBoxItem Content="Bluebeam PDF"/>
        <ComboBoxItem Content="Microsoft Print to PDF"/>
    </ComboBox>
</GroupBox>
```

---

#### **Column 3: File**

**Chức năng:** Cài đặt file output

**Options:**

1. **RadioButtons:**
   - ⚪ **Create separate files** (mặc định)
   - ⚪ Combine multiple views/sheets into a single file

2. **CheckBox:**
   - ☐ Keep Paper Size & Orientation

3. **Buttons (Enabled khi tick checkbox):**
   - 🔘 **Custom File Name** - Mở CustomFileNameDialog
   - 🔘 **Order sheets and views**

**XAML:**
```xaml
<GroupBox Header="File">
    <RadioButton Content="Create separate files" IsChecked="True"/>
    <RadioButton Content="Combine multiple views/sheets into a single file"/>
    
    <CheckBox Name="KeepPaperSizeCheckBox" 
              Content="Keep Paper Size &amp; Orientation"/>
    
    <Button Content="Custom File Name" 
            Click="EditSelectedFilenames_Click"
            IsEnabled="{Binding ElementName=KeepPaperSizeCheckBox, Path=IsChecked}"/>
    <Button Content="Order sheets and views" 
            IsEnabled="{Binding ElementName=KeepPaperSizeCheckBox, Path=IsChecked}"/>
</GroupBox>
```

---

## Data Binding

### ExportSettings Properties

Cần đảm bảo class `ExportSettings` có các properties sau:

```csharp
public class ExportSettings : INotifyPropertyChanged
{
    // Format Selection
    public bool IsPdfSelected { get; set; }
    public bool IsDwgSelected { get; set; }
    public bool IsDgnSelected { get; set; }
    public bool IsDwfSelected { get; set; }
    public bool IsNwcSelected { get; set; }
    public bool IsIfcSelected { get; set; }
    public bool IsImageSelected { get; set; }
    
    // PDF Options
    public bool HideCropBoundaries { get; set; }
    public string PaperSize { get; set; }
    public int Resolution { get; set; }
    
    // Other settings...
}
```

---

## Event Handlers

### Code-Behind Methods

```csharp
// Custom File Name button
private void EditSelectedFilenames_Click(object sender, RoutedEventArgs e)
{
    // Mở CustomFileNameDialog
    var dialog = new CustomFileNameDialog(_doc, _uiApp);
    if (dialog.ShowDialog() == true)
    {
        // Apply configuration to selected sheets/views
        var config = dialog.SelectedParameters;
        ApplyCustomFileNameToSheets(SelectedSheets, config);
    }
}

// Browse Output Folder button
private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
{
    var dialog = new System.Windows.Forms.FolderBrowserDialog();
    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    {
        OutputFolder = dialog.SelectedPath;
    }
}
```

---

## Style Guide

### GroupBox Styling

```xaml
<GroupBox Header="Header Text" 
          BorderBrush="#E1E1E1" 
          BorderThickness="1">
    <StackPanel Margin="10">
        <!-- Content -->
    </StackPanel>
</GroupBox>
```

### Control Sizing

- **CheckBox/RadioButton FontSize:** 13
- **TextBox Height:** 25
- **Button Height:** 30
- **ComboBox Width:** 120-150

### Spacing

- **Margin between groups:** 5-10
- **Margin between controls:** 5-8
- **Inner padding:** 10

---

## Testing Checklist

### Visual Tests

- [ ] Format icons hiển thị đúng màu và icon
- [ ] 3 cột Paper Placement / Hidden Line / Options căn chỉnh đều
- [ ] 3 cột Zoom / Printer / File có height đồng đều
- [ ] GroupBox borders render đúng
- [ ] Scrollbar xuất hiện khi window nhỏ

### Functional Tests

- [ ] CheckBox format icons toggle đúng
- [ ] Radio buttons Paper Placement toggle đúng
- [ ] ComboBox Margin/Quality/Colors mở được
- [ ] TextBox X/Y nhập được số
- [ ] Radio buttons File options toggle đúng
- [ ] Checkbox "Keep Paper Size" enable/disable buttons
- [ ] Button "Custom File Name" mở dialog
- [ ] Data binding với ExportSettings hoạt động

### Integration Tests

- [ ] Export PDF với settings từ UI
- [ ] Custom File Name áp dụng đúng
- [ ] Hide Crop Boundaries binding đúng
- [ ] Format selection (PDF/DWG/IFC/IMG) trigger đúng tab

---

## Known Issues

### Issue 1: ComboBox Width

**Problem:** ComboBox có thể bị thu nhỏ khi window resize

**Solution:** Đặt MinWidth cho ComboBox
```xaml
<ComboBox MinWidth="100" Width="120"/>
```

### Issue 2: Button Enable State

**Problem:** Buttons không enable khi checkbox checked

**Solution:** Kiểm tra binding ElementName
```xaml
IsEnabled="{Binding ElementName=KeepPaperSizeCheckBox, Path=IsChecked}"
```

---

## Next Steps

### Phase 1: UI Enhancements
1. ✅ Format icons row
2. ✅ Paper Placement group
3. ✅ Hidden Line Views group
4. ✅ Options group
5. ✅ Zoom/Printer/File groups

### Phase 2: Functionality
1. ⏳ Connect Margin ComboBox to export logic
2. ⏳ Connect X/Y offset TextBox to export
3. ⏳ Connect Vector/Raster processing to export
4. ⏳ Connect Raster Quality to export
5. ⏳ Connect Colors ComboBox to export
6. ⏳ Connect Zoom percentage to export
7. ⏳ Connect Printer selection to export

### Phase 3: Advanced Features
1. ⏳ Order sheets and views dialog
2. ⏳ Save/Load preset configurations
3. ⏳ Batch apply settings to multiple formats
4. ⏳ Real-time preview

---

## Conclusion

Tab Format mới đã được redesign với:

✅ **7 format icons** với màu sắc riêng biệt  
✅ **3 nhóm PDF options** (Paper/Hidden Line/Options)  
✅ **3 nhóm bottom** (Zoom/Printer/File)  
✅ **Custom File Name integration** đã hoạt động  
✅ **Data binding** cho ExportSettings  
✅ **Professional styling** với GroupBox và borders  

**Build Status:** ✅ 0 Errors, 8 Warnings  
**Ready for Testing:** YES  
**Deploy to Revit:** Copy `bin\Debug\ProSheetsAddin.dll` to Revit Addins folder

---

## References

- **CustomFileNameDialog:** `Views/CustomFileNameDialog.xaml`
- **ProSheetsMainWindow:** `Views/ProSheetsMainWindow.xaml`
- **Documentation:** `CUSTOM_FILENAME_DIALOG.md`
- **Quickstart:** `CUSTOM_FILENAME_QUICKSTART.md`

---

**Last Updated:** October 2, 2025  
**Version:** 1.0.0  
**Author:** ProSheets Development Team
