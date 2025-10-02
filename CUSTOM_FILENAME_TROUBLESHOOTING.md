# Custom File Name Dialog - Troubleshooting Guide

## 🐛 Common Issues & Solutions

### Issue 1: Dialog Won't Open / Error on Click

#### Symptoms:
- Click "Custom File Name" header → Nothing happens
- Error message: "Error initializing Custom File Name Dialog"
- Application crash when opening dialog

#### Solutions:

**Solution A: Check Debug Output**
```
1. Open Visual Studio
2. Go to: View → Output
3. Look for lines starting with "ERROR in CustomFileNameDialog"
4. The error message will tell you exactly what went wrong
```

**Solution B: Verify Selection**
```
1. Make sure you have at least ONE sheet/view selected
2. Check boxes should be checked (✓)
3. Try selecting just one item first
```

**Solution C: Check Document**
```csharp
// The dialog needs a valid Revit document
// If document is null, it will still work but with limited parameters
```

---

### Issue 2: Parameters List is Empty

#### Symptoms:
- Left panel shows "Available Parameters" but list is empty
- Only title, no items

#### Solutions:

**Solution A: Click Refresh Button**
```
Click the ⟳ (Refresh) button in the control panel
This will reload all parameters
```

**Solution B: Check Document**
```
- Dialog loads 25+ built-in parameters by default
- If you see nothing, there may be a XAML binding issue
- Check Debug Output for "LoadAvailableParameters" messages
```

---

### Issue 3: Search Not Working

#### Symptoms:
- Type in search box → Nothing happens
- List doesn't filter

#### Solutions:

**Solution A: Check Placeholder Text**
```
- Placeholder "🔍 Search parameters..." should disappear when typing
- If it stays, there's a binding issue
```

**Solution B: Clear and Retry**
```
1. Clear search box completely
2. Wait for list to show all parameters
3. Type again slowly
```

