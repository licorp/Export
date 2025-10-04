# NWC Export Implementation Guide

## 📋 Tóm tắt
Guide này hướng dẫn implement đầy đủ tính năng export Navisworks (.nwc) với UI giống ProSheets.

## ✅ Đã hoàn thành

### 1. NWCExportSettings Model
**File**: `Models/NWCExportSettings.cs`

```csharp
public class NWCExportSettings : INotifyPropertyChanged
{
    // Standard options
    public bool ConvertConstructionParts { get; set; } = true;
    public bool ConvertElementIds { get; set; } = true;
    public string ConvertElementParameters { get; set; } = "Elements";
    public bool ConvertElementProperties { get; set; } = true;
    public bool ConvertLinkedFiles { get; set; } = false;
    public bool ConvertRoomAsAttribute { get; set; } = true;
    public bool ConvertURLs { get; set; } = true;
    public string Coordinates { get; set; } = "Shared";
    public bool DivideFileIntoLevels { get; set; } = true;
    public bool ExportRoomGeometry { get; set; } = false;
    public bool TryAndFindMissingMaterials { get; set; } = true;

    // Revit 2020 & above
    public bool ConvertLinkedCADFormat { get; set; } = false;
    public bool ConvertLights { get; set; } = false;
    public double FacetingFactor { get; set} = 1.0;
}
```

## 🔧 Cần implement tiếp

### 2. Update NavisworksExportManager

**File**: `Managers/NavisworksExportManager.cs`

**Thay đổi signature method**:
```csharp
// OLD
public bool ExportToNavisworks(List<ViewItem> selectedViews, string outputFolder, string fileNamePrefix = "")

// NEW  
public bool ExportToNavisworks(List<ViewItem> selectedViews, NWCExportSettings settings, string outputFolder, string fileNamePrefix = "")
```

**Thêm method**:
```csharp
private NavisworksExportOptions CreateNavisworksExportOptions(NWCExportSettings settings)
{
    var options = new NavisworksExportOptions();
    
    // Standard options
    options.ConvertConstructionParts = settings.ConvertConstructionParts;
    options.ConvertElementIds = settings.ConvertElementIds;
    options.ConvertElementProperties = settings.ConvertElementProperties;
    options.ConvertLinkedFiles = settings.ConvertLinkedFiles;
    options.ConvertRoomAsAttribute = settings.ConvertRoomAsAttribute;
    options.ConvertURLs = settings.ConvertURLs;
    options.DivideFileIntoLevels = settings.DivideFileIntoLevels;
    options.ExportRoomGeometry = settings.ExportRoomGeometry;
    options.FindMissingMaterials = settings.TryAndFindMissingMaterials;
    
    // Parameters
    switch (settings.ConvertElementParameters)
    {
        case "None":
            options.Parameters = NavisworksParameters.None;
            break;
        case "Elements":
            options.Parameters = NavisworksParameters.Elements;
            break;
        case "All":
            options.Parameters = NavisworksParameters.All;
            break;
    }
    
    // Coordinates
    switch (settings.Coordinates)
    {
        case "Shared":
            options.Coordinates = NavisworksCoordinates.Shared;
            break;
        case "Project":
            options.Coordinates = NavisworksCoordinates.Project;
            break;
        case "Internal":
            options.Coordinates = NavisworksCoordinates.Internal;
            break;
    }
    
    // Revit 2020+ features
    if (IsRevit2020OrAbove())
    {
        options.ConvertLinkedCADFormats = settings.ConvertLinkedCADFormat;
        options.ConvertLights = settings.ConvertLights;
        options.FacetingFactor = settings.FacetingFactor;
    }
    
    return options;
}

private bool IsRevit2020OrAbove()
{
    var app = _document.Application;
    var version = app.VersionNumber;
    return int.Parse(version) >= 2020;
}
```

### 3. Thêm NWC Settings Tab vào UI

**File**: `Views/ProSheetsMainWindow.xaml`

**Tìm TabControl và thêm TabItem mới**:

