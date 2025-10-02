# NWC Settings Tab Documentation

## ✅ Hoàn Thành

Đã thêm tab **NWC Settings** vào ProSheets với giao diện đầy đủ theo Navisworks export options.

---

## 🎨 Layout Overview

```
┌─────────────────────────────────────────────────────────────────┐
│ Tabs: Selection | Formats | Create                              │
├─────────────────────────────────────────────────────────────────┤
│ Sub-tabs: Format | DWG Settings | 🔧 NWC Settings                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ 📄 PDF  📐 DWG  📋 DGN  📁 DWF  🔧 NWC  🏗️ IFC  🖼️ IMG      │ │ ← Format Icons
│ └────────────────────────────────────────────────────────────┘ │
│                      (NWC highlighted with border)              │
│                                                                  │
│ ┌──────────────────────────┐ ┌──────────────────────────┐      │
│ │ Standard options         │ │ Revit 2020 & above       │      │
│ │                          │ │                          │      │
│ │ ☑️ Convert construction   │ │ ☐ Convert linked CAD     │      │
│ │ ☑️ Convert element Ids    │ │ ☐ Convert lights         │      │
│ │ Convert params: Elements │ │                          │      │
│ │ ☑️ Convert properties     │ │ Faceting factor: 1       │      │
│ │ ☐ Convert linked files   │ │                          │      │
│ │ ☑️ Convert room attribute │ │                          │      │
│ │ ☑️ Convert URLs           │ │                          │      │
│ │ Coordinates: Shared      │ │                          │      │
│ │ ☑️ Divide into levels     │ │                          │      │
│ │ ☐ Export room geometry   │ │                          │      │
│ │ ☑️ Find missing materials │ │                          │      │
│ └──────────────────────────┘ └──────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📂 Structure

### Tab Header
```xaml
<TabItem Header="NWC Settings" IsEnabled="{Binding ExportSettings.IsNwcSelected}">
```

**Enable Condition:** Tab chỉ active khi NWC format được chọn

---

### Format Icons Row

**Giống như tab Format, nhưng NWC được highlight:**

| Format | Status | Border | Background |
|--------|--------|--------|------------|
| 📄 PDF | Inactive | None | #FFF3E0 |
| 📐 DWG | Inactive | None | #E3F2FD |
| 📋 DGN | Inactive | None | #F3E5F5 |
| 📁 DWF | Inactive | None | #E8F5E9 |
| 🔧 **NWC** | **Active** | **#F57F17 (2px)** | **#FFF9C4** |
| 🏗️ IFC | Inactive | None | #E0F2F1 |
| 🖼️ IMG | Inactive | None | #FCE4EC |

**NWC Highlight Code:**
```xaml
<Border Background="#FFF9C4" CornerRadius="3" Padding="10,5" 
        BorderBrush="#F57F17" BorderThickness="2">
    <StackPanel Orientation="Horizontal">
        <CheckBox x:Name="NWCCheckNWC" IsChecked="True"/>
        <TextBlock Text="🔧" FontSize="20"/>
        <TextBlock Text="NWC" FontWeight="Bold" Foreground="#F57F17"/>
    </StackPanel>
</Border>
```

---

## 🛠️ Standard Options (Left Column)

### GroupBox Header
**"Standard options"**

### Options List (13 items)

| # | Option | Type | Default | Width |
|---|--------|------|---------|-------|
| 1 | Convert construction parts | CheckBox | ✅ Checked | - |
| 2 | Convert element Ids | CheckBox | ✅ Checked | - |
| 3 | Convert element parameters | ComboBox | Elements | 120px |
| 4 | Convert element properties | CheckBox | ✅ Checked | - |
| 5 | Convert linked files | CheckBox | ☐ Unchecked | - |
| 6 | Convert room as attribute | CheckBox | ✅ Checked | - |
| 7 | Convert URLs | CheckBox | ✅ Checked | - |
| 8 | Coordinates | ComboBox | Shared | 120px |
| 9 | Divide file into levels | CheckBox | ✅ Checked | - |
| 10 | Export room geometry | CheckBox | ☐ Unchecked | - |
| 11 | Try and find missing materials | CheckBox | ✅ Checked | - |

---

### ComboBox 1: Convert element parameters

```xaml
<StackPanel Orientation="Horizontal" Margin="0,0,0,8">
    <TextBlock Text="Convert element parameters" Width="180" FontSize="13"/>
    <ComboBox x:Name="ElementParamsCombo" Width="120">
        <ComboBoxItem Content="Elements" IsSelected="True"/>
        <ComboBoxItem Content="Instances"/>
        <ComboBoxItem Content="Types"/>
    </ComboBox>
