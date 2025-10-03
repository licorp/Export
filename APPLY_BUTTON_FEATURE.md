# Apply Button Feature

## 📋 Tổng Quan

Đã thêm nút **"Apply"** vào giao diện Profile để người dùng có thể kiểm soát khi nào áp dụng settings từ profile đã chọn.

## 🎯 Mục Đích

**TRƯỚC ĐÂY**:
- Khi chọn profile khác trong ComboBox → **TỰ ĐỘNG** áp dụng tất cả settings
- Người dùng không có thời gian xem preview hoặc quyết định có muốn áp dụng không

**BÂY GIỜ**:
- Khi chọn profile khác → **KHÔNG** tự động áp dụng
- Nút **Apply** được enable
- Người dùng nhấn **Apply** → Áp dụng settings từ profile đã chọn

## 🎨 Giao Diện

### UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Profile:  [crr_1 ▼]  [Apply]  [+]  [💾]  [🗑]               │
└─────────────────────────────────────────────────────────────┘
```

**Vị trí các nút**:
1. **Apply** (70px width) - Primary button style (màu xanh nổi bật)
2. **+** (32px) - Tạo profile mới
3. **💾** (32px) - Save settings vào profile hiện tại
4. **🗑** (32px) - Xóa profile

### States

| Trạng thái | Apply Button | Mô tả |
|-----------|-------------|-------|
| **Initial Load** | Disabled | Chưa chọn profile khác |
| **Profile Selected** | **Enabled** | Đã chọn profile khác, chờ apply |
| **After Apply** | Disabled | Đã áp dụng xong |

## 🔧 Chi Tiết Kỹ Thuật

### 1. XAML Changes

**File**: `Views/ProSheetsMainWindow.xaml`

```xaml
<!-- Apply Button (NEW) -->
<Button x:Name="ApplyProfileButton" 
        Width="70" Height="32" 
        Margin="0,0,10,0"
        Style="{StaticResource PrimaryButtonStyle}"
        ToolTip="Apply selected profile settings"
        Click="ApplyProfile_Click"
        IsEnabled="False">
    <TextBlock Text="Apply" FontSize="13" FontWeight="SemiBold"/>
