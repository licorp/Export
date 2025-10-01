using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;
using ProSheetsAddin.Models;
using ProSheetsAddin.Managers;

namespace ProSheetsAddin.Views
{
    /// <summary>
    /// Export + - Professional Style Interface
    /// </summary>
    public partial class ProSheetsMainWindow : Window, INotifyPropertyChanged
    {
        private readonly Document _document;
        private ObservableCollection<SheetItem> _sheets;
        private ProSheetsProfileManager _profileManager;
        private ProSheetsProfile _selectedProfile;

        // Enhanced properties for data binding
        public int SelectedSheetsCount 
        { 
            get 
            { 
                return Sheets?.Count(s => s.IsSelected) ?? 0; 
            } 
        }

        public int SelectedViewsCount 
        { 
            get 
            { 
                return Views?.Count(v => v.IsSelected) ?? 0; 
            } 
        }
        
        public ObservableCollection<SheetItem> SheetItems => Sheets;
        
        // New property for XAML binding
        public ObservableCollection<SheetItem> Sheets 
        { 
            get => _sheets; 
            set 
            {
                _sheets = value;
                OnPropertyChanged(nameof(Sheets));
                OnPropertyChanged(nameof(SelectedSheetsCount));
            }
        }

        private ObservableCollection<ViewItem> _views;
        public ObservableCollection<ViewItem> Views 
        { 
            get => _views; 
            set 
            {
                _views = value;
                OnPropertyChanged(nameof(Views));
                OnPropertyChanged(nameof(SelectedViewsCount));
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            WriteDebugLog($"Property '{propertyName}' changed - firing PropertyChanged event");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Profile Manager properties
        public ObservableCollection<ProSheetsProfile> Profiles => _profileManager?.Profiles;
        
        public ProSheetsProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnProfileChanged();
                }
            }
        }

        // Export settings với data binding
        public ExportSettings ExportSettings { get; set; }
        
        public ProSheetsMainWindow(Document document)
        {
            WriteDebugLog("===== EXPORT + CONSTRUCTOR STARTED =====");
            WriteDebugLog($"Document: {document?.Title ?? "NULL"}");
            
            _document = document;
            
            // Initialize export settings with data binding
            ExportSettings = new ExportSettings();
            WriteDebugLog("ExportSettings initialized");
            
            InitializeComponent();
            WriteDebugLog("InitializeComponent completed");
            
            // Set DataContext for binding - should point to this window, not ExportSettings
            this.DataContext = this;
            WriteDebugLog("DataContext set to this window");
            
            InitializeProfiles();
            LoadSheets();
            LoadViews();
            UpdateFormatSelection();
            
            WriteDebugLog("===== EXPORT + CONSTRUCTOR COMPLETED SUCCESSFULLY =====");
        }

        // DllImport for OutputDebugStringA to work with DebugView
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        private static extern void OutputDebugStringA(string lpOutputString);

