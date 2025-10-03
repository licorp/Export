# 🔧 Radio Button GroupName Fix

## ❌ Problem
```
[59560] [ProSheets ProfileDialog] 14:40:49.349 - ImportFile mode selected
[59560] [ProSheets ProfileDialog] 14:40:58.959 - Mode selected: CopyCurrent, Profile name: 'crr_1'
```

**Issue**: User selected "Import from File" and chose XML file, but when clicking "Create", mode was detected as `CopyCurrent` instead of `ImportFile`.

## 🔍 Root Cause

**XAML Before**:
```xaml
<RadioButton x:Name="CopyCurrentRadio" Content="Copy from current settings" IsChecked="True" Margin="0,0,0,8" FontSize="12"/>
<RadioButton x:Name="UseDefaultRadio" Content="Use default settings" Margin="0,0,0,8" FontSize="12"/>
<RadioButton x:Name="ImportFileRadio" Content="Import from a file" VerticalAlignment="Center" FontSize="12"/>
```

**Problem**: Radio buttons had **NO GroupName** attribute!

In WPF, when radio buttons are in the same container (e.g., StackPanel), they should automatically form a group. However, when setting `ImportFileRadio.IsChecked = true` programmatically in code-behind:

```csharp
ImportFileRadio.IsChecked = true;  // ← Set in Browse button click
```

WPF may **NOT automatically uncheck** the other radio buttons if they don't have an explicit `GroupName`. This caused:
1. `ImportFileRadio.IsChecked = true` triggered → "ImportFile mode selected" logged
2. But `CopyCurrentRadio.IsChecked` remained `true` (default)
3. When "Create" button clicked, validation checked `CopyCurrentRadio.IsChecked == true` **FIRST**
4. Result: Mode detected as `CopyCurrent` ❌

## ✅ Solution

**XAML After**:
```xaml
<RadioButton x:Name="CopyCurrentRadio" Content="Copy from current settings" IsChecked="True" Margin="0,0,0,8" FontSize="12" GroupName="ProfileMode"/>
<RadioButton x:Name="UseDefaultRadio" Content="Use default settings" Margin="0,0,0,8" FontSize="12" GroupName="ProfileMode"/>
<RadioButton x:Name="ImportFileRadio" Content="Import from a file" VerticalAlignment="Center" FontSize="12" GroupName="ProfileMode"/>
```

**Changes**:
- Added `GroupName="ProfileMode"` to ALL 3 radio buttons
- Now WPF will **automatically uncheck** other radio buttons when one is checked
- When `ImportFileRadio.IsChecked = true` is set in code, WPF will uncheck `CopyCurrentRadio` and `UseDefaultRadio`

## 📊 Expected Behavior

**Test Scenario**:
1. Click "Add Profile"
2. Click "..." button → Select "crr_1.xml"
3. Click "Create"

**Expected Logs**:
```
[ProSheets ProfileDialog] Browse button clicked
[ProSheets ProfileDialog] File selected: D:\OneDrive\Desktop\crr_1.xml
[ProSheets ProfileDialog] Profile name changed: 'crr_1'
[ProSheets ProfileDialog] Auto-filled profile name: 'crr_1'
[ProSheets ProfileDialog] ImportFile mode selected  ← Radio button checked via code
[ProSheets ProfileDialog] Create button clicked
[ProSheets ProfileDialog] Mode selected: ImportFile, Profile name: 'crr_1'  ← ✅ CORRECT!
[ProSheets ProfileDialog] Profile creation confirmed - Mode: ImportFile, Name: 'crr_1'
```

