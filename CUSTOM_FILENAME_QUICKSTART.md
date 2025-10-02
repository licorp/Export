# 🚀 Custom File Name Dialog - Quick Start Guide

## 📌 TL;DR (Too Long; Didn't Read)

**What**: Dialog to create custom file names from Revit parameters  
**Where**: Click "Custom File Name" column header in ProSheets  
**How**: Select parameters → Configure prefix/suffix → Apply  

---

## ⚡ Quick Usage (3 Steps)

### 1️⃣ **Select Sheets/Views**
```
✅ Check boxes next to sheets you want to rename
```

### 2️⃣ **Open Dialog**
```
Click: "Custom File Name" column header
```

### 3️⃣ **Configure & Apply**
```
1. Double-click parameter to add (e.g., "Sheet Number")
2. Edit Prefix/Suffix if needed
3. Check Preview
4. Click OK
```

---

## 🎨 UI Overview

```
┌──────────────────────────────────────────────────┐
│ DiRoots                          [—] [✕]        │
├──────────────────────────────────────────────────┤
│                                                  │
│  ┌─ Available ────┐  ┌─ Configuration ────────┐│
│  │  Parameters    │  │ Param | Pre | Val | Suf││
│  │                │  │───────┼─────┼─────┼────││
│  │ 🔍 Search...   │  │ Sheet │     │ A101│    ││
│  │                │  │ Number│     │     │    ││
│  │ • Sheet Number │  │───────┼─────┼─────┼────││
│  │ • Sheet Name   │  │ Sheet │     │Floor│    ││
│  │ • Revision     │  │ Name  │     │Plan │    ││
│  │ ...            │  └────────────────────────┘│
│  └────────────────┘                             │
│                    [↑] [↓] [+] [-] [⟳]         │
│                                                  │
│  ┌─ Preview: ─────────────────────────────────┐│
│  │  A101-Floor Plan                           ││
│  └────────────────────────────────────────────┘│
│                                                  │
│                       [Cancel]  [OK]            │
└──────────────────────────────────────────────────┘
```

---

## 🎯 Common Use Cases

### Default Template
```
Sheet Number - Current Revision - Sheet Name
→ A101-Rev A-Floor Plan
```

### Project-Based
```
Project Number - Sheet Number - Date
→ 2025-001-A101-2025-10-02
```

### Discipline Prefix
```
Sheet Number - Drawing Name
→ A101-Floor_Plan
```

---

## ⌨️ Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Search parameters | Type in search box |
| Add parameter | Double-click in list |
| Remove parameter | Select + click [-] |
| Move up | Select + click [↑] |
| Move down | Select + click [↓] |
| Apply | Enter or click OK |
| Cancel | Esc or click Cancel |

---

## 🎛️ Control Buttons

| Button | Icon | Function |
|--------|------|----------|
| Move Up | ↑ | Reorder parameter up |
| Move Down | ↓ | Reorder parameter down |
| Add | + | Add selected parameter |
| Remove | - | Remove selected parameter |
| Refresh | ⟳ | Reload parameters list |

---

## 📝 Configuration Options

### Prefix
- **Purpose**: Text BEFORE parameter value
- **Example**: `Prefix="REV"` → `REVA101`

### Suffix
- **Purpose**: Text AFTER parameter value
- **Example**: `Suffix="_v1"` → `A101_v1`

### Separator
- **Purpose**: Character between parameters
- **Default**: `-` (dash)
- **Options**: `-`, `_`, ` ` (space), custom

---

## ✅ Best Practices

### DO ✅
- ✅ Use consistent separators (`-` or `_`)
- ✅ Test with preview before applying
- ✅ Keep filenames under 100 characters
- ✅ Avoid special characters: `\ / : * ? " < > |`

### DON'T ❌
- ❌ Use too many parameters (max 3-4)
- ❌ Leave empty parameters (will be skipped)
- ❌ Use spaces if uploading to web systems
- ❌ Forget to check preview

---

## 🔥 Pro Tips

1. **Search Faster**: Type first few letters to filter parameters
2. **Reorder Smart**: Most important parameter first
3. **Use Separator**: `-` works best for most systems
4. **Preview First**: Always check before clicking OK
5. **Save Time**: Same config applies to all selected sheets

---

## 🐛 Troubleshooting

### Issue: Dialog won't open
**Fix**: Select at least one sheet/view first

### Issue: Parameters empty
**Fix**: Click Refresh button (⟳)

### Issue: Preview looks wrong
**Fix**: Check Prefix/Suffix/Separator settings

### Issue: Filename too long
**Fix**: Remove parameters or shorten prefix/suffix

---

## 📚 Parameter Categories

| Category | Examples |
|----------|----------|
| Identity Data | Sheet Number, Sheet Name, Revision |
| Project Info | Project Name, Client Name, Date |
| Drawing | Scale, Drawn By, Checked By |
| IFC | IFC Building GUID, IFC Project GUID |
| Custom | Your project parameters |

---

## 💡 Example Configurations

### Configuration 1: Standard
```yaml
Parameters:
  - Sheet Number    [Prefix: "" | Suffix: "" | Sep: "-"]
  - Sheet Name      [Prefix: "" | Suffix: "" | Sep: ""]
Result: A101-Floor Plan
```

### Configuration 2: Revision Tracking
```yaml
Parameters:
  - Sheet Number       [Prefix: "" | Suffix: "" | Sep: "_"]
  - Current Revision   [Prefix: "Rev" | Suffix: "" | Sep: "_"]
  - Sheet Issue Date   [Prefix: "" | Suffix: "" | Sep: ""]
Result: A101_RevA_2025-10-02
```

### Configuration 3: Project Filing
```yaml
Parameters:
  - Project Number  [Prefix: "" | Suffix: "" | Sep: "-"]
  - Sheet Number    [Prefix: "" | Suffix: "" | Sep: "-"]
  - Drawn By        [Prefix: "" | Suffix: "" | Sep: ""]
Result: 2025-001-A101-JDoe
```

---

## 🎬 Video Tutorial (Steps)

1. **Launch**: Open ProSheets → Select Sheets tab
2. **Select**: Check boxes for sheets to rename
3. **Click**: "Custom File Name" column header
4. **Add**: Double-click "Sheet Number" in left list
5. **Add**: Double-click "Sheet Name"
6. **Preview**: Check bottom preview box
7. **Apply**: Click OK button
8. **Verify**: Check "Custom File Name" column updated

---

## 📊 Success Metrics

After using Custom File Name Dialog:
- ✅ All sheets have consistent naming
- ✅ Filenames follow company standards
- ✅ Export creates properly named files
- ✅ Easy to search/sort exported files

---

## 🆘 Need Help?

1. **Documentation**: See [CUSTOM_FILENAME_DIALOG.md](CUSTOM_FILENAME_DIALOG.md)
2. **Full Guide**: See [CUSTOM_FILENAME_IMPLEMENTATION.md](CUSTOM_FILENAME_IMPLEMENTATION.md)
3. **Main README**: See [README.md](README.md)

---

## 🎉 You're Ready!

**That's it!** You can now create professional, consistent file names for all your sheets and views.

**Questions?** Check the full documentation or contact support.

---

**Version**: 1.0  
**Date**: October 2, 2025  
**Revit**: 2023-2026
