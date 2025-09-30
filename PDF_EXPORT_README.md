# Export + PDF Functionality - README

## ✅ Tính năng xuất PDF đã được cập nhật

### 🚀 **Các cải tiến mới:**

#### **1. Native Revit API PDF Export**
- ✅ Sử dụng `PDFExportManager` với Revit API native
- ✅ Hỗ trợ xuất từng sheet riêng biệt
- ✅ Tự động phát hiện kích thước giấy từ sheet
- ✅ Chất lượng cao (600 DPI) 
- ✅ Ẩn crop boundaries và scope boxes

#### **2. Tự động đặt tên file thông minh**
```
Format: {ProjectNumber}_{SheetNumber}_{SheetName}_{Revision}
Ví dụ: PRJ001_A01_Ground_Floor_Plan_RevA.pdf
```

**Tính năng:**
- ✅ Lấy thông tin project từ Revit
- ✅ Tự động làm sạch ký tự không hợp lệ 
- ✅ Giới hạn độ dài tên file (200 ký tự)
- ✅ Bao gồm revision number (nếu có)
- ✅ Thay thế khoảng trắng bằng underscore

#### **3. Debug Logging nâng cao**
- ✅ Tích hợp DebugView với `OutputDebugStringA`
- ✅ Timestamps chi tiết cho mỗi bước export
- ✅ Log thành công/thất bại từng sheet
- ✅ Thống kê tổng kết export

### 🛠️ **Cách sử dụng:**

#### **Bước 1: Chọn sheets**
1. Mở Export + từ Revit ribbon
2. Chọn các sheets cần export từ danh sách
3. Sử dụng "Toggle All" để chọn/bỏ chọn tất cả

#### **Bước 2: Cấu hình export**
1. **Format tab**: Chọn ☑️ PDF
2. **Folder**: Chọn thư mục xuất file
3. **Settings**: Cấu hình tùy chọn nâng cao

#### **Bước 3: Export**
1. Nhấn **"Create"** button
2. Xem xét thông tin export summary
3. Xác nhận **"Yes"** để bắt đầu

### 📋 **Export Summary hiển thị:**
```
EXPORT + SUMMARY

Sheets: 5
Formats: PDF
Output: C:\Export_Plus_Output\
Estimated Files: 5

Template: {SheetNumber}_{SheetName}
Combine Files: false
Include Revision: true

Tiếp tục xuất file?
```

### 📁 **Cấu trúc file xuất:**
```
C:\Export_Plus_Output\
├── PRJ001_A01_Ground_Floor_Plan_RevA.pdf
├── PRJ001_A02_First_Floor_Plan_RevA.pdf
├── PRJ001_E01_Electrical_Plan_RevB.pdf
└── PRJ001_S01_Structural_Plan_RevA.pdf
```

### 🔧 **Cấu hình PDF Options:**
```csharp
PDFExportOptions:
├── PaperFormat: Default (Auto-detect)
├── PaperOrientation: Auto
├── Combine: false (Individual files)
├── HideCropBoundaries: true
├── HideScopeBoxes: true
├── ColorDepth: Color mode
└── Quality: High (600 DPI)
```

### 📊 **Debug Logging Sample:**
```
[Export +] 14:30:25.123 - Starting PDF export for 3 sheets
[Export +] 14:30:25.145 - Output folder: C:\Export_Plus_Output\
[Export +] 14:30:25.167 - PDF options created
[Export +] 14:30:25.189 - Exporting sheet: A01 - Ground Floor Plan
[Export +] 14:30:25.234 - Generated filename: PRJ001_A01_Ground_Floor_Plan_RevA
[Export +] 14:30:26.456 - Successfully exported: PRJ001_A01_Ground_Floor_Plan_RevA.pdf
[Export +] 14:30:26.478 - SUCCESS: A01
[Export +] 14:30:26.501 - Export completed - Success: 3, Failed: 0
```

### ⚠️ **Lưu ý quan trọng:**

#### **Revit API Version Compatibility:**
- Tương thích với Revit 2022+
- Sử dụng reflection để handle version differences
- Fallback graceful nếu enum không tồn tại

#### **File Naming Rules:**
- Loại bỏ: `< > : " | ? * / \`
- Thay thế spaces với `_`
- Giới hạn 200 ký tự
- Thêm prefix project number nếu có

#### **Error Handling:**
- Validate sheets selection
- Validate output folder
- Validate format selection  
- Try-catch cho từng sheet export
- Comprehensive error logging

### 🏗️ **Technical Architecture:**

```
ProSheetsMainWindow.xaml.cs
    ├── Create_Click() - Main export handler
    ├── Validation logic
    └── Call PDFExportManager

PDFExportManager.cs
    ├── ExportSheetsToPDF() - Batch export
    ├── ExportSingleSheetToPDF() - Individual export
    ├── CreatePDFExportOptions() - Configuration
    ├── GenerateFileName() - Smart naming
    └── WriteDebugLog() - DebugView logging

BatchExportManager.cs
    └── Updated to use PDFExportManager
```

### 🎯 **Next Steps:**

#### **Planned Enhancements:**
- [ ] DWG export integration
- [ ] Batch combine PDF option
- [ ] Custom naming templates
- [ ] Progress bar for large exports
- [ ] Export profiles save/load
- [ ] Multi-threading support

#### **Current Status:**
✅ **PDF Export**: Fully functional with native Revit API  
⏳ **DWG Export**: Using existing managers  
⏳ **Other Formats**: Integration pending  

---

## 🚀 Ready to use Export + PDF functionality!

**Build Status:** ✅ Success (0 errors, warnings only)  
**Testing:** Ready for real-world Revit projects  
**Documentation:** Complete with debug examples  

Enjoy the enhanced PDF export experience! 🎉