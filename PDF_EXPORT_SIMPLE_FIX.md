# PDF Export Simple Fix

## Problem Summary
- "Invalid parameter passed to C runtime function" - repeated many times
- "Starting a new transaction is not permitted" - nested Transaction
- "CombinedFile cannot be set to false when PrintRange is Current/Visible"

## Root Causes
1. **Transaction nesting**: ApplyViewOptionsToSheet has Transaction inside, called from TransactionGroup
2. **CombinedFile property**: Must be TRUE when using PrintRange.Current
3. **Printer driver issues**: PDF24 may have compatibility issues

## Solution Steps

### 1. Fix Transaction Nesting
Use `ApplyViewOptionsToSheetNoTransaction()` inside Transaction (not TransactionGroup):

```csharp
using (Transaction trans = new Transaction(_document, "Apply PDF View Options"))
{
    trans.Start();
    foreach (var sheetItem in sheetItems)
    {
        ViewSheet sheet = _document.GetElement(sheetItem.Id) as ViewSheet;
        if (sheet != null)
        {
            PDFOptionsApplier.ApplyViewOptionsToSheetNoTransaction(_document, sheet, settings);
        }
    }
    trans.Commit();
}
```

### 2. Fix CombinedFile Property
```csharp
pm.PrintRange = PrintRange.Current;
pm.CombinedFile = true; // MUST be true for Current range
```

### 3. Add Extensive Logging
Log every step to identify where "Invalid parameter" occurs:
- Printer selection
- File path validation
- Each PrintParameters property change
- Before/after Apply()
- Before/after SubmitPrint()

### 4. Try Microsoft Print to PDF
If PDF24 fails, fallback to:
```csharp
pm.SelectNewPrintDriver("Microsoft Print to PDF");
```

## Files Modified
1. `Managers/PDFExportManager.cs` - Main export logic
2. `Services/PDFOptionsApplier.cs` - Added NoTransaction version
3. `Events/PDFExportEventHandler.cs` - ExternalEvent handler

## Testing Checklist
- [ ] No "Invalid parameter" errors
- [ ] No Transaction errors  
- [ ] PDF file created successfully
- [ ] All settings applied (zoom, colors, etc.)
- [ ] View options applied (hidden categories)
