# IFC Settings Browse Button Issue

## Problem Description

The Browse buttons ("...") in the IFC Settings tab for selecting Property Sets files are currently **non-functional** due to a WPF build system limitation.

## Root Cause

### WPF Build Process
WPF projects compile in two phases:

1. **Temporary Assembly Compilation** (`_CompileTemporaryAssembly`)
   - Validates XAML markup
   - Generates `.g.cs` files containing field declarations for `x:Name` controls
   - Compiles code-behind WITH the XAML but WITHOUT the `.g.cs` file

2. **Final Build**
   - Compiles the actual DLL including `.g.cs` files

### The Circular Dependency
```
XAML declares:
  <Button x:Name="BrowseUserPsetsButtonIFC" ... />
  <TextBox x:Name="UserPsetsPathTextBoxIFC" ... />

Code-behind references:
  BrowseUserPsetsButtonIFC.Click += Handler;
  UserPsetsPathTextBoxIFC.Text = "...";

BUT:
  - Temp assembly needs to compile code-behind
  - Code-behind references x:Name controls
  - x:Name field declarations are in .g.cs
  - .g.cs is NOT generated until temp validation SUCCEEDS
  - **DEADLOCK**: Need .g.cs to compile, need to compile to generate .g.cs
```

### Build Errors Encountered
```
error CS0103: The name 'UserPsetsPathTextBoxIFC' does not exist in the current context
error CS0103: The name 'BrowseUserPsetsButtonIFC' does not exist in the current context
error CS1061: 'ProSheetsMainWindow' does not contain a definition for 'BrowseButton_Loaded'
```

## Attempted Solutions

### ❌ Approach 1: Direct Click Handlers in XAML
```xaml
<Button x:Name="BrowseBtn" Click="BrowseBtn_Click" />
```
```csharp
private void BrowseBtn_Click(object sender, RoutedEventArgs e) {
    UserPsetsPathTextBoxIFC.Text = ...;  // ERROR: Control doesn't exist yet
}
```
**Result**: Compilation fails during temp assembly validation.

### ❌ Approach 2: Loaded Event with Dynamic Handler Attachment
```xaml
<Button x:Name="BrowseBtn" Loaded="Browse_Loaded" />
```
```csharp
private void Browse_Loaded(object sender, RoutedEventArgs e) {
    button.Click += Handler;  // ERROR: WPF still validates this method exists
}

private void Handler(...) {
    UserPsetsPathTextBoxIFC.Text = ...;  // ERROR: Control doesn't exist yet
}
```
**Result**: Compilation still fails. WPF validates ALL event handler methods in code-behind.

### ❌ Approach 3: Constructor Wire-Up
```csharp
public ProSheetsMainWindow(...) {
    InitializeComponent();
    BrowseBtn.Click += Handler;  // ERROR: Controls don't exist in temp assembly
}
```
**Result**: Same issue - code-behind is compiled in temp assembly without `.g.cs`.

## Working Solutions

### ✅ Solution 1: Comment Out Event Handlers (Current State)
The code for Browse button functionality exists but is **commented out** to allow successful compilation:

```csharp
#region IFC Settings Event Handlers (DISABLED - WPF Build Issue)
/*
private void WireUpIFCBrowseButtons() { ... }
private void BrowseIFCFile_Click(...) { ... }
*/
#endregion
```

**UI State**: Browse buttons are visible but non-functional (no click handlers attached).

### ✅ Solution 2: MVVM Pattern with Commands (Recommended)

Create a ViewModel with `ICommand` properties:

```csharp
// Models/IFCSettingsViewModel.cs
public class IFCSettingsViewModel : INotifyPropertyChanged
{
    private string _userPsetsPath;
    public string UserPsetsPath 
    { 
        get => _userPsetsPath; 
        set { _userPsetsPath = value; OnPropertyChanged(); } 
    }
    
    public ICommand BrowseUserPsetsCommand { get; }
    
    public IFCSettingsViewModel()
    {
        BrowseUserPsetsCommand = new RelayCommand(BrowseUserPsets);
    }
    
    private void BrowseUserPsets()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select User-Defined Property Sets File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() == true)
        {
            UserPsetsPath = dialog.FileName;
        }
    }
}
```

Update XAML:
```xaml
<TextBox Text="{Binding UserPsetsPath}" />
<Button Command="{Binding BrowseUserPsetsCommand}" Content="..." />
```

**Advantages**:
- No code-behind event handlers
- Cleaner separation of concerns
- Testable logic
- No WPF build issues

### ✅ Solution 3: Attached Behavior

Create a reusable Attached Property:

```csharp
// Utils/BrowseBehavior.cs
public static class BrowseBehavior
{
    public static readonly DependencyProperty TargetTextBoxProperty =
        DependencyProperty.RegisterAttached(
            "TargetTextBox",
            typeof(TextBox),
            typeof(BrowseBehavior),
            new PropertyMetadata(null, OnTargetTextBoxChanged));
    
    public static void SetTargetTextBox(Button button, TextBox value)
    {
        button.SetValue(TargetTextBoxProperty, value);
    }
    
    public static TextBox GetTargetTextBox(Button button)
    {
        return (TextBox)button.GetValue(TargetTextBoxProperty);
    }
    
    private static void OnTargetTextBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button button && e.NewValue is TextBox textBox)
        {
            button.Click -= BrowseButton_Click;
            button.Click += BrowseButton_Click;
        }
    }
    
    private static void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var targetTextBox = GetTargetTextBox(button);
        
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select IFC Configuration File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() == true && targetTextBox != null)
        {
            targetTextBox.Text = dialog.FileName;
        }
    }
}
```

Update XAML:
```xaml
<TextBox x:Name="UserPsetsPathTextBox" />
<Button Content="..." 
        local:BrowseBehavior.TargetTextBox="{Binding ElementName=UserPsetsPathTextBox}" />
```

**Advantages**:
- No code-behind in MainWindow
- Reusable across multiple windows
- Works around WPF build limitation
- No XAML-to-code-behind validation

### ✅ Solution 4: Post-Build Code Injection

Use IL weaving or code generation after successful compilation:
- Build project without event handlers → Success
- Inject handler code using tools like Fody/Cecil
- Final DLL has working Browse buttons

**Advantages**:
- Cleanest separation
- No runtime overhead
- Works around WPF limitation

**Disadvantages**:
- Complex build pipeline
- Harder to debug

## Recommended Next Steps

1. **Immediate**: Implement **Solution 2 (MVVM Commands)**
   - Create `IFCSettingsViewModel` class
   - Implement `RelayCommand` helper
   - Update XAML bindings
   - Remove commented code

2. **Alternative**: Implement **Solution 3 (Attached Behavior)**
   - Create `BrowseBehavior` attached property
   - Update XAML with behavior attachment
   - Test in Revit

3. **Document**: Update README.md with architecture decision

## Current Code Location

All Browse button implementation code is located in:

**File**: `Views/ProSheetsMainWindow.xaml.cs`
**Region**: `#region IFC Settings Event Handlers (DISABLED - WPF Build Issue)` (lines ~2703-2801)
**Status**: Commented out with `/* ... */`

**XAML**: `Views/ProSheetsMainWindow.xaml` (lines 1227-1248)
- Buttons have `Tag` property set ("UserPsets", "ParamMapping")
- No event handlers attached
- IsEnabled binding works correctly

## References

- [WPF Temporary Assembly Issue (Stack Overflow)](https://stackoverflow.com/questions/47747941/wpf-temporary-assembly-compilation)
- [MVVM Pattern in WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/commanding-overview)
- [WPF Attached Behaviors](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/attached-properties-overview)