</Button>
```

**Properties**:
- `IsEnabled="False"` - Ban đầu disabled
- `Style="PrimaryButtonStyle"` - Màu xanh nổi bật (khác các nút phụ)
- `Width="70"` - Rộng hơn các nút icon

### 2. Code-Behind Changes

**File**: `Views/ProSheetsMainWindow.Profiles.cs`

#### A. ProfileComboBox_SelectionChanged

**TRƯỚC**:
```csharp
private void ProfileComboBox_SelectionChanged(...)
{
    if (ProfileComboBox.SelectedItem is Profile selectedProfile)
    {
        _selectedProfile = selectedProfile;
        _profileManager.SwitchProfile(selectedProfile); // ❌ TỰ ĐỘNG APPLY!
    }
}
```

**SAU**:
```csharp
private void ProfileComboBox_SelectionChanged(...)
{
    if (ProfileComboBox.SelectedItem is Profile selectedProfile)
    {
        _selectedProfile = selectedProfile;
        
        // ✅ Enable Apply button, chờ user click
        if (ApplyProfileButton != null)
        {
            ApplyProfileButton.IsEnabled = true;
        }
        
        WriteDebugLog($"Profile '{selectedProfile.Name}' selected. Click Apply to load.");
    }
}
```

#### B. ApplyProfile_Click (NEW)

```csharp
private void ApplyProfile_Click(object sender, RoutedEventArgs e)
{
    if (ProfileComboBox.SelectedItem is Profile selectedProfile)
    {
        try
        {
            // Switch profile (triggers ProfileChanged event)
            _profileManager.SwitchProfile(selectedProfile);
            
            // Disable Apply button after success
            ApplyProfileButton.IsEnabled = false;
            
            // Show notification
            MessageBox.Show(
                $"Profile '{selectedProfile.Name}' has been applied.",
                "Profile Applied",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WriteDebugLog($"ERROR: {ex.Message}");
            MessageBox.Show(
                $"Failed to apply profile: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
```

### 3. Event Flow

```
┌──────────────────────┐
│ User chọn profile    │
│ trong ComboBox       │
└──────────┬───────────┘
           │
           v
┌──────────────────────────────────┐
│ ProfileComboBox_SelectionChanged │
│ - Lưu _selectedProfile           │
│ - Enable Apply button            │
│ - KHÔNG apply settings           │
└──────────┬───────────────────────┘
           │
           │ (User nhấn Apply)
           v
┌──────────────────────────────┐
│ ApplyProfile_Click           │
│ - Call SwitchProfile()       │
│ - Trigger ProfileChanged evt │
└──────────┬───────────────────┘
           │
           v
┌──────────────────────────────┐
│ OnProfileChanged (event)     │
│ - Call ApplyProfileToUI()    │
│ - Load all settings to UI    │
└──────────┬───────────────────┘
           │
           v
┌──────────────────────────────┐
│ ApplyProfileToUI             │
│ - OutputFolder               │
│ - Export formats (PDF, DWG)  │
│ - Hide settings              │
│ - Custom file names          │
└──────────────────────────────┘
```

## 📖 User Workflow

### Scenario 1: Chọn và Apply Profile

```
1. User mở dialog → ComboBox hiển thị "Default" profile
   └─ Apply button: [DISABLED]

2. User chọn "crr_1" trong ComboBox
   └─ Apply button: [ENABLED] (màu xanh sáng)
   └─ Settings CHƯA thay đổi (vẫn giữ current)

3. User nhấn "Apply"
   └─ Settings từ "crr_1" được load vào UI
   └─ MessageBox: "Profile 'crr_1' has been applied successfully."
   └─ Apply button: [DISABLED]
   └─ Custom file names: "P100-PLAN-Checker-Designer"
```

### Scenario 2: Chọn Nhưng Không Apply

```
1. User chọn "Profile A" → Apply button [ENABLED]

2. User đổi ý, chọn "Profile B" → Apply button vẫn [ENABLED]

3. User không nhấn Apply, đóng dialog
   └─ Settings KHÔNG thay đổi (vẫn dùng profile cũ)
```

### Scenario 3: Test Nhiều Profiles

```
1. Chọn "crr_1" → Apply
   └─ Custom names: "P100-A-PLAN NAME"

2. Chọn "123test" → Apply
   └─ Custom names: "P100-PLAN-Checker-Designer"

3. So sánh kết quả trong UI
```

## ✅ Benefits

### 1. **User Control**
- Người dùng quyết định KHI NÀO áp dụng settings
- Không bị thay đổi settings đột ngột

### 2. **Preview Capability**
- Có thể xem thông tin profile trước khi apply
- Kiểm tra "Profile created on 2025-10-03"

### 3. **Error Recovery**
- Nếu Apply lỗi → MessageBox thông báo
- Settings không thay đổi nếu có lỗi

### 4. **Better UX**
- Nút Apply màu xanh nổi bật
- Disabled khi không cần thiết
- Tooltip rõ ràng: "Apply selected profile settings"

## 🎯 Testing Checklist

- [ ] Load dialog → Apply button disabled
- [ ] Chọn profile khác → Apply button enabled
- [ ] Nhấn Apply → Settings được load
- [ ] Nhấn Apply → MessageBox hiện "Profile applied successfully"
- [ ] Sau Apply → Apply button disabled
- [ ] Custom file names thay đổi theo profile
- [ ] Chọn profile 1 → Apply → Chọn profile 2 → Apply (switch nhanh)
- [ ] Error handling: Apply profile không tồn tại

## 📊 Debug Logs

Khi Apply profile, debug log sẽ hiển thị:

```
[Export +] Profile selected: crr_1
[Export +] Profile 'crr_1' selected. Click Apply to load settings.
[Export +] Apply Profile clicked
[Export +] Applying profile: crr_1
[Export +] Switched to profile: crr_1
[Export +] Profile changed event: crr_1
[Export +] Applying profile 'crr_1' to UI
[Export +] === APPLYING CUSTOM FILE NAME PARAMETERS FROM XML ===
[Export +] Found 4 combine parameters in XML
[Export +]   Param 1: 'Sheet Number' = 'P100', separator: '-'
[Export +]   Param 2: 'Sheet Name' = 'PLAN', separator: '-'
[Export +]   Param 3: 'Checked By' = 'Checker', separator: '-'
[Export +]   Param 4: 'Designed By' = 'Designer' (last, no separator)
[Export +] Applied custom name to sheet P100: 'P100-PLAN-Checker-Designer'
[Export +] Custom file names applied to 27/27 sheets
[Export +] Profile 'crr_1' applied successfully
```

## 🔄 Comparison: Before vs After

| Aspect | Before | After |
|--------|--------|-------|
| **Profile Selection** | Auto-apply immediately | Select only, wait for Apply |
| **User Control** | ❌ No control | ✅ Full control |
| **Apply Button** | ❌ Not exist | ✅ Exists, state-aware |
| **Error Handling** | Silent fail | MessageBox alert |
| **UX Flow** | 1 step (select) | 2 steps (select → apply) |
| **Flexibility** | Low | High |

## 💡 Future Enhancements

1. **Preview Panel**: Hiển thị settings của profile trước khi apply
2. **Confirmation Dialog**: "Are you sure?" nếu có unsaved changes
3. **Undo Button**: Quay lại profile trước đó
4. **Auto-Save**: Tự động save current khi switch profile
5. **Profile Comparison**: So sánh 2 profiles side-by-side

## 📝 Related Files

- `Views/ProSheetsMainWindow.xaml` - UI definition (Apply button)
- `Views/ProSheetsMainWindow.Profiles.cs` - Event handlers
- `Managers/ProfileManagerService.cs` - Profile management logic
- `Models/ProSheetsModels.cs` - Profile data models

## ✨ Summary

Nút **Apply** giúp người dùng:
- ✅ **Kiểm soát** khi nào settings được áp dụng
- ✅ **Xem trước** thông tin profile
- ✅ **Tránh lỗi** do auto-apply không mong muốn
- ✅ **Linh hoạt** chọn nhiều profiles mà không lo settings bị thay đổi

**User-friendly workflow**: SELECT → REVIEW → APPLY → CONFIRM ✅