**Then in main window**:
```
[Export +] Creating new profile: crr_1, Mode: ImportFile  ← ✅ CORRECT!
[Export +] ImportFile mode - Starting import from: D:\OneDrive\Desktop\crr_1.xml
[XMLProfileManager] XML profile loaded: crr_1
[Export +] === IMPORTING XML PROFILE SETTINGS ===
[Export +] Format flags set - PDF:True, DWG:False, ...
[Export +] PDF settings set - Vector:True, Quality:High, ...
[Export +] === SAVING PROFILE WITH ALL SETTINGS ===
[Export +] === APPLYING SETTINGS TO UI ===
[Export +] Settings applied to UI successfully
```

## 🧪 How to Test

### Test 1: Import XML Profile
```
1. Open ProSheetsAddin in Revit
2. Click "Add Profile"
3. Click "..." button
4. Select "crr_1.xml"
5. Verify profile name auto-fills: "crr_1"
6. Verify "Import from a file" radio is checked
7. Click "Create"
8. ✅ Check DebugView for "Mode: ImportFile" (NOT "Mode: CopyCurrent")
9. ✅ Check settings are applied to UI (Vector Processing checked, etc.)
```

### Test 2: Copy Current Settings
```
1. Click "Add Profile"
2. Type name: "MyProfile"
3. Leave "Copy from current settings" checked (default)
4. Click "Create"
5. ✅ Check DebugView for "Mode: CopyCurrent"
6. ✅ Verify profile created with current settings
```

### Test 3: Use Default Settings
```
1. Click "Add Profile"
2. Type name: "DefaultTest"
3. Click "Use default settings" radio button
4. Click "Create"
5. ✅ Check DebugView for "Mode: UseDefault"
```

### Test 4: Radio Button Exclusivity
```
1. Click "Add Profile"
2. Click "Use default settings"
3. ✅ Verify "Copy from current settings" is unchecked
4. Click "..." and select XML file
5. ✅ Verify "Import from a file" is checked
6. ✅ Verify "Use default settings" is unchecked
7. Click "Copy from current settings"
8. ✅ Verify "Import from a file" is unchecked
```

## 📝 Technical Details

### WPF Radio Button Grouping

**Without GroupName**:
```csharp
// When you do this in code:
ImportFileRadio.IsChecked = true;

// WPF behavior:
// - ImportFileRadio.IsChecked = true ✓
// - CopyCurrentRadio.IsChecked = true (unchanged!) ❌
// - UseDefaultRadio.IsChecked = false ✓
// Result: MULTIPLE radio buttons checked!
```

**With GroupName**:
```csharp
// When you do this in code:
ImportFileRadio.IsChecked = true;

// WPF behavior (with GroupName="ProfileMode"):
// - ImportFileRadio.IsChecked = true ✓
// - CopyCurrentRadio.IsChecked = false (auto-unchecked!) ✓
// - UseDefaultRadio.IsChecked = false ✓
// Result: ONLY ONE radio button checked!
```

### Validation Logic

```csharp
// CreateButton_Click validation (order matters!)
if (CopyCurrentRadio.IsChecked == true)  // ← Checked FIRST
{
    SelectedMode = ProfileCreationMode.CopyCurrent;
}
else if (UseDefaultRadio.IsChecked == true)
{
    SelectedMode = ProfileCreationMode.UseDefault;
}
else if (ImportFileRadio.IsChecked == true)  // ← Checked LAST
{
    SelectedMode = ProfileCreationMode.ImportFile;
}
```

**Why order matters**: If multiple radio buttons are checked (bug before fix), the first `if` will win. With GroupName fix, only ONE can be checked at a time.

## ✅ Build Status

```
Build: SUCCESS
Errors: 0
Warnings: 10 (unrelated: CS0168, CS1998)
Output: ProSheetsAddin.dll → bin\Debug\
```

## 📁 Files Changed

- ✅ `Views/ProfileNameDialog.xaml` - Added `GroupName="ProfileMode"` to 3 radio buttons

---
**Status**: ✅ FIXED  
**Date**: 2025-10-03  
**Issue**: Radio button exclusivity not working programmatically  
**Solution**: Added GroupName attribute for proper WPF grouping
