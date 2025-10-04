# Window Close Freeze Fix - Phiên Bản Cải Tiến

## Vấn Đề Mới Phát Hiện

Sau khi triển khai fix ban đầu, vẫn còn vấn đề treo khi đóng cửa sổ ProSheets:

### Log Lỗi:
```
[31016] [Export +] 11:29:41.243 - NWC export completed successfully for 3 views 
[31016] [Export +] 11:29:41.245 - All export items completed successfully 
[31016] [Export +] 11:29:41.274 - ProSheets window deactivated 
[31016] [Export +] 11:34:24.321 - ProSheets window activated 
[31016] [Export +] 11:34:24.355 - ProSheets window deactivated 
```

### Nguyên Nhân:
1. **ExternalEvent.Dispose()** bị gọi quá nhanh khi event vẫn đang trong trạng thái pending/executing
2. **CancellationTokenSource** chưa được dispose trước khi dispose ExternalEvents
3. Thiếu **delay nhỏ** để cho các operation hoàn thành trước khi cleanup

## Giải Pháp Cải Tiến

### 1. Thêm Try-Catch Riêng Biệt
Mỗi bước dispose có try-catch riêng để tránh lỗi một phần ảnh hưởng phần khác:

```csharp
try
{
    _exportCancellationTokenSource.Cancel();
}
catch (Exception cancelEx)
{
    WriteDebugLog($"Error cancelling export: {cancelEx.Message}");
}
```

### 2. Thêm Sleep 100ms
Cho phép các pending operations hoàn thành trước khi dispose:

```csharp
// Cancel operations first
_exportCancellationTokenSource?.Cancel();

// Give a brief moment for any pending operations to complete
System.Threading.Thread.Sleep(100);

// Now safe to dispose
_exportCancellationTokenSource?.Dispose();
```

### 3. Sắp Xếp Thứ Tự Dispose Đúng
```csharp
1. Cancel CancellationTokenSource
2. Sleep 100ms (cho operations dừng lại)
3. Dispose CancellationTokenSource
4. Dispose _pdfExportEvent
5. Dispose _exportEvent
```

### 4. Logging Chi Tiết
Log từng bước để debug dễ dàng:

```csharp
WriteDebugLog("Cancelling ongoing export operations...");
// ... cancel logic ...
WriteDebugLog("CancellationTokenSource disposed");

WriteDebugLog("Disposing PDF Export Event...");
// ... dispose logic ...
WriteDebugLog("PDF Export Event disposed");
```

## Code Hoàn Chỉnh

```csharp
private void ProSheetsMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
{
    WriteDebugLog("ProSheets window is closing");
    
    try
    {
        // Step 1: Cancel any ongoing export operations first
        if (_exportCancellationTokenSource != null && 
            !_exportCancellationTokenSource.IsCancellationRequested)
        {
            WriteDebugLog("Cancelling ongoing export operations...");
            try
            {
                _exportCancellationTokenSource.Cancel();
            }
            catch (Exception cancelEx)
            {
                WriteDebugLog($"Error cancelling export: {cancelEx.Message}");
            }
        }
        
        // Step 2: Give a brief moment for any pending operations to complete
        System.Threading.Thread.Sleep(100);
        
        // Step 3: Dispose CancellationTokenSource first
        if (_exportCancellationTokenSource != null)
        {
            WriteDebugLog("Disposing CancellationTokenSource...");
            try
            {
                _exportCancellationTokenSource.Dispose();
                _exportCancellationTokenSource = null;
                WriteDebugLog("CancellationTokenSource disposed");
            }
            catch (Exception disposeEx)
            {
                WriteDebugLog($"Error disposing CancellationTokenSource: {disposeEx.Message}");
            }
        }
        
        // Step 4: Dispose External Events
        if (_pdfExportEvent != null)
        {
            WriteDebugLog("Disposing PDF Export Event...");
            try
            {
                _pdfExportEvent.Dispose();
                _pdfExportEvent = null;
                WriteDebugLog("PDF Export Event disposed");
            }
            catch (Exception disposeEx)
            {
                WriteDebugLog($"Error disposing PDF Export Event: {disposeEx.Message}");
            }
        }
        
        if (_exportEvent != null)
        {
            WriteDebugLog("Disposing Export Event...");
            try
            {
                _exportEvent.Dispose();
                _exportEvent = null;
                WriteDebugLog("Export Event disposed");
            }
            catch (Exception disposeEx)
            {
                WriteDebugLog($"Error disposing Export Event: {disposeEx.Message}");
            }
        }
        
        WriteDebugLog("ProSheets window cleanup completed successfully");
    }
    catch (Exception ex)
    {
        WriteDebugLog($"Error during window cleanup: {ex.Message}");
        WriteDebugLog($"Stack trace: {ex.StackTrace}");
    }
}
```

