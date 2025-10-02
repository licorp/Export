# Views Selection Implementation

## Tổng quan
Đã implement Views selection interface giống ProSheets với đầy đủ tính năng như yêu cầu.

## Các thay đổi đã thực hiện

### 1. Model - ViewItem.cs
**Đã cập nhật ViewItem với các properties:**
- `ViewName`: Tên view
- `ViewType`: Loại view (có thể chỉnh sửa qua dropdown)
- `Scale`: Tỷ lệ view (1:100, Custom, etc.)
- `DetailLevel`: Mức độ chi tiết (Fine, Medium, Coarse)
- `Discipline`: Discipline của view (Architectural, Structural, MEP)
- `CustomFileName`: Tên file tùy chỉnh (editable)
- `IsSelected`: Checkbox để chọn view
- `AvailableViewTypes`: List các loại view để hiển thị trong dropdown

**Constructor từ Revit View:**
```csharp
public ViewItem(View revitView)
{
    RevitViewId = revitView.Id;
    ViewName = revitView.Name;
    ViewType = GetViewTypeString(revitView.ViewType);
    Scale = GetViewScale(revitView);
    DetailLevel = revitView.DetailLevel.ToString();
    Discipline = GetViewDiscipline(revitView);
    CustomFileName = ViewName;
}
```

### 2. XAML - ProSheetsMainWindow.xaml

#### Ribbon Controls (đã có sẵn)
```xaml
<!-- Radio Buttons cho Sheets/Views -->
<RadioButton x:Name="SheetsRadio" Content="Sheets" IsChecked="True"/>
<RadioButton x:Name="ViewsRadio" Content="Views"/>

<!-- View/Sheet Set Dropdown -->
<ComboBox x:Name="ViewSheetSetCombo">
    <ComboBoxItem Content="<None>"/>
    <ComboBoxItem Content="All Sheets"/>
    <ComboBoxItem Content="Architectural"/>
    ...
</ComboBox>

<!-- Filter và Reset Buttons -->
<Button Content="Filter" Click="FilterByVSSet_Click"/>
<Button Content="Reset" Click="ResetFilter_Click"/>

<!-- Custom Filename Button -->
<Button Content="Custom Filename" Click="SetAllCustomFileName_Click"/>

<!-- Search TextBox -->
<TextBox x:Name="SearchTextBox"/>
```

#### Views DataGrid (đã cập nhật)
**6 cột chính:**

1. **Checkbox Column (40px)**: Chọn view
2. **View Name (200px)**: Tên view
3. **All View (100px)**: Dropdown để thay đổi loại view
   ```xaml
   <DataGridTemplateColumn Header="All View">
       <ComboBox ItemsSource="{Binding AvailableViewTypes}"
                 SelectedItem="{Binding ViewType}"/>
   </DataGridTemplateColumn>
   ```
4. **View Scale (80px)**: Tỷ lệ (1:100, Custom, etc.)
5. **Detail Level (80px)**: Fine/Medium/Coarse
6. **Discipline (100px)**: Architectural/Structural/MEP
7. **Custom File Name (*)**: Editable TextBox cho custom filename

### 3. Code-Behind - ProSheetsMainWindow.xaml.cs

#### Event Handlers đã implement:

**1. SheetsRadio_Checked / ViewsRadio_Checked**
```csharp
private void SheetsRadio_Checked(object sender, RoutedEventArgs e)
{
    SheetsDataGrid.Visibility = Visibility.Visible;
    ViewsDataGrid.Visibility = Visibility.Collapsed;
    LoadSheets();
}

private void ViewsRadio_Checked(object sender, RoutedEventArgs e)
{
    SheetsDataGrid.Visibility = Visibility.Collapsed;
    ViewsDataGrid.Visibility = Visibility.Visible;
    LoadViews();
}
```

