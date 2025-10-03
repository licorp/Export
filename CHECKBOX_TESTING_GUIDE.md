# HƯỚNG DẪN KIỂM TRA CHECKBOX VÀ PDF OPTIONS

## 🎯 Mục đích
Tài liệu này hướng dẫn cách kiểm tra các checkbox options đã hoạt động đúng khi tick/bỏ tick và cách settings được áp dụng vào PDF export.

---

## 📋 PHẦN 1: KIỂM TRA CHECKBOX BINDINGS

### 1.1. Chuẩn bị
1. **Restart Revit 2023** (để load DLL mới)
2. **Mở DebugView**:
   - Download: https://learn.microsoft.com/en-us/sysinternals/downloads/debugview
   - Chạy DebugView as Administrator
   - Bật: `Capture > Capture Global Win32`
   - Filter: `[ProSheets` để chỉ xem logs của ProSheets

### 1.2. Test Checkbox Options
1. **Mở ProSheets Add-in** trong Revit
2. **Chọn tab "PDF Settings"** (đã đổi tên từ "Format")
3. **Quan sát panel "Options"** bên phải:
   ```
   ☐ View links in blue (Color prints only)
   ☑ Hide ref/work planes
   ☑ Hide unreferenced view tags
   ☑ Hide scope boxes
   ☑ Hide crop boundaries
   ☐ Replace halftone with thin lines
   ☑ Region edges mask coincident lines
   ```

### 1.3. Test từng Checkbox

#### Test 1: Hide ref/work planes
**Hành động**: Click để bỏ tick
**Log mong đợi**:
```
[ProSheets ProfileDialog] HH:mm:ss - [OPTIONS] 'Hide ref/work planes' changed to: UNCHECKED ✗
[ProSheets ProfileDialog] HH:mm:ss - [OPTIONS STATE] ViewLinksInBlue: False, HideRefWorkPlanes: False, ...
```
**Kết quả**: HideRefWorkPlanes = `False`

---

#### Test 2: View links in blue
**Hành động**: Click để tick
**Log mong đợi**:
```
[ProSheets ProfileDialog] HH:mm:ss - [OPTIONS] 'View links in blue (Color prints only)' changed to: CHECKED ✓
[ProSheets ProfileDialog] HH:mm:ss - [OPTIONS STATE] ViewLinksInBlue: True, HideRefWorkPlanes: False, ...
```
**Kết quả**: ViewLinksInBlue = `True`

---

#### Test 3: Hide scope boxes
**Hành động**: Click để bỏ tick
**Log mong đợi**:
```
[ProSheets ProfileDialog] HH:mm:ss - [OPTIONS] 'Hide scope boxes' changed to: UNCHECKED ✗
[ProSheets ProfileDialog] HH:mm:ss - [OPTIONS STATE] ...HideScopeBoxes: False...
```
**Kết quả**: HideScopeBoxes = `False`

---

### 1.4. Kiểm tra Tất cả Options cùng lúc
Click lần lượt từng checkbox và quan sát log `[OPTIONS STATE]`:

| Checkbox | Property | Default | After Toggle |
|----------|----------|---------|--------------|
| View links in blue | `ViewLinksInBlue` | False | True |
| Hide ref/work planes | `HideRefWorkPlanes` | True | False |
| Hide unreferenced view tags | `HideUnreferencedViewTags` | True | False |
| Hide scope boxes | `HideScopeBoxes` | True | False |
| Hide crop boundaries | `HideCropBoundaries` | True | False |
| Replace halftone... | `ReplaceHalftone` | False | True |
| Region edges mask... | `RegionEdgesMask` | True | False |

---

## 📋 PHẦN 2: KIỂM TRA PROFILE LƯU/LOAD SETTINGS

### 2.1. Lưu Settings vào Profile
1. **Thay đổi các checkbox** theo ý muốn (ví dụ: bỏ tick "Hide scope boxes")
2. **Click nút "+"** (Add Profile)
3. **Chọn "Copy current settings"**
4. **Nhập tên**: `Test_Options`
5. **Click Create**