</StackPanel>
```

**Options:**
- ⚪ **Elements** (default)
- ⚪ Instances
- ⚪ Types

---

### ComboBox 2: Coordinates

```xaml
<StackPanel Orientation="Horizontal" Margin="0,0,0,8">
    <TextBlock Text="Coordinates" Width="180" FontSize="13"/>
    <ComboBox x:Name="CoordinatesCombo" Width="120">
        <ComboBoxItem Content="Shared" IsSelected="True"/>
        <ComboBoxItem Content="Internal"/>
        <ComboBoxItem Content="Project Base"/>
        <ComboBoxItem Content="Survey"/>
    </ComboBox>
</StackPanel>
```

**Options:**
- ⚪ **Shared** (default)
- ⚪ Internal
- ⚪ Project Base
- ⚪ Survey

---

## 🚀 Revit 2020 & Above Options (Right Column)

### GroupBox Header
**"Revit 2020 & above"**

### Options List (3 items)

| # | Option | Type | Default | Width |
|---|--------|------|---------|-------|
| 1 | Convert linked CAD format | CheckBox | ☐ Unchecked | - |
| 2 | Convert lights | CheckBox | ☐ Unchecked | - |
| 3 | Faceting factor | TextBox | 1 | 80px |

---

### Faceting Factor Input

```xaml
<StackPanel Orientation="Horizontal">
    <TextBlock Text="Faceting factor" Width="120" FontSize="13"/>
    <TextBox x:Name="FacetingFactorBox" Width="80" Height="25" 
             Text="1" 
             VerticalContentAlignment="Center" 
             HorizontalContentAlignment="Right" 
             FontSize="13"/>
</StackPanel>
```

**Properties:**
- **Default Value:** 1
- **Alignment:** Right
- **Type:** Numeric input
- **Range:** Typically 0.1 - 15 (không validate trong XAML)

---

## 📊 XAML Structure Summary

```xaml
<TabItem Header="NWC Settings" IsEnabled="{Binding ExportSettings.IsNwcSelected}">
    <ScrollViewer>
        <Grid>
            <!-- Row 0: Format Icons -->
            <StackPanel Grid.Row="0">
                7 Format Borders (NWC highlighted)
            </StackPanel>
            
            <!-- Row 1: Options Grid -->
            <Grid Grid.Row="1">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>     <!-- Standard -->
                    <ColumnDefinition Width="*"/>     <!-- Revit 2020+ -->
                </Grid.ColumnDefinitions>
                
                <!-- Column 0: Standard options -->
                <GroupBox Grid.Column="0">
                    11 CheckBoxes + 2 ComboBoxes
                </GroupBox>
                
                <!-- Column 1: Revit 2020 & above -->
                <GroupBox Grid.Column="1">
                    2 CheckBoxes + 1 TextBox (Faceting factor)
                </GroupBox>
            </Grid>
        </Grid>
    </ScrollViewer>
</TabItem>
```

---

## 🔗 Data Binding Requirements

### ExportSettings Properties

Cần thêm các properties sau vào class `ExportSettings`:

```csharp
public class ExportSettings : INotifyPropertyChanged
{
    // Format selection
    public bool IsNwcSelected { get; set; }
    