```xaml
<!-- Add after existing tabs (PDF, DWG, IFC, etc.) -->
<TabItem Header="NWC">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <Grid Margin="15">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- Standard options -->
            <GroupBox Header="Standard options" Grid.Column="0" Margin="0,0,10,0" Padding="10">
                <StackPanel>
                    <CheckBox Content="Convert construction parts" 
                             IsChecked="{Binding NWCSettings.ConvertConstructionParts}" 
                             Margin="0,5"/>
                    
                    <CheckBox Content="Convert element Ids" 
                             IsChecked="{Binding NWCSettings.ConvertElementIds}" 
                             Margin="0,5"/>
                    
                    <StackPanel Orientation="Horizontal" Margin="0,10">
                        <TextBlock Text="Convert element parameters" VerticalAlignment="Center" Width="180"/>
                        <ComboBox SelectedItem="{Binding NWCSettings.ConvertElementParameters}" Width="120">
                            <ComboBoxItem Content="None"/>
                            <ComboBoxItem Content="Elements" IsSelected="True"/>
                            <ComboBoxItem Content="All"/>
                        </ComboBox>
                    </StackPanel>
                    
                    <CheckBox Content="Convert element properties" 
                             IsChecked="{Binding NWCSettings.ConvertElementProperties}" 
                             Margin="0,5"/>
                    
                    <CheckBox Content="Convert linked files" 
                             IsChecked="{Binding NWCSettings.ConvertLinkedFiles}" 
                             Margin="0,5"/>
                    
                    <CheckBox Content="Convert room as attribute" 
                             IsChecked="{Binding NWCSettings.ConvertRoomAsAttribute}" 
                             Margin="0,5"/>
                    
                    <CheckBox Content="Convert URLs" 
                             IsChecked="{Binding NWCSettings.ConvertURLs}" 
                             Margin="0,5"/>
                    
                    <StackPanel Orientation="Horizontal" Margin="0,10">
                        <TextBlock Text="Coordinates" VerticalAlignment="Center" Width="100"/>
                        <ComboBox SelectedItem="{Binding NWCSettings.Coordinates}" Width="120">
                            <ComboBoxItem Content="Shared" IsSelected="True"/>
                            <ComboBoxItem Content="Project"/>
                            <ComboBoxItem Content="Internal"/>
                        </ComboBox>
                    </StackPanel>
                    
                    <CheckBox Content="Divide file into levels" 
                             IsChecked="{Binding NWCSettings.DivideFileIntoLevels}" 
                             Margin="0,5"/>
                    
                    <CheckBox Content="Export room geometry" 
                             IsChecked="{Binding NWCSettings.ExportRoomGeometry}" 
                             Margin="0,5"/>
                    
                    <CheckBox Content="Try and find missing materials" 
                             IsChecked="{Binding NWCSettings.TryAndFindMissingMaterials}" 
                             Margin="0,5"/>
                </StackPanel>
            </GroupBox>
            
            <!-- Revit 2020 & above -->
            <GroupBox Header="Revit 2020 &amp; above" Grid.Column="1" Margin="10,0,0,0" Padding="10">
                <StackPanel>
                    <CheckBox Content="Convert linked CAD format" 
                             IsChecked="{Binding NWCSettings.ConvertLinkedCADFormat}" 
                             Margin="0,5"/>
                    
                    <CheckBox Content="Convert lights" 
                             IsChecked="{Binding NWCSettings.ConvertLights}" 
                             Margin="0,5"/>
                    
                    <StackPanel Orientation="Horizontal" Margin="0,15">
                        <TextBlock Text="Faceting factor" VerticalAlignment="Center" Width="120"/>
                        <TextBox Text="{Binding NWCSettings.FacetingFactor}" Width="80" 
                                Height="24" VerticalContentAlignment="Center"/>
                        <TextBlock Text="(0.1 - 10.0)" VerticalAlignment="Center" Margin="10,0,0,0" 
                                  Foreground="Gray" FontSize="11"/>
                    </StackPanel>
                    
                    <TextBlock Text="Lower values create smaller files but less detail.&#x0a;Higher values create larger files with more detail." 
                              TextWrapping="Wrap" Foreground="Gray" FontSize="11" Margin="0,10,0,0"/>
                </StackPanel>
            </GroupBox>
        </Grid>
    </ScrollViewer>
</TabItem>
```

### 4. Thêm Property vào ProSheetsMainWindow code-behind

**File**: `Views/ProSheetsMainWindow.xaml.cs`

```csharp
// Add property
private NWCExportSettings _nwcSettings = new NWCExportSettings();

public NWCExportSettings NWCSettings
{
    get => _nwcSettings;
    set
    {
        _nwcSettings = value;
        OnPropertyChanged(nameof(NWCSettings));
    }
}
```

### 5. Update Export Handlers

**File**: Nơi gọi NavisworksExportManager

```csharp
// OLD
var nwcManager = new NavisworksExportManager(_document);
nwcManager.ExportToNavisworks(selectedViews, outputFolder, filePrefix);

// NEW
var nwcManager = new NavisworksExportManager(_document);
nwcManager.ExportToNavisworks(selectedViews, NWCSettings, outputFolder, filePrefix);
```

### 6. Profile Integration (Optional - Save/Load settings)

**File**: `Models/ProSheetsProfile.cs`

```csharp
// Add property
public NWCExportSettings NWCSettings { get; set; } = new NWCExportSettings();
```

**Save/Load logic** tương tự như PDF/DWG settings hiện có.

## 📸 Kết quả mong đợi

Sau khi implement xong:
1. ✅ Tab "NWC" xuất hiện trong Format Settings
2. ✅ 2 GroupBox: "Standard options" và "Revit 2020 & above"
3. ✅ Checkboxes cho từng option
4. ✅ ComboBoxes cho Parameters và Coordinates
5. ✅ TextBox cho Faceting Factor
6. ✅ Settings được save/load cùng Profile
7. ✅ Export NWC với settings đã cấu hình

## 🎯 Testing

1. **Test Standard Options**:
   - Enable/disable các checkboxes
   - Thay đổi Parameters (None/Elements/All)
   - Thay đổi Coordinates (Shared/Project/Internal)
   - Export và verify trong Navisworks

2. **Test Revit 2020+ Options**:
   - Enable Convert lights
   - Thay đổi Faceting Factor (0.5, 1.0, 2.0)
   - So sánh file size và detail level

3. **Test Profile Save/Load**:
   - Cấu hình settings
   - Save profile
   - Đóng và mở lại
   - Load profile → Verify settings còn nguyên

## ⚠️ Known Issues

1. **NavisworksExportManager conflicts**: 
   - Old version có signature khác
   - Cần replace toàn bộ file hoặc update carefully

2. **ComboBox binding**:
   - Phải dùng `SelectedItem` chứ không phải `SelectedValue`
   - ComboBoxItem.Content phải match với string trong model

3. **Revit version check**:
   - Một số properties chỉ available từ Revit 2020
   - Try-catch để tránh crash trên Revit cũ

## 📝 Notes

- File NWC có extension `.nwc` (Navisworks Cache)
- NWC files dùng cho coordination và clash detection
- Chỉ export 3D views, không export 2D sheets
- Default settings đã được set theo best practices

---

**Status**: ⏳ Partial implementation complete
**Next Steps**: Update NavisworksExportManager và add UI tab
