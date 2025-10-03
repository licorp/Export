# View/Sheet Set Management Feature

## Overview
Tính năng View/Sheet Set Management cho phép người dùng:
- Xem và chọn các View/Sheet Sets có sẵn trong project
- Filter sheets/views theo các sets đã định nghĩa
- Lưu selection hiện tại thành một View/Sheet Set mới
- Tự động apply selection từ các sets đã lưu

## Components

### 1. Models

**ViewSheetSetInfo.cs**
- Model chứa thông tin về View/Sheet Set
- Properties:
  - `Name`: Tên của set
  - `ViewIds`: Danh sách View IDs trong set
  - `SheetIds`: Danh sách Sheet IDs trong set
  - `IsBuiltIn`: Set có phải built-in không (All Sheets, All Views)
  - `TotalCount`: Tổng số items
  - `DisplayName`: Tên hiển thị (bao gồm count)

### 2. Managers

**ViewSheetSetManager.cs**
- Service quản lý các ViewSheetSet operations
- Methods:
  - `GetAllViewSheetSets()`: Lấy tất cả sets từ project
  - `CreateViewSheetSet(name, ids)`: Tạo set mới
  - `GetSheetsFromSet(name)`: Lấy sheets từ set
  - `GetViewsFromSet(name)`: Lấy views từ set
  - `DeleteViewSheetSet(name)`: Xóa set
  - `SetNameExists(name)`: Kiểm tra tên có tồn tại không

### 3. Views

**SaveViewSheetSetDialog.xaml/cs**
- Dialog để nhập tên cho View/Sheet Set mới
- Validation:
  - Tên không được rỗng
  - Không chứa ký tự không hợp lệ
  - Không trùng với tên reserved (All Sheets, All Views)
- Hiển thị số lượng items sẽ được lưu

**ProSheetsMainWindow.ViewSheetSets.cs**
- Partial class chứa các methods cho View/Sheet Set management
- Methods:
  - `LoadViewSheetSets()`: Load sets vào ComboBox
  - `ApplyViewSheetSetFilter()`: Apply filter theo set đã chọn
  - `CreateSheetItem()`: Helper tạo SheetItem từ ViewSheet
  - `CreateViewItem()`: Helper tạo ViewItem từ View

## User Workflow

### Xem danh sách View/Sheet Sets
1. Mở ProSheets dialog
2. Nhìn vào dropdown "View/Sheet Set"
3. Click dropdown để xem tất cả sets có sẵn
4. Format hiển thị: `[Set Name] ([Count])`
   - Example: "All Sheets (45)", "Architectural (12)", "Custom Set (8)"

### Filter theo View/Sheet Set
1. Check vào checkbox bên trái dropdown
2. Chọn set từ dropdown
3. Danh sách sheets/views sẽ tự động filter theo set đã chọn
4. Uncheck checkbox để hiện lại tất cả

### Lưu View/Sheet Set mới
1. Select các sheets/views muốn lưu (check vào checkbox)
2. Click button "Save V/S Set"
3. Dialog hiện ra với thông tin:
   - "This set will contain [X] sheets/views"
4. Nhập tên cho set
5. Click "Save"
6. Set mới được tạo và tự động chọn trong dropdown

### Replace existing set
1. Click "Save V/S Set"
2. Nhập tên trùng với set đã có
3. Dialog hỏi: "Do you want to replace it?"
4. Click "Yes" để replace, "No" để cancel

## Technical Details

### Built-in Sets
- **All Sheets**: Chứa tất cả sheets không phải template
- **All Views**: Chứa tất cả views có thể print

### ViewSheetSet Storage
- Lưu trong Revit project database
- Sử dụng Revit API `ViewSheetSet` class
- Persistent across project saves
- Có thể share giữa các users

### Filter Logic
```csharp
// Sheets mode
var filteredSheets = _viewSheetSetManager.GetSheetsFromSet(setInfo.Name);
var filteredIds = new HashSet<ElementId>(filteredSheets.Select(s => s.Id));
Sheets.Clear();
foreach (var sheet in allSheets.Where(s => filteredIds.Contains(s.Id)))
{
    Sheets.Add(CreateSheetItem(sheet));
}

// Views mode
var filteredViews = _viewSheetSetManager.GetViewsFromSet(setInfo.Name);
var filteredIds = new HashSet<ElementId>(filteredViews.Select(v => v.Id));
Views.Clear();
foreach (var view in allViews.Where(v => filteredIds.Contains(v.Id)))
{
    Views.Add(CreateViewItem(view));
}
```

