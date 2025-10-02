# ProSheets Exporter - Revit Addin

**✅ BUILD THÀNH CÔNG - Giao diện hiện đại với WPF**

Xuất hàng loạt các Views/Sheets từ Revit sang PDF, DWG, IFC và nhiều định dạng khác với giao diện chuyên nghiệp tương tự DiRoots ProSheets.

## 🔧 Hỗ trợ Revit Versions

✅ **Revit 2023** - Tested & Verified  
✅ **Revit 2024** - Compatible  
✅ **Revit 2025** - Compatible  
✅ **Revit 2026** - Compatible  

**Note**: Sử dụng modern Revit API (UnitTypeId, ForgeTypeId) đảm bảo tương thích lâu dài.  
Xem chi tiết tại [COMPATIBILITY.md](COMPATIBILITY.md)

## 🎉 Trạng thái phát triển

✅ **Compilation**: THÀNH CÔNG - Build without errors  
✅ **Interface**: Modern WPF với tab navigation  
✅ **Sheet Management**: DataGrid với checkbox selection, paper size detection  
✅ **Format Selection**: Button interface cho PDF, DWG, IFC, JPG  
✅ **Export Configuration**: Folder browser và settings panel  
✅ **Vietnamese Support**: UI hoàn toàn bằng tiếng Việt  
✅ **Professional Design**: Thiết kế tương tự DiRoots ProSheets  
✅ **Multi-Version Support**: Tương thích Revit 2023-2026  

## Tính năng chính

### 📄 Xuất đa định dạng
- **PDF**: Xuất PDF chất lượng cao với tự động phát hiện paper size
- **DWG/DGN**: Xuất CAD files với cài đặt nâng cao
- **DWF**: Xuất Design Web Format
- **NWC**: Xuất cho Navisworks
- **IFC**: Xuất Building Information Model
- **Hình ảnh**: PNG, JPEG, TIFF với độ phân giải tùy chỉnh
- **XML**: Xuất thông tin sheets và parameters

### 🎯 Tự động phát hiện kích thước giấy
- Phát hiện paper size từ Title Block
- Hỗ trợ các chuẩn: A-series, B-series, ARCH, ANSI
- Tự động xác định hướng Portrait/Landscape
- Custom paper size cho các format đặc biệt

### 📝 Quy tắc đặt tên tùy chỉnh
- Template linh hoạt với parameters
- Biến có sẵn: `{ProjectNumber}`, `{SheetNumber}`, `{SheetName}`, `{Revision}`, `{Date}`, `{Time}`, `{User}`
- Custom parameters từ sheets
- Tự động làm sạch tên file không hợp lệ

### 💾 Quản lý Profiles
- Lưu và tái sử dụng cài đặt
- Multiple profiles cho các dự án khác nhau
- Export/Import profiles
- Profile mặc định

### 🖨️ Tích hợp PDF24
- Sử dụng PDF24 làm virtual printer
- Chất lượng PDF cao hơn
- Tùy chọn fallback về Revit native PDF

### ⏰ Lịch tự động xuất (Scheduled Export)
- Lập lịch xuất tự động theo thời gian
- Hỗ trợ: Once, Daily, Weekly, Monthly
- Email notification khi hoàn thành
- Tracking và logging

### 📋 Drawing Transmittal
- Tạo Drawing Transmittal tự động
- Issue Register với revision history
- Excel format với templates tùy chỉnh
- Tracking thông tin revision

### 🔍 Active Sheets Selection
- Phát hiện sheets đang mở
- Lọc sheets đã modified
- Lọc theo parameters và revision
- Quick selection tools

## Cài đặt

### Yêu cầu hệ thống
- Autodesk Revit 2020 trở lên
- .NET Framework 4.8
- Windows 10/11

### Các bước cài đặt

1. **Download và build project:**
   ```bash
   git clone [repository-url]
   cd ProSheetsAddin
   ```

2. **Build project trong Visual Studio:**
   - Mở `ProSheetsAddin.sln`
   - Build Solution (Ctrl+Shift+B)

3. **Copy files vào Revit Addins folder:**
   ```
   C:\ProgramData\Autodesk\Revit\Addins\2023\
   ```
   - Copy `ProSheetsAddin.dll`
   - Copy `ProSheetsAddin.addin`

4. **Khởi động Revit:**
   - Tab "ProSheets Export" sẽ xuất hiện trong Ribbon

## Sử dụng

### Xuất cơ bản

1. **Mở ProSheets Exporter:**
   - Click "ProSheets Exporter" trong ribbon tab
   
2. **Chọn thư mục xuất:**
   - Click "Browse" để chọn folder
   
3. **Chọn Sheets:**
   - Tab "Chọn Sheets": Tick chọn sheets cần xuất
   - Sử dụng filters để lọc nhanh
   
4. **Chọn định dạng:**
   - Tab "Định dạng": Chọn các format cần xuất
   - Cài đặt quality cho PDF/Images
   
5. **Cấu hình đặt tên:**
   - Tab "Đặt tên": Customize file naming template
   - Preview tên file ở cuối tab
   
6. **Export:**
   - Click "Xuất / Export"

### Tính năng nâng cao

#### 📋 Drawing Transmittal
```csharp
// Tạo transmittal cho selected sheets
var transmittalDialog = new TransmittalDialog(selectedSheets, document);
transmittalDialog.ShowDialog();
```

