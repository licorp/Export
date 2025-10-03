# ⚠️ CRITICAL FIX: Radio Button GroupName

## Problem
```
User selected XML file but mode detected as "CopyCurrent" instead of "ImportFile"
```

## Root Cause
Radio buttons had **NO GroupName** → When setting `ImportFileRadio.IsChecked = true` in code, WPF didn't auto-uncheck other radio buttons → Multiple buttons checked at same time!

## Solution
```xaml
<!-- BEFORE ❌ -->
<RadioButton x:Name="CopyCurrentRadio" ... />
<RadioButton x:Name="UseDefaultRadio" ... />
<RadioButton x:Name="ImportFileRadio" ... />

<!-- AFTER ✅ -->
<RadioButton x:Name="CopyCurrentRadio" ... GroupName="ProfileMode"/>
<RadioButton x:Name="UseDefaultRadio" ... GroupName="ProfileMode"/>
<RadioButton x:Name="ImportFileRadio" ... GroupName="ProfileMode"/>
```

## Expected Result
```
[ProSheets ProfileDialog] Mode selected: ImportFile, Profile name: 'crr_1'  ✅
[Export +] Creating new profile: crr_1, Mode: ImportFile  ✅
[Export +] === IMPORTING XML PROFILE SETTINGS ===
[Export +] === APPLYING SETTINGS TO UI ===
```

## Test Now
1. Click "Add Profile"
2. Click "..." → Select XML file
3. Click "Create"
4. ✅ Check DebugView for "Mode: ImportFile" (NOT "CopyCurrent")

---
**File**: `Views/ProfileNameDialog.xaml`  
**Build**: ✅ SUCCESS  
**Ready to test**: YES