        /// <summary>
        /// Enhanced debug logging method compatible with DebugView
        /// </summary>
        private void WriteDebugLog(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string fullMessage = $"[Export +] {timestamp} - {message}";
                
                // Primary output for DebugView compatibility
                OutputDebugStringA(fullMessage + "\r\n");
                
                // Secondary outputs for development
                System.Diagnostics.Debug.WriteLine(fullMessage);
                Console.WriteLine(fullMessage);
                System.Diagnostics.Trace.WriteLine(fullMessage);
            }
            catch (Exception ex)
            {
                // Fallback logging
                System.Diagnostics.Debug.WriteLine($"[Export +] Logging error: {ex.Message}");
                OutputDebugStringA($"[Export +] Logging error: {ex.Message}\r\n");
            }
        }

        private void LoadSheets()
        {
            WriteDebugLog("LoadSheets started");
            
            try
            {
                var newSheets = new ObservableCollection<SheetItem>();
                WriteDebugLog("ObservableCollection<SheetItem> created");
                
                var collector = new FilteredElementCollector(_document)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(sheet => !sheet.IsTemplate);
                
                int sheetCount = collector.Count();
                WriteDebugLog($"Found {sheetCount} non-template sheets in document");
                
                int addedCount = 0;
                foreach (var sheet in collector)
                {
                    // Get sheet revision
                    string revision = "";
                    try
                    {
                        Parameter revParam = sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION);
                        revision = revParam?.AsString() ?? "";
                    }
                    catch { revision = ""; }

                    // Get sheet size
                    string sheetSize = "";
                    try 
                    {
                        Parameter sizeParam = sheet.get_Parameter(BuiltInParameter.SHEET_HEIGHT);
                        if (sizeParam != null)
                        {
                            double height = sizeParam.AsDouble();
                            Parameter widthParam = sheet.get_Parameter(BuiltInParameter.SHEET_WIDTH);
                            double width = widthParam?.AsDouble() ?? 0;
                            
                            // Convert from feet to mm (Revit internal units are feet)
                            height = height * 304.8;
                            width = width * 304.8;
                            
                            // Determine standard paper size
                            if (Math.Abs(height - 297) < 10 && Math.Abs(width - 210) < 10) sheetSize = "A4";
                            else if (Math.Abs(height - 420) < 10 && Math.Abs(width - 297) < 10) sheetSize = "A3";
                            else if (Math.Abs(height - 594) < 10 && Math.Abs(width - 420) < 10) sheetSize = "A2";
                            else if (Math.Abs(height - 841) < 10 && Math.Abs(width - 594) < 10) sheetSize = "A1";
                            else if (Math.Abs(height - 1189) < 10 && Math.Abs(width - 841) < 10) sheetSize = "A0";
                            else sheetSize = $"{width:0}x{height:0}";
                        }
                    }
                    catch { sheetSize = "Unknown"; }
                    
                    var sheetItem = new SheetItem
                    {
                        IsSelected = false,
                        SheetNumber = sheet.SheetNumber ?? "NO_NUMBER",
                        SheetName = sheet.Name ?? "NO_NAME",
                        Revision = revision,
                        Size = sheetSize,
                        CustomFileName = $"{sheet.SheetNumber ?? "UNKNOWN"}_{(sheet.Name ?? "UNKNOWN").Replace(" ", "_")}"
                    };
                    
                    // Subscribe to PropertyChanged to track selection changes
                    sheetItem.PropertyChanged += (s, e) => 
                    {
                        if (e.PropertyName == "IsSelected")
                        {
                            WriteDebugLog($"Sheet selection changed: {sheetItem.Number} -> {sheetItem.IsSelected}");
                            UpdateStatusText();
                            UpdateExportSummary();
                        }
                    };
                    
                    newSheets.Add(sheetItem);
                    addedCount++;
                    WriteDebugLog($"Added sheet #{addedCount}: {sheet.SheetNumber} - {sheet.Name}");
                }
                
                WriteDebugLog($"Sheet collection ready with {newSheets.Count} items");
                
                // Set the Sheets property to trigger data binding
                Sheets = newSheets;
                WriteDebugLog("Sheets property updated - DataBinding should refresh");
                
                UpdateStatusText();
                UpdateExportSummary();
                WriteDebugLog($"LoadSheets completed successfully - Total sheets loaded: {Sheets?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"CRITICAL ERROR in LoadSheets: {ex.Message}");
                WriteDebugLog($"StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error loading sheets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadViews()
        {
            WriteDebugLog("LoadViews started");
            try
            {
                var newViews = new ObservableCollection<ViewItem>();
                WriteDebugLog("ObservableCollection<ViewItem> created");

                // Get all views from document (excluding sheets and templates)
                var collector = new FilteredElementCollector(_document)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate && 
                               v.ViewType != ViewType.DrawingSheet &&
                               v.ViewType != ViewType.ProjectBrowser &&
                               v.ViewType != ViewType.SystemBrowser &&
                               v.CanBePrinted)
                    .OrderBy(v => v.ViewType.ToString())
                    .ThenBy(v => v.Name);

                WriteDebugLog($"Found {collector.Count()} printable views in document");

                int addedCount = 0;
                foreach (var view in collector)
                {
                    var viewItem = new ViewItem(view);
                    
                    // Subscribe to PropertyChanged to track selection changes
                    viewItem.PropertyChanged += (s, e) => 
                    {
                        if (e.PropertyName == "IsSelected")
                        {
                            WriteDebugLog($"View selection changed: {viewItem.ViewName} -> {viewItem.IsSelected}");
                            UpdateViewStatusText();
                        }
                    };
                    
                    newViews.Add(viewItem);
                    addedCount++;
                    WriteDebugLog($"Added view #{addedCount}: {view.ViewType} - {view.Name}");
                }
                
                WriteDebugLog($"View collection ready with {newViews.Count} items");
                
                // Set the Views property to trigger data binding
                Views = newViews;
                WriteDebugLog("Views property updated - DataBinding should refresh");
                
                UpdateViewStatusText();
                WriteDebugLog($"LoadViews completed successfully - Total views loaded: {Views?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"CRITICAL ERROR in LoadViews: {ex.Message}");
                WriteDebugLog($"StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error loading views: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateViewStatusText()
        {
            var selectedCount = Views?.Count(v => v.IsSelected) ?? 0;
            var totalCount = Views?.Count ?? 0;
            WriteDebugLog($"[Export +] UpdateViewStatusText called - Selected: {selectedCount}, Total: {totalCount}");
        }

        private void UpdateStatusText()
        {
            var selectedCount = Sheets?.Count(s => s.IsSelected) ?? 0;
            var totalCount = Sheets?.Count ?? 0;
            WriteDebugLog($"[Export +] UpdateStatusText called - Selected: {selectedCount}, Total: {totalCount}");
            
            // Update window title to show status
            try
            {
                this.Title = $"Export + - {selectedCount} of {totalCount} sheets selected";
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error updating window title: {ex.Message}");
            }
        }

        private void UpdateFormatSelection()
        {
            WriteDebugLog("[Export +] UpdateFormatSelection called");
            
            try
            {
                WriteDebugLog($"[Export +] Current format states: {string.Join(", ", ExportSettings?.GetSelectedFormatsList() ?? new List<string>())}");
                
                // Format selection is handled by data binding in XAML
                WriteDebugLog("[Export +] Format selection updated via data binding");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export +] ERROR in UpdateFormatSelection: {ex.Message}");
            }
        }

        private void UpdateExportSummary()
        {
            try
            {
                var selectedCount = SelectedSheetsCount;
                var selectedFormats = ExportSettings?.GetSelectedFormatsList() ?? new List<string>();
                var estimatedFiles = selectedCount * selectedFormats.Count;

                // Update export settings with current selection count
                if (ExportSettings != null)
                {
                    ExportSettings.SelectedSheetsCount = selectedCount;
                }

                WriteDebugLog($"[Export +] Export summary updated: {selectedCount} sheets, {selectedFormats.Count} formats, {estimatedFiles} files");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export +] ERROR in UpdateExportSummary: {ex.Message}");
            }
        }

        // Event Handlers for Export + Interface - moved to Profile Manager Methods region

        private void ToggleAll_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[Export +] Toggle All clicked");
            
            if (Sheets != null && Sheets.Any())
            {
                bool selectAll = !Sheets.All(s => s.IsSelected);
                foreach (var sheet in Sheets)
                {
                    sheet.IsSelected = selectAll;
                }
                WriteDebugLog($"[Export +] Toggled all sheets to: {selectAll}");
                UpdateStatusText();
                UpdateExportSummary();
            }
        }

        private void EditCustomDrawingNumber_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[Export +] Edit Custom Drawing Number clicked");
            MessageBox.Show("Custom Drawing Number Editor sẽ được thêm trong phiên bản tiếp theo!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FormatToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Tag is string format)
            {
                WriteDebugLog($"[Export +] Format {format} checked via ToggleButton");
                ExportSettings?.SetFormatSelection(format, true);
                UpdateExportSummary();
            }
        }

        private void FormatToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Tag is string format)
            {
                WriteDebugLog($"[Export +] Format {format} unchecked via ToggleButton");
                ExportSettings?.SetFormatSelection(format, false);
                UpdateExportSummary();
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[Export +] Browse folder clicked - Export + Enhanced version");
            
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            
            if (!string.IsNullOrEmpty(ExportSettings?.OutputFolder))
            {
                dialog.SelectedPath = ExportSettings.OutputFolder;
                WriteDebugLog($"[Export +] Current folder: {ExportSettings.OutputFolder}");
            }
            
            dialog.Description = "Chọn thư mục xuất file Export +";
            dialog.ShowNewFolderButton = true;
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExportSettings.OutputFolder = dialog.SelectedPath;
                WriteDebugLog($"[Export +] Folder updated: {dialog.SelectedPath}");
                UpdateExportSummary();
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("===== CREATE BUTTON CLICKED (EXPORT + ENHANCED) =====");
            
            var selectedSheets = Sheets?.Where(s => s.IsSelected).ToList();
            WriteDebugLog($"Found {selectedSheets?.Count ?? 0} selected sheets for export");
            
            // Log selected sheet details
            if (selectedSheets != null && selectedSheets.Any())
            {
                foreach (var sheet in selectedSheets)
                {
                    WriteDebugLog($"Selected sheet: {sheet.Number} - {sheet.Name}");
                }
            }
            
            if (selectedSheets == null || !selectedSheets.Any())
            {
                WriteDebugLog("VALIDATION ERROR: No sheets selected for export");
                MessageBox.Show("Vui lòng chọn ít nhất một sheet để export!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var outputPath = ExportSettings?.OutputFolder ?? "";
            WriteDebugLog($"Output path validation: '{outputPath}'");
            
            if (string.IsNullOrEmpty(outputPath))
            {
                WriteDebugLog("VALIDATION ERROR: Empty or null output path");
                MessageBox.Show("Vui lòng chọn thư mục xuất file!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var selectedFormats = ExportSettings?.GetSelectedFormatsList() ?? new List<string>();
            WriteDebugLog($"Selected export formats: [{string.Join(", ", selectedFormats)}]");
            
            if (!selectedFormats.Any())
            {
                WriteDebugLog("VALIDATION ERROR: No export formats selected");
                MessageBox.Show("Vui lòng chọn ít nhất một định dạng file!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Show detailed export summary
            var summary = $@"EXPORT + SUMMARY
            
Sheets: {selectedSheets.Count}
Formats: {string.Join(", ", selectedFormats)}
Output: {outputPath}
Estimated Files: {selectedSheets.Count * selectedFormats.Count}

Template: {ExportSettings?.FileNameTemplate ?? "Default"}
Combine Files: {ExportSettings?.CombineFiles ?? false}
Include Revision: {ExportSettings?.IncludeRevision ?? false}

Tiếp tục xuất file?";
            
            WriteDebugLog($"[Export +] Showing export summary dialog");
            var result = MessageBox.Show(summary, "Export + Confirmation", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                WriteDebugLog("[Export +] User confirmed export - Starting export process...");
                
                try
                {
                    // Update status via window title
                    this.Title = "Export + - Exporting...";

                    bool exportSuccess = false;
                    int totalExported = 0;

                    // Convert SheetItem to ViewSheet for export
                    var sheetsToExport = new List<ViewSheet>();
                    foreach (var sheetItem in selectedSheets)
                    {
                        // Find the actual ViewSheet from document
                        var collector = new FilteredElementCollector(_document);
                        var sheet = collector.OfClass(typeof(ViewSheet))
                                           .Cast<ViewSheet>()
                                           .FirstOrDefault(s => s.SheetNumber == sheetItem.Number);
                        if (sheet != null)
                        {
                            sheetsToExport.Add(sheet);
                        }
                    }

                    WriteDebugLog($"[Export +] Found {sheetsToExport.Count} ViewSheets for export");

                    // Export to different formats
                    foreach (var format in selectedFormats)
                    {
                        WriteDebugLog($"[Export +] Starting export to {format}");
                        
                        if (format.ToUpper() == "PDF")
                        {
                            var pdfManager = new PDFExportManager(_document);
                            bool pdfResult = pdfManager.ExportSheetsToPDF(sheetsToExport, outputPath, ExportSettings);
                            if (pdfResult)
                            {
                                totalExported += sheetsToExport.Count;
                                exportSuccess = true;
                                WriteDebugLog($"[Export +] PDF export completed successfully");
                            }
                        }
                        else
                        {
                            WriteDebugLog($"[Export +] Format {format} not yet implemented");
                        }
                    }
                    
                    WriteDebugLog("[Export +] Export process completed");
                    
                    if (exportSuccess)
                    {
                        MessageBox.Show($"Export completed successfully!\n\nExported: {totalExported} files\nLocation: {outputPath}", 
                            "Export Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Export failed or no files were exported.", 
                            "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                        
                    // Update status via window title
                    this.Title = exportSuccess ? "Export + - Export completed" : "Export + - Export failed";
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"[Export +] ERROR in export process: {ex.Message}");
                    MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Update status via window title
                    this.Title = "Export + - Export failed";
                }
            }
            else
            {
                WriteDebugLog("[Export +] User cancelled export");
            }
        }

        // ViewDebugLog_Click method removed - use DebugView instead to see OutputDebugStringA logs

        // Legacy event handlers for compatibility
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Tab selection changed");
        }

        private void SheetsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WriteDebugLog("[ProSheets] DataGrid mouse button down");
        }

        private void SheetsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            WriteDebugLog("[ProSheets] DataGrid cell edit ending");
        }

        private void SheetsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteDebugLog("[ProSheets] DataGrid selection changed");
            UpdateStatusText();
        }

        // New event handlers for enhanced UI
        private void SheetsRadio_Checked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Sheets radio button checked");
            if (SheetsDataGrid != null && ViewsDataGrid != null)
            {
                SheetsDataGrid.Visibility = System.Windows.Visibility.Visible;
                ViewsDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                LoadSheets(); // Reload sheet data when switching to Sheets view
            }
        }

        private void ViewsRadio_Checked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Views radio button checked");
            if (SheetsDataGrid != null && ViewsDataGrid != null)
            {
                SheetsDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                ViewsDataGrid.Visibility = System.Windows.Visibility.Visible;
                LoadViews(); // Load view data when switching to Views
            }
        }

        private void ViewsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Views DataGrid selection changed");
            UpdateViewStatusText();
        }

        private void ViewSheetSetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo?.SelectedItem is ComboBoxItem item)
            {
                WriteDebugLog($"View/Sheet Set changed to: {item.Content}");
                // TODO: Filter sheets based on selected set
            }
        }

        private void FilterByVSSet_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Filter by View/Sheet Set clicked");
            // TODO: Implement filtering logic
        }

        private void SaveVSSet_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Save View/Sheet Set clicked");
            // TODO: Implement save set logic
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox searchBox = sender as TextBox;
            string searchText = searchBox?.Text?.ToLower() ?? "";
            WriteDebugLog($"Search text changed: '{searchText}'");
            
            // Search filtering will be implemented when DataGrid is properly connected
            WriteDebugLog($"Search functionality available but DataGrid connection pending");
            UpdateStatusText();
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Select All checkbox checked");
            if (Sheets != null)
            {
                foreach (var sheet in Sheets)
                {
                    sheet.IsSelected = true;
                }
                UpdateStatusText();
            }
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Select All checkbox unchecked");
            if (_sheets != null)
            {
                foreach (var sheet in _sheets)
                {
                    sheet.IsSelected = false;
                }
                UpdateStatusText();
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            ToggleAll_Click(sender, e);
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Clear All clicked");
            
            if (_sheets != null)
            {
                foreach (var sheet in _sheets)
                {
                    sheet.IsSelected = false;
                }
                UpdateStatusText();
                UpdateExportSummary();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Refresh clicked");
            LoadSheets();
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Setting changed");
            UpdateExportSummary();
        }

        private void FormatCheck_Changed(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Format check changed");
            UpdateExportSummary();
        }

        #region Profile Manager Methods

        private void InitializeProfiles()
        {
            WriteDebugLog("Initializing Profile Manager");
            try
            {
                _profileManager = new ProSheetsProfileManager();
                
                // Select first profile by default
                if (_profileManager.Profiles.Count > 0)
                {
                    _selectedProfile = _profileManager.Profiles[0];
                    ApplyProfileToSettings(_selectedProfile);
                }
                
                WriteDebugLog($"Profile Manager initialized with {_profileManager.Profiles.Count} profiles");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error initializing Profile Manager: {ex.Message}");
            }
        }

        private void OnProfileChanged()
        {
            WriteDebugLog($"Profile changed to: {_selectedProfile?.ProfileName ?? "null"}");
            if (_selectedProfile != null)
            {
                ApplyProfileToSettings(_selectedProfile);
            }
        }

        private void ApplyProfileToSettings(ProSheetsProfile profile)
        {
            WriteDebugLog($"Applying profile settings: {profile.ProfileName}");
            try
            {
                if (ExportSettings != null)
                {
                    ExportSettings.OutputFolder = profile.OutputFolder;
                    ExportSettings.CreateSeparateFolders = profile.CreateSeparateFolders;
                    ExportSettings.HideCropBoundaries = profile.HideCropRegions;
                    ExportSettings.HideScopeBoxes = profile.HideScopeBoxes;
                    
                    // Apply format selections
                    ExportSettings.IsPdfSelected = profile.SelectedFormats.Contains("PDF");
                    ExportSettings.IsDwgSelected = profile.SelectedFormats.Contains("DWG");
                    ExportSettings.IsDgnSelected = profile.SelectedFormats.Contains("DGN");
                    ExportSettings.IsIfcSelected = profile.SelectedFormats.Contains("IFC");
                    ExportSettings.IsImgSelected = profile.SelectedFormats.Contains("IMG");
                    
                    WriteDebugLog($"Profile settings applied successfully");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error applying profile settings: {ex.Message}");
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo?.SelectedItem is ProSheetsProfile profile)
            {
                _selectedProfile = profile;
                OnProfileChanged();
                WriteDebugLog($"Profile selection changed to: {profile.ProfileName}");
            }
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Add Profile clicked");
            try
            {
                // Simple dialog for profile name
                string profileName = "New Profile " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                if (!string.IsNullOrEmpty(profileName))
                {
                    var newProfile = _profileManager.CreateProfileFromSettings(ExportSettings, profileName);
                    _profileManager.SaveProfile(newProfile);
                    
                    // Set as current profile
                    _selectedProfile = newProfile;
                    WriteDebugLog($"New profile created: {profileName}");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error creating new profile: {ex.Message}");
                MessageBox.Show($"Lỗi tạo profile: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Save Profile clicked");
            try
            {
                if (_selectedProfile != null)
                {
                    // Update selected profile with current settings
                    _selectedProfile.OutputFolder = ExportSettings.OutputFolder;
                    _selectedProfile.CreateSeparateFolders = ExportSettings.CreateSeparateFolders;
                    _selectedProfile.HideCropRegions = ExportSettings.HideCropBoundaries;
                    _selectedProfile.HideScopeBoxes = ExportSettings.HideScopeBoxes;
                    
                    var formats = new List<string>();
                    if (ExportSettings.IsPdfSelected) formats.Add("PDF");
                    if (ExportSettings.IsDwgSelected) formats.Add("DWG");
                    if (ExportSettings.IsDgnSelected) formats.Add("DGN");
                    if (ExportSettings.IsIfcSelected) formats.Add("IFC");
                    if (ExportSettings.IsImgSelected) formats.Add("IMG");
                    _selectedProfile.SelectedFormats = formats;
                    
                    _profileManager.SaveProfile(_selectedProfile);
                    WriteDebugLog($"Profile saved: {_selectedProfile.ProfileName}");
                    
                    MessageBox.Show($"Profile '{_selectedProfile.ProfileName}' đã được lưu thành công!", 
                                   "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error saving profile: {ex.Message}");
                MessageBox.Show($"Lỗi lưu profile: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Delete Profile clicked");
            try
            {
                if (_selectedProfile != null)
                {
                    var result = MessageBox.Show($"Bạn có chắc muốn xóa profile '{_selectedProfile.ProfileName}'?",
                                               "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _profileManager.DeleteProfile(_selectedProfile);
                        
                        // Select first remaining profile
                        if (_profileManager.Profiles.Count > 0)
                        {
                            _selectedProfile = _profileManager.Profiles[0];
                            ApplyProfileToSettings(_selectedProfile);
                        }
                        
                        WriteDebugLog($"Profile deleted: {_selectedProfile.ProfileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error deleting profile: {ex.Message}");
                MessageBox.Show($"Lỗi xóa profile: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Import Profile clicked");
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn file ProSheets profile",
                    Filter = "ProSheets files (*.xml;*.json)|*.xml;*.json|XML files (*.xml)|*.xml|JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".xml"
                };

                // Try to default to ProSheets folder if exists
                var diRootsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                              "DiRoots", "ProSheets");
                if (Directory.Exists(diRootsPath))
                {
                    openFileDialog.InitialDirectory = diRootsPath;
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    string extension = Path.GetExtension(openFileDialog.FileName).ToLower();
                    
                    if (extension == ".xml")
                    {
                        // Handle XML profile import with custom file name generation
                        ImportXMLProfile(openFileDialog.FileName);
                    }
                    else if (extension == ".json")
                    {
                        // Handle JSON profile import (existing functionality)
                        _profileManager.LoadProSheetsProfile(openFileDialog.FileName);
                        
                        // Select the newly imported profile
                        var fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                        var importedProfile = _profileManager.Profiles.FirstOrDefault(p => 
                            p.ProfileName.Contains(fileName) || fileName.Contains(p.ProfileName));
                        
                        if (importedProfile != null)
                        {
                            _selectedProfile = importedProfile;
                            ApplyProfileToSettings(_selectedProfile);
                            WriteDebugLog($"JSON Profile imported successfully: {importedProfile.ProfileName}");
                            MessageBox.Show($"Đã import JSON profile: {importedProfile.ProfileName}", 
                                           "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Định dạng file không được hỗ trợ. Vui lòng chọn file .xml hoặc .json", 
                                       "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error importing profile: {ex.Message}");
                MessageBox.Show($"Lỗi import profile: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportXMLProfile(string xmlFilePath)
        {
            try
            {
                WriteDebugLog($"Importing XML profile: {xmlFilePath}");
                
                // Get current sheets from document
                var currentSheets = GetCurrentDocumentSheets();
                
                // Load XML profile and generate custom file names
                var sheetInfos = _profileManager.LoadXMLProfileWithSheets(xmlFilePath, currentSheets);
                
                if (sheetInfos.Any())
                {
                    // Update the current sheets with custom file names from XML profile
                    foreach (var sheetInfo in sheetInfos)
                    {
                        var existingSheet = _sheets?.FirstOrDefault(s => s.SheetNumber == sheetInfo.SheetNumber);
                        if (existingSheet != null)
                        {
                            existingSheet.CustomFileName = sheetInfo.CustomFileName;
                            existingSheet.Revision = sheetInfo.Revision;
                            existingSheet.Size = sheetInfo.Size;
                        }
                    }
                    
                    // Also load as regular profile for format settings
                    _profileManager.LoadProSheetsProfile(xmlFilePath);
                    
                    // Find and select the imported profile
                    var fileName = Path.GetFileNameWithoutExtension(xmlFilePath);
                    var importedProfile = _profileManager.Profiles.FirstOrDefault(p => 
                        p.ProfileName.Contains(fileName) || fileName.Contains(p.ProfileName));
                    
                    if (importedProfile != null)
                    {
                        _selectedProfile = importedProfile;
                        ApplyProfileToSettings(_selectedProfile);
                    }
                    
                    WriteDebugLog($"XML Profile imported with {sheetInfos.Count} custom file names");
                    MessageBox.Show($"Đã import XML profile với {sheetInfos.Count} custom file names.\nCác tên file đã được cập nhật trong bảng.", 
                                   "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    WriteDebugLog("No sheet infos generated from XML profile");
                    MessageBox.Show("Không thể tạo custom file names từ XML profile này.", 
                                   "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error importing XML profile: {ex.Message}");
                MessageBox.Show($"Lỗi import XML profile: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ViewSheet> GetCurrentDocumentSheets()
        {
            try
            {
                if (_document == null) return new List<ViewSheet>();
                
                var collector = new FilteredElementCollector(_document)
                    .OfClass(typeof(ViewSheet))
                    .WhereElementIsNotElementType();
                
                return collector.Cast<ViewSheet>()
                                .Where(sheet => !sheet.IsTemplate)
                                .ToList();
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error getting current document sheets: {ex.Message}");
                return new List<ViewSheet>();
            }
        }

        #endregion

        #region Missing Event Handlers

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Close button clicked");
            this.Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Export button clicked");
            // Export functionality
            try
            {
                WriteDebugLog("Starting export process...");
                var selectedSheets = _sheets?.Where(s => s.IsSelected).ToList() ?? new List<SheetItem>();
                if (selectedSheets.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất 1 sheet để export!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                WriteDebugLog($"Exporting {selectedSheets.Count} sheets");
                MessageBox.Show($"Sẽ export {selectedSheets.Count} sheets. Tính năng export đầy đủ sẽ được implement sau.", "Export Preview", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in export: {ex.Message}");
                MessageBox.Show($"Lỗi export: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetFileNames_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Reset File Names clicked");
            try
            {
                if (_sheets != null)
                {
                    foreach (var sheet in _sheets)
                    {
                        // Reset to default naming: Sheet Number
                        sheet.CustomFileName = sheet.SheetNumber;
                    }
                    WriteDebugLog($"Reset {_sheets.Count} custom file names to default");
                    MessageBox.Show($"Đã reset {_sheets.Count} custom file names về mặc định (Sheet Number).", 
                                   "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error resetting file names: {ex.Message}");
                MessageBox.Show($"Lỗi reset file names: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyTemplate_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Apply Template clicked");
            try
            {
                if (_selectedProfile != null && _sheets != null)
                {
                    // Show dialog to select XML profile for custom naming template
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = "Chọn XML Profile để áp dụng template custom file name",
                        Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                        DefaultExt = ".xml"
                    };

                    // Try to default to ProSheets folder if exists
                    var diRootsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                                  "DiRoots", "ProSheets");
                    if (Directory.Exists(diRootsPath))
                    {
                        openFileDialog.InitialDirectory = diRootsPath;
                    }

                    if (openFileDialog.ShowDialog() == true)
                    {
                        // Get current sheets from document
                        var currentSheets = GetCurrentDocumentSheets();
                        
                        // Load XML profile and generate custom file names
                        var sheetInfos = _profileManager.LoadXMLProfileWithSheets(openFileDialog.FileName, currentSheets);
                        
                        if (sheetInfos.Any())
                        {
                            // Apply custom file names from template
                            foreach (var sheetInfo in sheetInfos)
                            {
                                var existingSheet = _sheets.FirstOrDefault(s => s.SheetNumber == sheetInfo.SheetNumber);
                                if (existingSheet != null)
                                {
                                    existingSheet.CustomFileName = sheetInfo.CustomFileName;
                                }
                            }
                            
                            WriteDebugLog($"Applied template to {sheetInfos.Count} sheets");
                            MessageBox.Show($"Đã áp dụng template cho {sheetInfos.Count} sheets.\nCustom file names đã được cập nhật.", 
                                           "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Không thể tạo custom file names từ template này.", 
                                           "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn profile và load sheets trước.", 
                                   "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error applying template: {ex.Message}");
                MessageBox.Show($"Lỗi áp dụng template: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion



    }
}