    // Standard options
    public bool ConvertConstructionParts { get; set; } = true;
    public bool ConvertElementIds { get; set; } = true;
    public string ElementParameters { get; set; } = "Elements"; // Elements/Instances/Types
    public bool ConvertElementProperties { get; set; } = true;
    public bool ConvertLinkedFiles { get; set; } = false;
    public bool ConvertRoomAsAttribute { get; set; } = true;
    public bool ConvertUrls { get; set; } = true;
    public string Coordinates { get; set; } = "Shared"; // Shared/Internal/Project Base/Survey
    public bool DivideFileIntoLevels { get; set; } = true;
    public bool ExportRoomGeometry { get; set; } = false;
    public bool FindMissingMaterials { get; set; } = true;
    
    // Revit 2020 & above
    public bool ConvertLinkedCadFormat { get; set; } = false;
    public bool ConvertLights { get; set; } = false;
    public double FacetingFactor { get; set; } = 1.0;
}
```

---

## 🎯 Control Naming Convention

| Control Type | Name | Purpose |
|--------------|------|---------|
| CheckBox | `PDFCheckNWC` | PDF format trong NWC tab |
| CheckBox | `DWGCheckNWC` | DWG format trong NWC tab |
| CheckBox | `NWCCheckNWC` | NWC format (checked) |
| CheckBox | `IFCCheckNWC` | IFC format trong NWC tab |
| CheckBox | `IMGCheckNWC` | IMG format trong NWC tab |
| ComboBox | `ElementParamsCombo` | Convert element parameters |
| ComboBox | `CoordinatesCombo` | Coordinates selection |
| TextBox | `FacetingFactorBox` | Faceting factor input |

**Suffix `NWC`:** Tránh conflict với controls trong tab Format

---

## 📝 Event Handlers (Future)

### Code-Behind Template

```csharp
// Element Parameters ComboBox
private void ElementParamsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (ElementParamsCombo.SelectedItem is ComboBoxItem item)
    {
        ExportSettings.ElementParameters = item.Content.ToString();
    }
}

// Coordinates ComboBox
private void CoordinatesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (CoordinatesCombo.SelectedItem is ComboBoxItem item)
    {
        ExportSettings.Coordinates = item.Content.ToString();
    }
}

// Faceting Factor TextBox
private void FacetingFactorBox_TextChanged(object sender, TextChangedEventArgs e)
{
    if (double.TryParse(FacetingFactorBox.Text, out double value))
    {
        ExportSettings.FacetingFactor = value;
    }
}

// CheckBox events (example)
private void ConvertConstructionParts_Checked(object sender, RoutedEventArgs e)
{
    ExportSettings.ConvertConstructionParts = true;
}

private void ConvertConstructionParts_Unchecked(object sender, RoutedEventArgs e)
{
    ExportSettings.ConvertConstructionParts = false;
}
```

---

## 🧪 Testing Checklist

### Visual Tests
- [ ] Tab "NWC Settings" xuất hiện trong TabControl
- [ ] Tab enable khi NWC format selected
- [ ] Format Icons row hiển thị đúng (NWC có border vàng)
- [ ] 2 GroupBoxes (Standard / Revit 2020+) layout đều nhau
- [ ] All checkboxes render correctly
- [ ] ComboBoxes mở được và hiển thị options
- [ ] Faceting factor TextBox nhập được số
- [ ] ScrollViewer hoạt động khi window nhỏ

### Functional Tests
- [ ] CheckBoxes toggle được
- [ ] ElementParamsCombo change selection
- [ ] CoordinatesCombo change selection
- [ ] FacetingFactorBox accept numeric input
- [ ] Default values đúng (checked/unchecked/text)
- [ ] Format icon checkboxes sync với main format selection

### Integration Tests
- [ ] Binding `IsNwcSelected` enable/disable tab
- [ ] Export NWC với settings từ UI
- [ ] All options apply đúng vào NavisworksExportManager
- [ ] Validation cho Faceting factor (0.1-15)

---

## 🎨 Styling

### GroupBox
```xaml
BorderBrush="#E1E1E1" 
BorderThickness="1"
```

### CheckBox
```xaml
FontSize="13"
Margin="0,0,0,8"
```

### ComboBox
```xaml
Width="120"
FontSize="13"
```

### TextBox (Faceting Factor)
```xaml
Width="80"
Height="25"
VerticalContentAlignment="Center"
HorizontalContentAlignment="Right"
FontSize="13"
```

### TextBlock Labels
```xaml
FontSize="13"
VerticalAlignment="Center"
Width="180" (for labels before ComboBox/TextBox)
Width="120" (for Faceting factor label)
```

---

## 📦 Build Status

```
Build succeeded.
    8 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.49

