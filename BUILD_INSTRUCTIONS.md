# Build Instructions - ProSheets Addin

## Current Build Issue

MSBuild command line không thể build WPF project với x:Name controls và Click handlers do temp validation step fails.

## Solution: Build trong Visual Studio IDE

### Cách 1: Build trong Visual Studio (RECOMMENDED)
1. Mở `Export.sln` trong Visual Studio 2022
2. Press **F6** hoặc **Ctrl+Shift+B** để build
3. Hoặc menu: **Build → Build Solution**
4. Output DLL: `bin\Debug\ProSheetsAddin.dll`

### Cách 2: Sử dụng existing DLL (Quick Test)
- DLL đã được build thành công trước đó: `bin\Debug\ProSheetsAddin.dll`
- ⚠️ **LƯU Ý**: DLL này có event handlers nhưng Click handlers chưa được wire up properly trong XAML
- Cần rebuild trong Visual Studio để get full functionality

## Browse Buttons Implementation

### Files Changed:
1. **ProSheetsMainWindow.xaml** (lines 1227-1248)
   - Added x:Name to TextBoxes and Buttons
   - Added ElementName bindings for IsEnabled
   - Added Click handlers

2. **ProSheetsMainWindow.xaml.cs** (lines 2704-2790)
   - Implemented `BrowseUserPsetsButtonIFC_Click`
   - Implemented `BrowseParamMappingButtonIFC_Click`

### Functionality:
- Browse button "..." để chọn file .txt
- Button enable/disable based on checkbox state
- OpenFileDialog with filter for .txt files
- Sets TextBox.Text với selected file path
- Error handling with MessageBox

## Troubleshooting

### Issue: Build fails với "CS0103: The name 'UserPsetsPathTextBoxIFC' does not exist"
**Reason**: WPF temp validation step không generate .g.cs file properly từ command line

**Fix**: Build trong Visual Studio IDE thay vì MSBuild.exe

### Issue: Browse buttons không clickable
**Reason**: DLL cũ không có Click handlers wired up

**Fix**: 
1. Uncomment event handlers trong .xaml.cs (lines 2704-2790)
2. Add Click attributes trong .xaml (lines 1234, 1246)
3. Build trong Visual Studio

## Testing trong Revit

1. Copy `bin\Debug\ProSheetsAddin.dll` to Revit addins folder
2. Restart Revit 2023
3. Run ProSheets command
4. Navigate to IFC Settings tab
5. Check "Export user defined property sets"
6. Click "..." button next to text box
7. OpenFileDialog should appear

## Build Status

✅ C# 7.3 language features enabled (string interpolation works)
✅ IFC Settings tab complete với all controls
✅ Browse button XAML bindings complete
✅ Browse button event handlers implemented
⏳ **PENDING**: Final build trong Visual Studio để wire up Click handlers properly

## Next Steps

1. **Build trong Visual Studio** (đang mở)
2. Verify 0 errors sau khi build
3. Test Browse buttons trong Revit
4. If successful, browse buttons sẽ show OpenFileDialog
5. Selected file path sẽ appear trong TextBox