**2. ViewsDataGrid_SelectionChanged**
```csharp
private void ViewsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    UpdateStatusText(); // Cập nhật "X views selected"
}
```

**3. ViewSheetSetCombo_SelectionChanged**
- Filter views theo View/Sheet Set được chọn

**4. FilterByVSSet_Click**
- Apply filter từ ViewSheetSetCombo

**5. ResetFilter_Click**
- Reset về show tất cả views

**6. SetAllCustomFileName_Click**
- Set custom filename cho tất cả views đã chọn

#### LoadViews Method (đã có sẵn)
```csharp
private void LoadViews()
{
    Views = new ObservableCollection<ViewItem>();
    
    var collector = new FilteredElementCollector(_document)
        .OfClass(typeof(View))
        .Cast<View>();
    
    foreach (var view in collector)
    {
        if (view.IsTemplate || !view.CanBePrinted) continue;
        
        var viewItem = new ViewItem(view);
        Views.Add(viewItem);
    }
    
    UpdateStatusText();
}
```

## Tính năng đã implement

### ✅ Hoàn thành
1. **Radio buttons** Sheets/Views - chuyển đổi giữa 2 danh sách
2. **View/Sheet Set dropdown** - filter theo nhóm
3. **Filter/Reset buttons** - apply và clear filters
4. **Custom Filename button** - set filename cho nhiều views cùng lúc
5. **Search box** - tìm kiếm views
6. **Views DataGrid** với 6 cột:
   - Checkbox selection
   - View Name
   - View Type dropdown (editable)
   - View Scale
   - Detail Level
   - Discipline
   - Custom File Name (editable)
7. **Status labels** - hiển thị số lượng views loaded và selected
8. **ViewItem model** - đầy đủ properties và data binding

### 🎯 Tích hợp sẵn có
- Navigation với Back/Next buttons
- Profile management (Import/Export JSON)
- Tab switching (Sheets → Format → Create)
- Export functionality cho cả Sheets và Views

## Cách sử dụng

### 1. Chuyển đổi giữa Sheets và Views
- Click radio button **"Sheets"** để xem danh sách sheets
- Click radio button **"Views"** để xem danh sách views

### 2. Filter Views
1. Chọn View/Sheet Set từ dropdown
2. Click nút **"Filter"** để apply
3. Click nút **"Reset"** để show tất cả

### 3. Chỉnh sửa View Type
- Click vào ô View Type trong DataGrid
- Chọn loại mới từ dropdown (3D, Rendering, Section, etc.)

### 4. Custom Filename
**Cách 1**: Edit từng view
- Double-click vào ô Custom File Name
- Nhập tên mới

**Cách 2**: Set cho nhiều views cùng lúc
- Chọn các views cần đổi tên (checkbox)
- Click nút **"Custom Filename"**
- Nhập pattern filename

### 5. Search Views
- Nhập text vào Search box
- DataGrid tự động filter theo ViewName

## Kết quả Build
```
Build succeeded.
    8 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.53
```

✅ DLL đã compile thành công tại: `bin\Debug\ProSheetsAddin.dll`

## Testing trong Revit
1. Load Revit 2023
2. Addin tự động load từ `ProSheetsAddin.addin`
3. Mở ProSheets command
4. Tab **"Sheets"**:
   - Chọn radio **"Views"**
   - Xem danh sách views với đầy đủ thông tin
   - Test chỉnh sửa View Type qua dropdown
   - Test edit Custom File Name
   - Test filter và search

## Notes
- Views DataGrid sử dụng cùng styling với Sheets DataGrid
- Tất cả columns đều resizable
- View Type dropdown trong DataGrid cho phép thay đổi loại view
- Custom File Name editable cho mỗi view
- Filter và Search hoạt động cho cả Sheets và Views

## Mở rộng trong tương lai
- Save/Load View/Sheet Sets
- Batch rename với patterns
- Group views by discipline
- Export views theo template
- Preview view thumbnail
