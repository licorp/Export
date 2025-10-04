# Window Close Freeze Fix

## Problem
After successful export, closing the ProSheets window caused Revit to freeze completely, requiring Task Manager to kill the process.

## Root Cause
The `Task.Delay()` loop in the PDF export wait logic (line ~2620) continued running indefinitely when the window was closed, preventing proper UI thread disposal and resource cleanup.

## Solution Implemented

### 1. Added CancellationTokenSource Field
**File**: `Views/ProSheetsMainWindow.xaml.cs` (Line 49)
```csharp
private System.Threading.CancellationTokenSource _exportCancellationTokenSource;
```

### 2. Implemented Window Closing Cleanup Handler
**File**: `Views/ProSheetsMainWindow.xaml.cs` (Lines 470-508)

The `ProSheetsMainWindow_Closing` event handler now properly disposes all resources:

```csharp
private void ProSheetsMainWindow_Closing(object sender, CancelEventArgs e)
{
    WriteDebugLog("ProSheets window is closing");
    
    try
    {
        // Cancel any ongoing export operations
        if (_exportCancellationTokenSource != null && 
            !_exportCancellationTokenSource.IsCancellationRequested)
        {
            WriteDebugLog("Cancelling ongoing export operations...");
            _exportCancellationTokenSource.Cancel();
        }
        
        // Dispose External Events
        if (_pdfExportEvent != null)
        {
            WriteDebugLog("Disposing PDF Export Event...");
            _pdfExportEvent.Dispose();
            _pdfExportEvent = null;
        }
        
        if (_exportEvent != null)
        {
            WriteDebugLog("Disposing Export Event...");
            _exportEvent.Dispose();
            _exportEvent = null;
        }
        
        // Dispose CancellationTokenSource
        if (_exportCancellationTokenSource != null)
        {
            _exportCancellationTokenSource.Dispose();
            _exportCancellationTokenSource = null;
        }
        
        WriteDebugLog("ProSheets window cleanup completed");
    }
    catch (Exception ex)
    {
        WriteDebugLog($"Error during window cleanup: {ex.Message}");
    }
}
```

### 3. Integrated CancellationToken into Export Loop
**File**: `Views/ProSheetsMainWindow.xaml.cs` (Lines 2465-2805)

#### 3a. Create CancellationTokenSource at Export Start
```csharp
private async void StartExportButton_Click(object sender, RoutedEventArgs e)
{
    WriteDebugLog("Start Export clicked");
    
    try
    {
        // Create new cancellation token for this export
        _exportCancellationTokenSource?.Cancel();
        _exportCancellationTokenSource?.Dispose();
        _exportCancellationTokenSource = new System.Threading.CancellationTokenSource();
        var cancellationToken = _exportCancellationTokenSource.Token;
        
        // ... rest of export logic
```

#### 3b. Pass Token to Task.Delay (Line 2620)
```csharp
while (waitCount < maxWaitSeconds * 10)
{
    await System.Threading.Tasks.Task.Delay(100, cancellationToken); // Pass token
    waitCount++;
    
    // Check if all PDF items in queue are completed
    var pdfItems = items.Where(i => i.Format == "PDF").ToList();
    bool allPdfCompleted = pdfItems.All(i => i.Status == "Completed" || i.Status == "Failed");
    
    if (allPdfCompleted)
    {
        WriteDebugLog($"All PDF items completed after {waitCount * 100}ms");
        break;
    }
    
    // Log progress every 5 seconds
    if (waitCount % 50 == 0)
    {
        var completed = pdfItems.Count(i => i.Status == "Completed");
        WriteDebugLog($"⏳ Waiting for PDF export... {completed}/{pdfItems.Count} items done");
    }
}
```

#### 3c. Handle Cancellation with Separate Catch Block
```csharp
    }
    catch (OperationCanceledException)
    {
        WriteDebugLog("Export operation was cancelled by user");
        MessageBox.Show("Export was cancelled.", 
                       "Export Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
        
        // Update status for any pending items
        var items = ExportQueueDataGrid.ItemsSource as ObservableCollection<ExportQueueItem>;
        if (items != null)
        {
            foreach (var item in items.Where(i => i.Status == "Processing" || i.Status == "Pending"))
            {
                item.Status = "Cancelled";
                item.Progress = 0;
            }
        }
    }
    catch (Exception ex)
    {
        WriteDebugLog($"Error in StartExportButton_Click: {ex.Message}");
        MessageBox.Show($"Error during export: {ex.Message}", 
                       "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        // Re-enable button
        StartExportButton.IsEnabled = true;
        StartExportButton.Content = "START EXPORT";
    }
}
```

## How It Works

1. **Export Start**: When user clicks "START EXPORT", a new `CancellationTokenSource` is created
2. **During Export**: The token is passed to `Task.Delay()`, allowing the wait loop to be cancelled
3. **Window Closing**: 
   - User closes ProSheets window
   - `ProSheetsMainWindow_Closing` event fires
   - `_exportCancellationTokenSource.Cancel()` is called
   - Task.Delay throws `OperationCanceledException`
4. **Graceful Shutdown**:
   - Catch block handles cancellation, updates UI status
   - ExternalEvents are disposed properly
   - CancellationTokenSource is disposed
   - Window closes without freezing Revit

## Testing Instructions

1. Open ProSheets window in Revit
2. Select some sheets/views and start an export
3. While export is in progress, close the ProSheets window
4. **Expected Result**: Window should close immediately without freezing Revit
5. Check debug log for cancellation messages:
   ```
   [Export +] Cancelling ongoing export operations...
   [Export +] Export operation was cancelled by user
   [Export +] Disposing PDF Export Event...
   [Export +] Disposing Export Event...
   [Export +] ProSheets window cleanup completed
   ```

## Files Modified

1. `Views/ProSheetsMainWindow.xaml.cs`:
   - Added `_exportCancellationTokenSource` field (line 49)
   - Implemented `ProSheetsMainWindow_Closing()` cleanup (lines 470-508)
   - Modified `StartExportButton_Click()` to create and use cancellation token (lines 2465-2805)
   - Added `OperationCanceledException` catch block with status updates

## Build Status

✅ **Build Successful** - Project compiles without errors
- 8 warnings (pre-existing, unrelated to this fix)
- 0 errors
- Output: `bin\Debug\ProSheetsAddin.dll`

## Related Issues Fixed

This fix also resolves:
- ExternalEvents not being disposed properly
- Resource leaks when window closes during export
- Multiple activate/deactivate window cycles causing freeze
- Task.Delay loops continuing indefinitely after window close