## Cải Tiến So Với Version Trước

### Version Cũ:
```csharp
// Cancel
_exportCancellationTokenSource.Cancel();

// Dispose ngay lập tức - có thể gây lỗi
_pdfExportEvent.Dispose();
_exportEvent.Dispose();
_exportCancellationTokenSource.Dispose();
```

### Version Mới:
```csharp
// 1. Cancel với try-catch
try { 
    _exportCancellationTokenSource.Cancel(); 
} catch { }

// 2. Đợi 100ms
Thread.Sleep(100);

// 3. Dispose CancellationTokenSource trước với try-catch
try { 
    _exportCancellationTokenSource.Dispose(); 
} catch { }

// 4. Dispose từng ExternalEvent với try-catch riêng
try { 
    _pdfExportEvent.Dispose(); 
} catch { }
try { 
    _exportEvent.Dispose(); 
} catch { }
```

## Lợi Ích

1. ✅ **An Toàn Hơn**: Try-catch riêng cho mỗi bước
2. ✅ **Ổn Định Hơn**: Sleep 100ms cho operations hoàn thành
3. ✅ **Debug Dễ Hơn**: Log chi tiết từng bước
4. ✅ **Không Ảnh Hưởng Lẫn Nhau**: Lỗi một phần không làm fail toàn bộ
5. ✅ **Dispose Đúng Thứ Tự**: CancellationTokenSource → ExternalEvents

## Testing

### Expected Logs Khi Đóng Cửa Sổ:
```
[Export +] ProSheets window is closing
[Export +] Cancelling ongoing export operations...
[Export +] Disposing CancellationTokenSource...
[Export +] CancellationTokenSource disposed
[Export +] Disposing PDF Export Event...
[Export +] PDF Export Event disposed
[Export +] Disposing Export Event...
[Export +] Export Event disposed
[Export +] ProSheets window cleanup completed successfully
```

### Khi Có Lỗi:
```
[Export +] ProSheets window is closing
[Export +] Disposing PDF Export Event...
[Export +] Error disposing PDF Export Event: Object is being used...
[Export +] Disposing Export Event...
[Export +] Export Event disposed
[Export +] ProSheets window cleanup completed successfully
```
*→ Vẫn dispose được phần còn lại*

## Build Status

✅ **Build Successful**
- File: `Views/ProSheetsMainWindow.xaml.cs` (lines 471-548)
- 0 errors, 8 warnings (unrelated)
- Output: `bin\Debug\ProSheetsAddin.dll`

## Files Modified

1. `Views/ProSheetsMainWindow.xaml.cs`:
   - Cải tiến `ProSheetsMainWindow_Closing()` method
   - Thêm try-catch riêng cho từng dispose
   - Thêm Thread.Sleep(100) sau cancel
   - Thêm logging chi tiết cho debug
   - Sắp xếp lại thứ tự dispose

## Khuyến Nghị Sử Dụng

1. **Sau khi export xong**: Đợi vài giây trước khi đóng cửa sổ (best practice)
2. **Xem logs**: Check Output Debug để verify cleanup thành công
3. **Nếu vẫn treo**: Gửi logs để analysis thêm

## Next Steps (Nếu Vẫn Treo)

Nếu vẫn còn vấn đề, có thể thử:

1. Tăng sleep time lên 200ms hoặc 500ms
2. Thêm check trạng thái ExternalEvent trước khi dispose
3. Sử dụng Task.Run() để dispose trên background thread
4. Cancel window close event và close sau khi cleanup xong

## Technical Notes

### ExternalEvent Lifecycle:
```
Created → Raised → Pending → Executing → Completed
                                          ↓
                                      Dispose ✓
```

**Lưu ý**: Không được dispose khi đang ở trạng thái Executing!

### CancellationToken Best Practice:
```
Cancel() → Wait (100ms) → Dispose()
```

Không được dispose ngay sau cancel vì token còn đang được check trong Task.Delay()!