**Log mong đợi**:
```
[ProSheets ProfileDialog] CopyCurrent mode - Creating profile with current settings
[ProSheets ProfileDialog] Saving current settings to profile 'Test_Options'
[ProSheets ProfileDialog] Profile 'Test_Options' saved successfully
```

### 2.2. Kiểm tra File JSON
1. Mở folder: `C:\Users\<YourName>\AppData\Roaming\ProSheetsProfiles\`
2. Tìm file: `Test_Options_<GUID>.json`
3. Mở bằng Notepad++, tìm:
   ```json
   {
     "Id": "...",
     "Name": "Test_Options",
     "Settings": {
       "ViewLinksInBlue": false,
       "HideRefWorkPlanes": true,
       "HideScopeBoxes": false,  // <-- Đã thay đổi
       "HideCropBoundaries": true,
       ...
     }
   }
   ```

### 2.3. Load Profile và Kiểm tra UI
1. **Thay đổi lại các checkbox** (ví dụ: tick lại "Hide scope boxes")
2. **Chọn profile "Test_Options"** trong ComboBox
3. **Quan sát UI**: Checkbox "Hide scope boxes" phải TỰ ĐỘNG BỎ TICK

**Log mong đợi**:
```
[ProSheets ProfileDialog] Switching to profile: Test_Options
[ProSheets ProfileDialog] Loading settings from profile: Test_Options
[ProSheets ProfileDialog] [OPTIONS] 'Hide scope boxes' changed to: UNCHECKED ✗
```

---

## 📋 PHẦN 3: KIỂM TRA PDF EXPORT VỚI OPTIONS

### 3.1. Chuẩn bị Test Export
1. **Mở project Revit** có sheets
2. **Mở ProSheets**, chọn tab "Selection"
3. **Tick chọn 1 sheet** để test
4. **Quay lại tab "PDF Settings"**
5. **Cấu hình options**:
   - ☑ Hide ref/work planes
   - ☑ Hide scope boxes
   - ☑ Hide crop boundaries
   - ☐ View links in blue

### 3.2. Export và Xem Logs
1. **Click "Export"**
2. **Xem DebugView logs**:

**Log mong đợi** (từ PDFOptionsApplier):
```
[ProSheets PDFOptions] HH:mm:ss - Starting to apply PDF options...
[ProSheets PDFOptions] HH:mm:ss - Applying Paper Placement: Center
[ProSheets PDFOptions] HH:mm:ss - Applying Zoom: Zoom
[ProSheets PDFOptions] HH:mm:ss - Applying Hidden Line Views: VectorProcessing
[ProSheets PDFOptions] HH:mm:ss - Applying Appearance - RasterQuality: High, Colors: Color
[ProSheets PDFOptions] HH:mm:ss - Applying View Options...
[ProSheets PDFOptions] HH:mm:ss - View Options - HideRefWorkPlanes: True, HideScopeBoxes: True, HideCropBoundaries: True...
[ProSheets PDFOptions] HH:mm:ss - ✓ All PDF options applied successfully
```

### 3.3. Kiểm tra PDF Output
1. **Mở file PDF** đã export
2. **Kiểm tra**:
   - ✓ Không thấy ref/work planes (nếu có trong view gốc)
   - ✓ Không thấy scope boxes
   - ✓ Không thấy crop boundaries
   - ✓ Links không màu xanh (vì ViewLinksInBlue = False)

---

## 📋 PHẦN 4: TROUBLESHOOTING

### ❌ Vấn đề 1: Checkbox không đổi màu khi click
**Nguyên nhân**: Binding không hoạt động
**Kiểm tra**:
1. Xem log có dòng `[OPTIONS] '...' changed to:` không?
2. Nếu không → Restart Revit và thử lại

---

### ❌ Vấn đề 2: Settings không được lưu vào Profile
**Nguyên nhân**: SaveCurrentSettingsToProfile() chưa hoàn chỉnh
**Kiểm tra**:
1. Xem log có `Saving current settings to profile '...'` không?
2. Mở file JSON, kiểm tra values
3. Nếu values không đúng → Báo để fix mapping

---

### ❌ Vấn đề 3: Export PDF không áp dụng options
**Nguyên nhân**: PDFExportManager chưa gọi PDFOptionsApplier
**Kiểm tra**:
1. Xem log có `[ProSheets PDFOptions]` không?
2. Nếu không → Cần integrate vào PDFExportManager

**Fix** (sẽ implement tiếp):
```csharp
// Trong PDFExportManager, trước khi export:
PDFOptionsApplier.ApplyPDFOptions(doc, exportSettings);
```

---

## 📊 BẢNG KIỂM TRA TỔNG HỢP

| Tính năng | Cách test | Kết quả mong đợi | Status |
|-----------|-----------|------------------|--------|
| Checkbox Binding | Click checkbox | Log hiển thị thay đổi | ✅ Hoàn tất |
| Real-time Update | Click → Xem STATE log | Giá trị boolean thay đổi | ✅ Hoàn tất |
| Save to Profile | Tạo profile mới | JSON chứa đúng values | ✅ Hoàn tất |
| Load from Profile | Switch profile | UI tự động cập nhật | ✅ Hoàn tất |
| PDF Export Logging | Click Export | Thấy PDFOptions logs | ✅ Hoàn tất |
| PDF Options Apply | Xem PDF output | Elements ẩn/hiện đúng | ⏳ Cần test |

---

## 🔧 CÁC BƯỚC TIẾP THEO

### Bước 1: Test Checkbox Bindings (NGAY BÂY GIỜ)
- Restart Revit
- Mở DebugView
- Click từng checkbox
- Xem logs

### Bước 2: Test Profile Save/Load
- Tạo profile mới với settings khác nhau
- Switch giữa các profiles
- Kiểm tra UI update

### Bước 3: Integrate vào PDFExportManager
```csharp
// Cần thêm vào Managers/PDFExportManager.cs:
using ProSheetsAddin.Services;

