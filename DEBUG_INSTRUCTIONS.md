# Hướng dẫn cài đặt và test ProSheets Addin

## 🔧 Cài đặt

### Bước 1: Copy files
```bash
# Copy addin manifest
copy "ProSheetsAddin.addin" "%APPDATA%\Autodesk\Revit\Addins\2023\"

# Copy compiled DLL  
copy "bin\Debug\ProSheetsAddin.dll" "%APPDATA%\Autodesk\Revit\Addins\2023\"
```

### Bước 2: Verify paths
Kiểm tra files đã copy đúng chưa:
- `C:\Users\[Username]\AppData\Roaming\Autodesk\Revit\Addins\2023\ProSheetsAddin.addin`
- `C:\Users\[Username]\AppData\Roaming\Autodesk\Revit\Addins\2023\ProSheetsAddin.dll`

## 🐛 Debug Setup

### Bước 1: Download DebugView
1. Tải DebugView từ Microsoft Sysinternals
2. Chạy DebugView as Administrator
3. Bật menu **Capture > Capture Win32**
4. Filter theo "[ProSheets]" để chỉ thấy logs của addin

### Bước 2: Test Commands
1. Mở Revit 2023
2. Tìm tab **ProSheets Export** trên ribbon
3. Sẽ có 2 buttons:
   - **Test ProSheets**: Command để debug
   - **ProSheets Exporter**: Full functionality

### Bước 3: Debug Process
1. Click **Test ProSheets** trước
2. Xem output trong DebugView:
   ```
   [ProSheets] TestCommand started  
   [ProSheets] Document: [Document Name]
   [ProSheets] Found [X] sheets
   [ProSheets] Attempting to create main window
   [ProSheets] Main window created successfully
   ```

## 🔍 Debug Logs Explained

### Window Creation Logs:
```
[ProSheets] Constructor started
[ProSheets] Adding DataGrid columns  
[ProSheets] DataGrid columns added successfully
[ProSheets] LoadSheets started
[ProSheets] Found [X] sheets in document
[ProSheets] Added sheet: [Number] - [Name]
[ProSheets] Setting DataGrid ItemsSource with [X] items
[ProSheets] LoadSheets completed successfully
[ProSheets] Constructor completed
```

### Selection Logs:
```
[ProSheets] Sheet '[Number]' IsSelected changed: false -> true
[ProSheets] PropertyChanged fired for '[Number]': IsSelected  
[ProSheets] Sheet selection changed via PropertyChanged: [Number]
[ProSheets] UpdateStatusText called - Selected count: 1
[ProSheets] Status text updated: 1 sheets selected
```

### DataGrid Events:
```
[ProSheets] DataGrid CellEditEnding - Column: Select
[ProSheets] DataGrid CurrentCellChanged
[ProSheets] DataGrid SelectionChanged - Selected items: 1
```

## 🧪 Test Scenarios

### Test 1: Basic Window Launch
1. Click **Test ProSheets**
2. Xem có dialog "Document: [Name]" xuất hiện không
3. Click OK để mở main window
4. Kiểm tra có sheets trong Selection tab không

### Test 2: Manual Selection
1. Mở main window
2. Click vào checkbox bên cạnh sheet names
3. Xem status bar có update "X sheets selected" không
4. Check DebugView để thấy selection events

### Test 3: Programmatic Selection  
1. Click button **Select All**
2. Xem tất cả checkboxes có được check không
3. Status bar có hiển thị đúng count không
4. Click **Clear All** để uncheck all

### Test 4: Tab Navigation
1. Click giữa các tabs: Selection > Format > Create
2. Xem content có thay đổi đúng không
3. Format tab có buttons PDF/DWG/IFC không
4. Create tab có folder picker không

## ❌ Troubleshooting

### Addin không xuất hiện trong Revit:
```
- Kiểm tra paths của .addin và .dll files
- Restart Revit
- Check Revit version (phải là 2023)
- Xem .NET Framework 4.8 đã cài chưa
```

### Window không mở được:
```  
- Check DebugView logs để thấy error messages
- Verify document có sheets không
- Kiểm tra WPF dependencies
```

### Selection không hoạt động:
```
- Xem DebugView có selection events không
- Test với buttons Select All/Clear All
- Check PropertyChanged events firing
- Verify DataGrid binding
```

### No debug output:
```
- Chạy DebugView as Administrator  
- Bật Capture Win32 trong menu
- Verify filter "[ProSheets]" settings
- Restart Revit và test lại
```

## 📝 Expected Debug Output

Khi mọi thứ hoạt động normal, bạn sẽ thấy:

```
[ProSheets] TestCommand started
[ProSheets] Document: Sample Project
[ProSheets] Found 5 sheets
[ProSheets] Attempting to create main window
[ProSheets] Constructor started
[ProSheets] Adding DataGrid columns
[ProSheets] DataGrid columns added successfully
[ProSheets] LoadSheets started
[ProSheets] Found 5 sheets in document  
[ProSheets] Added sheet: A101 - Floor Plan
[ProSheets] Added sheet: A201 - Elevations
[ProSheets] Added sheet: A301 - Sections
[ProSheets] Added sheet: A401 - Details
[ProSheets] Added sheet: A501 - Schedules
[ProSheets] Setting DataGrid ItemsSource with 5 items
[ProSheets] UpdateStatusText called - Selected count: 0
[ProSheets] Status text updated: 0 sheets selected
[ProSheets] LoadSheets completed successfully
[ProSheets] Constructor completed
[ProSheets] Main window created successfully
```

Thử test và báo kết quả debug logs nhé! 🚀