✅ DLL: bin\Debug\ProSheetsAddin.dll
✅ XAML compiled to BAML successfully
✅ NWC Settings tab ready for testing
```

---

## 🔄 Comparison: Standard Options vs Revit 2020+

| Feature | Standard Options | Revit 2020+ |
|---------|------------------|-------------|
| Number of options | 11 | 3 |
| CheckBoxes | 9 | 2 |
| ComboBoxes | 2 | 0 |
| TextBoxes | 0 | 1 |
| Default checked | 7 | 0 |
| Column width | Equal (50%) | Equal (50%) |

---

## 📖 Navisworks Export Settings Reference

### Convert element parameters options:
- **Elements:** Export tất cả parameters của elements
- **Instances:** Chỉ export instance parameters
- **Types:** Chỉ export type parameters

### Coordinates options:
- **Shared:** Sử dụng shared coordinates (khuyến nghị cho multi-model)
- **Internal:** Coordinates nội bộ Revit
- **Project Base:** Base point của project
- **Survey:** Survey point

### Faceting factor:
- **Range:** 0.1 - 15
- **Lower value:** Mesh chi tiết hơn, file lớn hơn
- **Higher value:** Mesh thô hơn, file nhỏ hơn
- **Default:** 1 (balanced)

---

## 🚀 Next Steps

### Phase 1: UI Polish ✅
- ✅ Format Icons row with NWC highlighted
- ✅ Standard options GroupBox (11 options)
- ✅ Revit 2020+ GroupBox (3 options)
- ✅ Proper spacing and alignment

### Phase 2: Functionality ⏳
1. Add event handlers for ComboBoxes
2. Add event handlers for CheckBoxes
3. Add validation for Faceting factor (numeric, range 0.1-15)
4. Connect to NavisworksExportManager
5. Apply settings to actual NWC export

### Phase 3: Advanced ⏳
1. Save/Load NWC preset configurations
2. Tooltips for each option explaining purpose
3. Real-time file size estimation based on faceting factor
4. Warning when Faceting factor < 0.5 or > 10

---

## 📚 Related Files

- **XAML:** `Views/ProSheetsMainWindow.xaml` (lines ~827-987)
- **Manager:** `Managers/NavisworksExportManager.cs`
- **Models:** `Models/ProSheetsModels.cs` (ExportSettings)
- **Format Tab:** Lines 639-825
- **DWG Tab:** Lines 827-840

---

## 💡 Usage Example

### User Workflow:
1. Click tab "Formats"
2. Check NWC format icon (🔧 NWC)
3. Tab "NWC Settings" becomes enabled
4. Click "NWC Settings" tab
5. Configure:
   - ✅ Convert construction parts
   - ✅ Convert element Ids
   - Select "Elements" for parameters
   - Select "Shared" coordinates
   - ✅ Divide file into levels
   - Set Faceting factor = 1
6. Go to "Create" tab
7. Click "Export" → NWC files created with configured settings

---

**Version:** 1.0.0  
**Date:** October 2, 2025  
**Status:** ✅ Build Success, Ready for Testing  
**Tab Position:** Format | DWG Settings | **NWC Settings**