#### ⏰ Scheduled Export
```csharp
// Setup lịch xuất hàng ngày lúc 6:00 AM
var schedule = new ScheduleSettings
{
    StartDate = DateTime.Today,
    StartTime = new TimeSpan(6, 0, 0),
    RepeatType = RepeatType.Daily,
    ProfileName = "Daily Export",
    SendNotification = true,
    NotificationEmail = "user@company.com"
};
```

#### 🎯 Paper Size Detection
```csharp
var paperSizeManager = new PaperSizeManager();
var paperSize = paperSizeManager.DetectPaperSize(sheet);
// Tự động detect: A3, A4, ARCH D, etc.
```

### Custom Naming Templates

#### Các biến cơ bản:
- `{ProjectNumber}` - Project number từ Project Information
- `{ProjectName}` - Tên project
- `{SheetNumber}` - Số sheet
- `{SheetName}` - Tên sheet  
- `{Revision}` - Current revision
- `{Date}` - Ngày xuất (yyyy-MM-dd)
- `{Time}` - Giờ xuất (HH-mm-ss)
- `{User}` - Username
- `{Computer}` - Computer name

#### Custom Parameters:
- `{DrawnBy}` - Từ sheet parameter "Drawn By"
- `{CheckedBy}` - Từ sheet parameter "Checked By"
- `{Scale}` - Scale của sheet
- Bất kỳ parameter nào có trong sheet

#### Ví dụ templates:
```
{ProjectNumber}_{SheetNumber}_{SheetName}_{Revision}
→ PROJ001_A101_Ground Floor Plan_A.pdf

{ProjectNumber}-{Date}-{SheetNumber}
→ PROJ001-2024-03-15-A101.pdf

{DrawnBy}_{SheetNumber}_{Scale}
→ JohnDoe_A101_1-100.pdf
```

## Profile Management

### Tạo Profile mới:
1. Cấu hình tất cả settings theo ý muốn
2. Tab "Profiles" → nhập "Tên profile mới"
3. Click "Save"

### Load Profile:
1. Tab "Profiles" → chọn profile từ dropdown
2. Click "Load"

### Export/Import Profiles:
```csharp
// Export profile
profileManager.ExportProfile("MyProfile", @"C:\Backup\MyProfile.json");

// Import profile
profileManager.ImportProfile(@"C:\Backup\MyProfile.json", "ImportedProfile");
```

## API Usage

### Batch Export programmatically:
```csharp
var batchExporter = new BatchExportManager();
var settings = new ExportSettings
{
    OutputFolder = @"C:\Export",
    ExportPDF = true,
    ExportDWG = true,
    FileNameTemplate = "{SheetNumber}_{SheetName}"
};

bool success = batchExporter.ExecuteBatchExport(document, selectedSheets, settings);
```

### Custom Paper Size Detection:
```csharp
var paperSizeManager = new PaperSizeManager();

// Detect từ sheet
var paperSize = paperSizeManager.DetectPaperSize(sheet);

// Get all standard sizes
var allSizes = paperSizeManager.GetAllStandardSizes();

// Custom size
var customSize = new PaperSize
{
    Name = "Custom Size",
    Width = 400,
    Height = 600,
    IsStandard = false
};
```

## Troubleshooting

### Common Issues:

1. **"No sheets found"**
   - Kiểm tra project có sheets không
   - Đảm bảo không bị filter loại bỏ hết sheets

2. **"PDF export failed"**
   - Kiểm tra quyền write vào output folder
   - Đảm bảo không có file cùng tên đang mở
   - Thử tắt PDF24 integration

3. **"Paper size detection failed"**
   - Kiểm tra Title Block có parameters Width/Height
   - Thử tắt auto-detect, dùng default size

4. **"Profile not found"**
   - Profiles lưu tại: `%APPDATA%\ProSheetsAddin\Profiles`
   - Tạo lại Default profile nếu bị mất

### Logs:
- Log files: `%APPDATA%\ProSheetsAddin\Logs`
- Debug info trong Visual Studio Output window

## Development

### Build Requirements:
- Visual Studio 2019/2022
- .NET Framework 4.8 SDK
- Revit API References

### Project Structure:
```
ProSheetsAddin/
├── Application.cs              # Main application entry
├── Commands/                   # External commands
│   ├── ExportCommand.cs
│   ├── ScheduleCommand.cs
│   └── TransmittalCommand.cs
├── Managers/                   # Business logic managers
│   ├── BatchExportManager.cs
│   ├── ProfileManager.cs
│   ├── PaperSizeManager.cs
│   └── SchedulingAssistant.cs
├── Models/                     # Data models
│   ├── ExportSettings.cs
│   ├── PaperSize.cs
│   └── XMLExportSettings.cs
├── Views/                      # WPF UI
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
└── Utils/                      # Utilities
    ├── FileNameGenerator.cs
    ├── ParameterUtils.cs
    └── NotificationHelper.cs
```

### Adding New Export Formats:
```csharp
// Implement in BatchExportManager.cs
private void ExportSheetToNewFormat(Document doc, ViewSheet sheet, ExportSettings settings, string outputPath)
{
    // Your export logic here
}
```

## License

Copyright © 2024 Licorp Vietnam. All rights reserved.

## Support

- Email: support@licorp.vn
- Website: https://www.licorp.vn
- Documentation: [Link to detailed docs]

## Version History

### v1.0.0
- Initial release
- Basic PDF/DWG/Image export
- Paper size auto-detection
- Profile management
- File naming templates

### Planned Features
- Cloud storage integration
- Advanced scheduling options
- Custom export plugins
- Multi-language support
- Batch processing improvements