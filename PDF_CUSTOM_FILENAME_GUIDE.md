# PDF Export với Custom File Name - Hướng dẫn sử dụng

## Tổng quan
ProSheets Addin hiện đã hỗ trợ xuất PDF với tên file tùy chỉnh (Custom File Name) cho từng sheet. Tính năng này cho phép bạn:
- Đặt tên file riêng cho mỗi sheet thay vì dùng tên mặc định
- Xuất nhiều sheet cùng lúc với tên file tùy chỉnh
- Theo dõi tiến trình export realtime
- Mở thư mục chứa file ngay sau khi export xong

## Quy trình sử dụng

### Bước 1: Chọn Sheets (Tab Selection)
1. Mở ProSheets Addin trong Revit
2. Vào tab **Selection**
3. Chọn các sheets cần export bằng cách tick vào checkbox
4. Bạn có thể filter theo:
   - View/Sheet Set
   - Discipline
   - Type

### Bước 2: Đặt tên file tùy chỉnh (Tab Custom File Name)
1. Vào tab **Custom File Name**
2. Trong danh sách sheets đã chọn, double-click vào cell **Custom File Name**
3. Nhập tên file mong muốn (không cần extension .pdf)
4. Nhấn Enter để lưu

**Lưu ý:**
- Nếu không đặt custom file name, hệ thống sẽ tự động tạo tên theo format: `ProjectNumber_SheetNumber_SheetName`
- Tên file sẽ được sanitize (loại bỏ các ký tự đặc biệt không hợp lệ)

### Bước 3: Chọn định dạng xuất (Tab Format)
1. Vào tab **Format**
2. Tick chọn **PDF** trong danh sách formats
3. Có thể chọn thêm các format khác (DWG, IFC...) nếu cần

### Bước 4: Cấu hình đầu ra (Tab Create)
1. Vào tab **Create**
2. Chọn thư mục đích:
   - Click nút 📁 bên cạnh **Folder Selection**
   - Chọn thư mục muốn lưu file
3. Chọn các tùy chọn:
   - **Save all files to one folder**: Lưu tất cả file vào 1 thư mục
   - **Save files in separate folders by format**: Tạo thư mục con cho mỗi format
   - **Report**: Chọn loại báo cáo (HTML/PDF/Text) hoặc "Don't Save Report"

### Bước 5: Export
1. Nhấn nút **START EXPORT** màu xanh lá
2. Hệ thống sẽ:
   - Hiển thị tiến trình export realtime trên progress bar
   - Cập nhật status cho từng item trong Export Queue
   - Hiển thị % hoàn thành
3. Sau khi export xong:
   - Dialog "Export completed." sẽ xuất hiện
   - Click **Open Folder** để mở thư mục chứa file
   - Click **✕** để đóng dialog

## Export Queue (Bảng theo dõi)
Trong tab Create, bảng **Export Queue** hiển thị:
- ☑ **Checkbox**: Chọn/bỏ chọn item
- **#**: Số thứ tự sheet
- **Name**: Tên sheet
- **Format**: Định dạng xuất (PDF, DWG, IFC...)
- **Size**: Khổ giấy (A0, A1, A2, A3, A4, Custom)
- **Orientation**: Portrait / Landscape
- **Progress**: Thanh tiến trình (0-100%)
- **Status**: 
  - **Pending** (Màu xám): Chờ export
  - **Processing** (Màu cam): Đang export
  - **Completed** (Màu xanh): Đã xong
  - **Error** (Màu đỏ): Lỗi

## Tính năng nâng cao

### Scheduling Assistant
Nếu muốn lên lịch export:
1. Bật toggle switch "The Scheduling Assistant"
2. Chọn ngày, giờ
3. Chọn lặp lại (Daily/Weekly/Monthly)
4. Nếu chọn Weekly: tick các ngày trong tuần
5. Click **START EXPORT** → Hệ thống sẽ lên lịch thay vì export ngay

### Set Paper Size & Orientation
- Click nút **Set Paper Size**: Áp dụng khổ giấy cho nhiều sheet cùng lúc
- Click nút **Set Orientation**: Đổi hướng giấy (Portrait/Landscape) cho nhiều sheet

## API Reference (Cho Developer)

### PDFExportManager.ExportSheetsWithCustomNames()
```csharp
public bool ExportSheetsWithCustomNames(
    List<SheetItem> sheetItems,           // Danh sách sheets với CustomFileName
    string outputFolder,                   // Thư mục đích
    ExportSettings settings,               // Cài đặt export
    Action<int, int, string> progressCallback = null  // Callback cập nhật tiến trình
)
```

**Parameters:**
- `sheetItems`: List<SheetItem> chứa Id, SheetNumber, SheetName, CustomFileName
- `outputFolder`: Đường dẫn thư mục lưu file
- `settings`: ExportSettings object (chứa cấu hình PDF options)
- `progressCallback`: (current, total, sheetNumber) => void

**Returns:**
- `true`: Export thành công (ít nhất 1 sheet)
- `false`: Export thất bại hoàn toàn

**Example:**
```csharp
var pdfManager = new PDFExportManager(_document);
bool result = pdfManager.ExportSheetsWithCustomNames(
    selectedSheets,
    @"C:\Export\Output",
    ExportSettings,
    (current, total, sheetNumber) => {
        Console.WriteLine($"Progress: {current}/{total} - {sheetNumber}");
    }
);
```

### ExportHandler (External Event)
Class xử lý export operations trong Revit API context:
```csharp
var exportHandler = new ExportHandler {
    Document = doc,
    SheetsToExport = selectedSheets,
    Formats = new List<string> { "PDF", "DWG" },
    OutputFolder = @"C:\Export\Output",
    ExportSettings = settings,
    ProgressCallback = (current, total, sheetNumber) => { ... },
    CompletionCallback = (success, message) => { ... }
};

var exportEvent = ExternalEvent.Create(exportHandler);
exportEvent.Raise();
```

## Troubleshooting

### Vấn đề: Export không chạy
- Kiểm tra đã chọn sheets chưa (tab Selection)
- Kiểm tra đã chọn format PDF chưa (tab Format)
- Kiểm tra đường dẫn output folder có hợp lệ không

### Vấn đề: Tên file không đúng
- Kiểm tra custom file name có chứa ký tự đặc biệt không
- Hệ thống sẽ tự động loại bỏ: \ / : * ? " < > |
- Nếu để trống custom file name → tự động tạo tên mặc định

### Vấn đề: Progress bar không cập nhật
- Đợi một chút, export PDF có thể mất thời gian với sheets lớn
- Kiểm tra DebugView để xem log chi tiết

### Vấn đề: Dialog "Export completed" không hiện
- Check file log trong DebugView
- Có thể dialog bị che khuất, check taskbar

## Changelog

### Version 1.2.0 (Current)
- ✅ Hỗ trợ custom file name cho PDF export
- ✅ Export Queue với realtime progress tracking
- ✅ Dialog "Export completed" với nút Open Folder
- ✅ Folder browser dialog cho Create tab
- ✅ External Event handler cho export operations
- ✅ Paper size auto-detection (A0-A4)
- ✅ Orientation detection (Portrait/Landscape)

### Version 1.1.0
- Xuất PDF với tên mặc định
- Multi-format export (PDF, DWG, IFC)
- Profile management
- Custom file name editor

## Liên hệ & Hỗ trợ
- GitHub: [licorp/Export](https://github.com/licorp/Export)
- Issues: Report bugs tại GitHub Issues
- Documentation: Xem thêm trong thư mục `/docs`