### Create ViewSheetSet
```csharp
using (var trans = new Transaction(_doc, "Create ViewSheetSet"))
{
    trans.Start();
    
    var printManager = _doc.PrintManager;
    printManager.PrintRange = PrintRange.Select;
    
    var viewSet = new ViewSet();
    foreach (var id in selectedIds)
    {
        var elem = _doc.GetElement(id);
        if (elem is View view && view.CanBePrinted)
        {
            viewSet.Insert(view);
        }
    }
    
    var viewSheetSetting = printManager.ViewSheetSetting;
    viewSheetSetting.CurrentViewSheetSet.Views = viewSet;
    viewSheetSetting.SaveAs(name);
    
    trans.Commit();
}
```

## UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│  View/Sheet Set │ ☐ [All Sheets (45) ▼] │ [Save V/S Set]  │
└─────────────────────────────────────────────────────────────┘
```

### Components:
1. **Label**: "View/Sheet Set"
2. **Checkbox**: Enable/disable filtering
3. **ComboBox**: Select set to apply
4. **Button**: Save current selection as new set

## Debug Logging

Các debug messages:
```
Loading View/Sheet Sets...
Found 5 View/Sheet Sets
  - All Sheets (45 items)
  - All Views (38 items)
  - Architectural (12 items)
View/Sheet Sets loaded successfully

View/Sheet Set changed to: Architectural
Applying View/Sheet Set filter: Architectural
Filtered to 12 sheets from set
Displaying 12 sheets from set 'Architectural'

Save View/Sheet Set clicked
Selected 8 items for saving to View/Sheet Set
User entered set name: Custom Set
Successfully created ViewSheetSet: Custom Set
```

## Error Handling

### Common Errors:
1. **No selection**: "No sheets/views selected"
2. **Invalid name**: "Set name contains invalid character"
3. **Reserved name**: "This name is reserved"
4. **Duplicate name**: "A View/Sheet Set named '[name]' already exists"
5. **No printable views**: "No printable views selected"
6. **Transaction failed**: "Failed to create ViewSheetSet: [error message]"

## Best Practices

1. **Naming Convention**:
   - Use descriptive names: "Architectural Floor Plans", "Structural Sections"
   - Avoid special characters
   - Keep names concise (< 50 characters)

2. **Organization**:
   - Create sets by discipline (Arch, Struct, MEP)
   - Create sets by phase (SD, DD, CD)
   - Create sets by submission type (Planning, Permit, Construction)

3. **Performance**:
   - Built-in sets (All Sheets, All Views) are fast
   - Custom sets with many items may take longer to filter
   - Use specific sets for large projects

## Integration với Export

View/Sheet Set selection có thể được sử dụng trước khi export:
1. Chọn set từ dropdown
2. Check vào filter checkbox
3. Danh sách filter theo set
4. Select/deselect individual items nếu cần
5. Export as usual

## Future Enhancements

Potential features:
- [ ] Rename existing sets
- [ ] Duplicate sets
- [ ] Merge multiple sets
- [ ] Export set definitions to JSON
- [ ] Import set definitions from JSON
- [ ] Batch create sets from naming patterns
- [ ] Auto-create sets based on sheet parameters

## Troubleshooting

### Set không hiển thị trong dropdown
- Check: Set có được tạo trong project này không?
- Check: Project file đã save chưa?
- Solution: Restart Revit, reload project

### Filter không hoạt động
- Check: Checkbox có được check không?
- Check: Set có chứa items không?
- Solution: Uncheck và check lại checkbox

### Cannot create set
- Check: Có items nào được select không?
- Check: Tên có hợp lệ không?
- Check: Transaction có bị lock không?
- Solution: Close transactions khác, try again

## API References

Revit API classes used:
- `Autodesk.Revit.DB.ViewSheetSet`
- `Autodesk.Revit.DB.ViewSet`
- `Autodesk.Revit.DB.PrintManager`
- `Autodesk.Revit.DB.ViewSheetSetting`
- `Autodesk.Revit.DB.FilteredElementCollector`
- `Autodesk.Revit.DB.Transaction`