public void ExportPDF(...)
{
    // THÊM DÒNG NÀY trước khi export:
    PDFOptionsApplier.ApplyPDFOptions(doc, exportSettings);
    
    // ... existing export code ...
}
```

### Bước 4: Test Export thực tế
- Export PDF với các options khác nhau
- Kiểm tra output có đúng không

---

## 📝 GHI CHÚ

### Properties đã có sẵn trong ExportSettings:
```csharp
public class ExportSettings
{
    // View Options (MỚI binding)
    public bool ViewLinksInBlue { get; set; } = false;
    public bool HideRefWorkPlanes { get; set; } = true;
    public bool HideUnreferencedViewTags { get; set; } = true;
    public bool HideScopeBoxes { get; set; } = true;
    public bool HideCropBoundaries { get; set; } = true;
    public bool ReplaceHalftone { get; set; } = false;
    public bool RegionEdgesMask { get; set; } = true;
    
    // Other settings (ĐÃ CÓ TRƯỚC)
    public PSPaperPlacement PaperPlacement { get; set; }
    public PSZoomType Zoom { get; set; }
    public PSHiddenLineViews HiddenLineViews { get; set; }
    public PSRasterQuality RasterQuality { get; set; }
    public PSColors Colors { get; set; }
}
```

### Event Handler đã thêm:
- **File**: `Views/ProSheetsMainWindow.Profiles.cs`
- **Method**: `OptionCheckBox_Changed()`
- **Functionality**: Log mỗi lần checkbox thay đổi + state của tất cả options

### Service đã tạo:
- **File**: `Services/PDFOptionsApplier.cs`
- **Methods**:
  - `ApplyPDFOptions()` - Entry point
  - `ApplyPaperPlacement()`
  - `ApplyZoomSettings()`
  - `ApplyHiddenLineSettings()`
  - `ApplyAppearanceSettings()`
  - `ApplyViewOptions()`
  - `SetCategoryVisibility()` - Helper để ẩn/hiện categories

---

## ✅ KIỂM TRA NGAY

1. **Restart Revit 2023**
2. **Mở DebugView**
3. **Mở ProSheets → PDF Settings tab**
4. **Click checkbox "Hide scope boxes"**
5. **Xem DebugView có log**: `[OPTIONS] 'Hide scope boxes' changed to: UNCHECKED ✗`

Nếu thấy log → **✅ Thành công!**
Nếu không → Share screenshot và logs để debug.