**Solution C: Code Fix** (if above doesn't work)
```csharp
// In CustomFileNameDialog.xaml.cs
private void SearchParameters_TextChanged(object sender, TextChangedEventArgs e)
{
    System.Diagnostics.Debug.WriteLine($"Search text changed: '{SearchParametersTextBox.Text}'");
    ApplyParameterFilter(SearchParametersTextBox.Text);
}
```

---

### Issue 4: Double-Click Doesn't Add Parameter

#### Symptoms:
- Double-click parameter in left list → Nothing happens
- Parameter doesn't appear in right table

#### Solutions:

**Solution A: Use Add Button Instead**
```
1. Single-click parameter to select it
2. Click the [+] (Add) button
```

**Solution B: Check Selection**
```
- Make sure parameter is highlighted (selected) before double-clicking
- Blue highlight = selected
```

**Solution C: Check Debug Output**
```
Look for error messages starting with:
"Error processing parameter:"
```

---

### Issue 5: Preview Doesn't Update

#### Symptoms:
- Add parameters → Preview shows old text
- Change Prefix/Suffix → Preview doesn't change

#### Solutions:

**Solution A: Manual Refresh**
```
1. Click away from the textbox (loses focus)
2. Preview should update automatically
```

**Solution B: Add/Remove Parameter**
```
- Adding or removing any parameter triggers preview update
- This can force a refresh
```

**Solution C: Check Binding**
```csharp
// Preview should be bound to PreviewText property
// In XAML:
<TextBlock Text="{Binding PreviewText}" />
```

---

### Issue 6: Can't Edit Prefix/Suffix/Separator

#### Symptoms:
- Click in Prefix column → Can't type
- Cells appear read-only

#### Solutions:

**Solution A: Double-Click to Edit**
```
DataGrid requires double-click to enter edit mode
Single-click selects, double-click edits
```

**Solution B: Press F2**
```
1. Single-click cell to select
2. Press F2 key to enter edit mode
3. Type your value
4. Press Enter to confirm
```

**Solution C: Check DataGrid Settings**
```xaml
<!-- Columns should have Mode=TwoWay -->
<DataGridTextColumn Binding="{Binding Prefix, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```

---

### Issue 7: Move Up/Down Buttons Don't Work

#### Symptoms:
- Click ↑ or ↓ → Parameter doesn't move
- Parameter stays in same position

#### Solutions:

**Solution A: Select Row First**
```
1. Click anywhere in the row to select it
2. Then click Move Up or Move Down
```

**Solution B: Check Bounds**
```
- Can't move first item up
- Can't move last item down
- These operations do nothing (by design)
```

---

### Issue 8: OK Button Does Nothing

#### Symptoms:
- Click OK → Dialog closes but nothing happens
- Custom File Name column not updated

#### Solutions:

**Solution A: Check Selected Sheets**
```
Dialog applies ONLY to sheets/views that were selected BEFORE opening
If you didn't select any, nothing will be updated
```

**Solution B: Check Debug Output**
```
Look for lines like:
"Updated X sheets with custom filename configuration"
```

**Solution C: Verify Parameters**
```
- At least one parameter must be in the configuration table
- If table is empty, validation will fail
```

---

### Issue 9: Custom File Name Not Generated Correctly

#### Symptoms:
- OK button works but filename is wrong
- Missing parts or extra characters

#### Solutions:

**Solution A: Check Parameter Values**
```
Some parameters may be empty in your sheets
Empty parameters are automatically skipped
```

**Solution B: Verify Separator**
```
Default separator is "-" (dash)
Check if you changed it accidentally
Common separators: - _ (space) . |
```

**Solution C: Check Prefix/Suffix**
```
Review each row in configuration table
Prefix adds BEFORE value
Suffix adds AFTER value
Example: Prefix="Rev" + Value="A" → "RevA"
```

---

### Issue 10: Special Characters in Filename

#### Symptoms:
- Filename contains: \ / : * ? " < > |
- Export fails later

#### Solutions:

**Solution A: Avoid Invalid Characters**
```
Windows doesn't allow these characters in filenames:
\ / : * ? " < > |

Use alternatives:
: → - (dash)
/ → - (dash)
? → (remove)
```

**Solution B: Validate Before Applying**
```csharp
// Add validation in OK button click:
if (PreviewText.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
{
    MessageBox.Show("Filename contains invalid characters!");
    return;
}
```

---

## 📊 Debug Checklist

When dialog has issues, check these in order:

### 1. Visual Studio Output Window
```
View → Output → Show output from: Debug
Look for lines containing "CustomFileNameDialog"
```

### 2. Selection State
```
- At least one sheet/view selected? ✓
- Checkbox is checked? ✓
- Blue highlight in DataGrid? ✓
```

### 3. Document Availability
```
- Revit document is open? ✓
- Document passed to dialog constructor? ✓
```

### 4. Parameter Loading
```
Debug Output should show:
"Starting LoadAvailableParameters..."
"Added 25+ built-in parameters"
"LoadAvailableParameters completed. Total: X parameters"
```

### 5. XAML Binding
```
Check for XAML errors in Output window
Look for: "System.Windows.Data Error"
```

---

## 🔧 Advanced Debugging

### Enable Detailed Logging

Add this to constructor:

```csharp
public CustomFileNameDialog(Document document = null)
{
    System.Diagnostics.Debug.WriteLine("=== CustomFileNameDialog CONSTRUCTOR START ===");
    try
    {
        System.Diagnostics.Debug.WriteLine("Step 1: InitializeComponent");
        InitializeComponent();
        
        System.Diagnostics.Debug.WriteLine("Step 2: Set DataContext");
        DataContext = this;
        
        System.Diagnostics.Debug.WriteLine($"Step 3: Document = {(document == null ? "NULL" : "OK")}");
        _document = document;
        
        System.Diagnostics.Debug.WriteLine("Step 4: LoadAvailableParameters");
        LoadAvailableParameters();
        
        System.Diagnostics.Debug.WriteLine("Step 5: LoadDefaultConfiguration");
        LoadDefaultConfiguration();
        
        System.Diagnostics.Debug.WriteLine("Step 6: UpdatePreview");
        UpdatePreview();
        
        System.Diagnostics.Debug.WriteLine("Step 7: Subscribe to events");
        _selectedParameters.CollectionChanged += (s, e) => UpdatePreview();
        
        System.Diagnostics.Debug.WriteLine("=== CustomFileNameDialog CONSTRUCTOR END (SUCCESS) ===");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"=== EXCEPTION IN CONSTRUCTOR ===");
        System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
        throw;
    }
}
```

### Check Each Method

```csharp
// Add at start of every method:
System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] METHOD: {nameof(MethodName)} START");

// Add at end:
System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] METHOD: {nameof(MethodName)} END");
```

---

## 🆘 Still Not Working?

If none of the above solutions work:

### 1. Check Build Output
```powershell
# In PowerShell:
cd "path\to\project"
msbuild ProSheetsAddin.csproj /t:Build /v:detailed > build.log 2>&1
# Check build.log for errors
```

### 2. Verify DLL is Updated
```powershell
# Check DLL timestamp:
Get-Item "bin\Debug\ProSheetsAddin.dll" | Select-Object LastWriteTime
```

### 3. Clean and Rebuild
```powershell
msbuild ProSheetsAddin.csproj /t:Clean
msbuild ProSheetsAddin.csproj /t:Build
```

### 4. Check Revit Version
```
Dialog is tested with Revit 2023-2026
If using older version, some APIs may not exist
```

---

## 📞 Get Help

If issue persists:

1. **Copy Debug Output**: View → Output → Copy All
2. **Take Screenshot**: Of the error or issue
3. **Note Steps**: Exact steps to reproduce
4. **Check Documentation**: See CUSTOM_FILENAME_DIALOG.md

---

**Last Updated**: October 2, 2025  
**Version**: 1.0  
**Revit Versions**: 2023-2026
