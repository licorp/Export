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
using System.Windows.Media;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;
using ProSheetsAddin.Models;
using ProSheetsAddin.Managers;
using MessageBox = System.Windows.MessageBox;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using WpfGrid = System.Windows.Controls.Grid;
using WpfColor = System.Windows.Media.Color;

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

        // Properties for Create tab
        private string _outputFolder;
        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                _outputFolder = value;
                OnPropertyChanged(nameof(OutputFolder));
            }
        }

        public ObservableCollection<object> SelectedItemsForExport
        {
            get
            {
                var selectedItems = new ObservableCollection<object>();
                
                // Add selected sheets
                if (Sheets != null)
                {
                    foreach (var sheet in Sheets.Where(s => s.IsSelected))
                    {
                        selectedItems.Add(new
                        {
                            Number = sheet.SheetNumber,
                            Name = sheet.SheetName,
                            CustomFileName = sheet.CustomFileName,
                            Type = "Sheet"
                        });
                    }
                }
                
                // Add selected views
                if (Views != null)
                {
                    foreach (var view in Views.Where(v => v.IsSelected))
                    {
                        selectedItems.Add(new
                        {
                            Number = view.ViewType,
                            Name = view.ViewName,
                            CustomFileName = view.CustomFileName,
                            Type = "View"
                        });
                    }
                }
                
                return selectedItems;
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
            
            // Initialize output folder to Desktop
            OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            WriteDebugLog($"Default output folder set to: {OutputFolder}");
            
            InitializeComponent();
            WriteDebugLog("InitializeComponent completed");
            
            // Configure window for non-modal operation
            ConfigureNonModalWindow();
            
            // Set DataContext for binding - should point to this window, not ExportSettings
            this.DataContext = this;
            WriteDebugLog("DataContext set to this window");
            
            InitializeProfiles();
            LoadSheets();
            LoadViews();
            UpdateFormatSelection();
            UpdateNavigationButtons();
            
            WriteDebugLog("===== EXPORT + CONSTRUCTOR COMPLETED SUCCESSFULLY =====");
        }

        private void ConfigureNonModalWindow()
        {
            try
            {
                // Configure window to work well as non-modal
                this.ShowInTaskbar = true;
                this.Topmost = false;
                this.WindowState = WindowState.Normal;
                
                // Handle window closing event
                this.Closing += ProSheetsMainWindow_Closing;
                
                // Handle window activated/deactivated for better UX
                this.Activated += ProSheetsMainWindow_Activated;
                this.Deactivated += ProSheetsMainWindow_Deactivated;
                
                WriteDebugLog("Non-modal window configuration completed");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error configuring non-modal window: {ex.Message}");
            }
        }

        private void ProSheetsMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WriteDebugLog("ProSheets window is closing");
            // Don't prevent closing, but log it
        }

        private void ProSheetsMainWindow_Activated(object sender, EventArgs e)
        {
            WriteDebugLog("ProSheets window activated");
            // Window brought to front - could refresh data if needed
        }

        private void ProSheetsMainWindow_Deactivated(object sender, EventArgs e)
        {
            WriteDebugLog("ProSheets window deactivated");
            // Window lost focus - user might be working in Revit
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
                        Id = sheet.Id,
                        IsSelected = false,
                        SheetNumber = sheet.SheetNumber ?? "NO_NUMBER",
                        SheetName = sheet.Name ?? "NO_NAME",
                        Revision = revision,
                        Size = sheetSize,
                        CustomFileName = $"{sheet.SheetNumber ?? "UNKNOWN"}_{(sheet.Name ?? "UNKNOWN").Replace(" ", "_")}"
                    };
                    
                    WriteDebugLog($"Created SheetItem: Number='{sheetItem.SheetNumber}', Name='{sheetItem.SheetName}'");
                    
                    // Subscribe to PropertyChanged to track selection changes
                    sheetItem.PropertyChanged += (s, e) => 
                    {
                        if (e.PropertyName == "IsSelected")
                        {
                            WriteDebugLog($"Sheet selection changed: {sheetItem.SheetNumber} -> {sheetItem.IsSelected}");
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
                            UpdateStatusText();
                            UpdateExportSummary();
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
                
                UpdateStatusText();
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
            UpdateCreateTabSummary();
        }

        private void UpdateStatusText()
        {
            var selectedSheetsCount = Sheets?.Count(s => s.IsSelected) ?? 0;
            var totalSheetsCount = Sheets?.Count ?? 0;
            var selectedViewsCount = Views?.Count(v => v.IsSelected) ?? 0;
            var totalViewsCount = Views?.Count ?? 0;
            
            WriteDebugLog($"[Export +] UpdateStatusText called - Sheets: {selectedSheetsCount}/{totalSheetsCount}, Views: {selectedViewsCount}/{totalViewsCount}");
            
            // Update status text controls
            try
            {
                // Update sheet count text
                if (SheetsCountText != null)
                {
                    SheetsCountText.Text = $"{selectedSheetsCount} sheets selected";
                }
                
                // Update views count text  
                if (ViewsCountText != null)
                {
                    ViewsCountText.Text = $"{selectedViewsCount} views selected";
                }
                
                // Update total items text
                if (TotalItemsText != null)
                {
                    var totalItemsCount = totalSheetsCount + totalViewsCount;
                    TotalItemsText.Text = $"Total: {totalItemsCount} items";
                }
                
                var totalSelected = selectedSheetsCount + selectedViewsCount;
                var totalItemsForTitle = totalSheetsCount + totalViewsCount;
                this.Title = $"Export + - {totalSelected} of {totalItemsForTitle} items selected ({selectedSheetsCount} sheets, {selectedViewsCount} views)";
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error updating status controls: {ex.Message}");
            }
            
            UpdateCreateTabSummary();
        }

        private void UpdateCreateTabSummary()
        {
            try
            {
                // Update selection summary
                var sheetsSelected = Sheets?.Count(s => s.IsSelected) ?? 0;
                var viewsSelected = Views?.Count(v => v.IsSelected) ?? 0;
                var totalSelected = sheetsSelected + viewsSelected;
                
                if (SelectionSummaryText != null)
                {
                    if (totalSelected == 0)
                    {
                        SelectionSummaryText.Text = "No items selected";
                    }
                    else
                    {
                        var items = new List<string>();
                        if (sheetsSelected > 0) items.Add($"{sheetsSelected} sheet{(sheetsSelected > 1 ? "s" : "")}");
                        if (viewsSelected > 0) items.Add($"{viewsSelected} view{(viewsSelected > 1 ? "s" : "")}");
                        SelectionSummaryText.Text = $"{totalSelected} item{(totalSelected > 1 ? "s" : "")} selected: {string.Join(", ", items)}";
                    }
                }
                
                // Update format summary
                if (FormatSummaryText != null)
                {
                    var formats = new List<string>();
                    if (ExportSettings?.IsPdfSelected == true) formats.Add("PDF");
                    if (ExportSettings?.IsDwgSelected == true) formats.Add("DWG");
                    // Note: Replace IsImageSelected with correct property when available
                    // if (ExportSettings?.IsImageSelected == true) formats.Add("Image");
                    if (ExportSettings?.IsIfcSelected == true) formats.Add("IFC");
                    
                    FormatSummaryText.Text = formats.Any() ? string.Join(", ", formats) : "No formats selected";
                }
                
                // Refresh SelectedItemsForExport binding
                OnPropertyChanged(nameof(SelectedItemsForExport));
                
                WriteDebugLog($"Create tab summary updated: {totalSelected} items, formats updated");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error updating Create tab summary: {ex.Message}");
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
            UpdateStatusText();
        }

        private void ViewSheetSetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo?.SelectedItem is ComboBoxItem item)
            {
                WriteDebugLog($"View/Sheet Set changed to: {item.Content}");
                // Auto-filter when selection changes
                FilterSheetsBySet(item.Content.ToString());
            }
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

        private void ImportProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Import Profile clicked");
            try
            {
                // Open file dialog to select JSON profile
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Import Profile from JSON",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string jsonPath = openFileDialog.FileName;
                    WriteDebugLog($"Selected JSON file: {jsonPath}");
                    
                    // Read and import profile
                    var profile = _profileManager.LoadProfileFromFile(jsonPath);
                    if (profile != null)
                    {
                        _profileManager.SaveProfile(profile);
                        _selectedProfile = profile;
                        
                        // Refresh profile list
                        InitializeProfiles();
                        
                        WriteDebugLog($"Profile imported: {profile.ProfileName}");
                        MessageBox.Show($"Profile '{profile.ProfileName}' đã được import thành công!", 
                                       "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error importing profile: {ex.Message}");
                MessageBox.Show($"Lỗi import profile: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Export Profile clicked");
            try
            {
                if (_selectedProfile != null)
                {
                    // Update profile with current settings
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
                    
                    // Save to internal storage first
                    _profileManager.SaveProfile(_selectedProfile);
                    
                    // Open save dialog to export to JSON file
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = "Export Profile to JSON",
                        Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                        DefaultExt = ".json",
                        FileName = _selectedProfile.ProfileName + ".json"
                    };
                    
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string jsonPath = saveFileDialog.FileName;
                        _profileManager.ExportProfileToFile(_selectedProfile, jsonPath);
                        
                        WriteDebugLog($"Profile exported to: {jsonPath}");
                        MessageBox.Show($"Profile '{_selectedProfile.ProfileName}' đã được export thành công!", 
                                       "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một profile trước!", "Thông báo", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error exporting profile: {ex.Message}");
                MessageBox.Show($"Lỗi export profile: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        #region Navigation Methods

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateNavigationButtons();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Back button clicked");
            
            if (MainTabControl.SelectedIndex > 0)
            {
                MainTabControl.SelectedIndex--;
                WriteDebugLog($"Navigated to tab index: {MainTabControl.SelectedIndex}");
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Next button clicked");
            
            if (MainTabControl.SelectedIndex < MainTabControl.Items.Count - 1)
            {
                MainTabControl.SelectedIndex++;
                WriteDebugLog($"Navigated to tab index: {MainTabControl.SelectedIndex}");
            }
        }

        private void UpdateNavigationButtons()
        {
            try
            {
                int selectedIndex = MainTabControl.SelectedIndex;
                int totalTabs = MainTabControl.Items.Count;
                
                // Tab 0 = Sheets: Back disabled, Next enabled
                // Tab 1 = Format: Both enabled
                // Tab 2 = Create: Back enabled, Next disabled
                
                BackButton.IsEnabled = selectedIndex > 0;
                NextButton.IsEnabled = selectedIndex < totalTabs - 1;
                
                WriteDebugLog($"Navigation buttons updated - Tab: {selectedIndex}, Back: {BackButton.IsEnabled}, Next: {NextButton.IsEnabled}");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error updating navigation buttons: {ex.Message}");
            }
        }

        #endregion

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
            try
            {
                WriteDebugLog("Starting export process...");
                
                var selectedSheets = _sheets?.Where(s => s.IsSelected).ToList() ?? new List<SheetItem>();
                var selectedViews = _views?.Where(v => v.IsSelected).ToList() ?? new List<ViewItem>();
                var totalSelected = selectedSheets.Count + selectedViews.Count;
                
                if (totalSelected == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất 1 sheet hoặc view để export!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                WriteDebugLog($"Exporting {selectedSheets.Count} sheets and {selectedViews.Count} views");

                // Get output folder
                string outputFolder = OutputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // Get selected formats
                var formats = ExportSettings?.GetSelectedFormatsList() ?? new List<string>();
                if (!formats.Any())
                {
                    MessageBox.Show("Vui lòng chọn ít nhất 1 format để export!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                WriteDebugLog($"Selected formats: {string.Join(", ", formats)}");

                bool exportSuccess = false;
                var exportResults = new List<string>();

                // Export PDF
                if (ExportSettings.IsPdfSelected && selectedSheets.Any())
                {
                    try
                    {
                        var pdfManager = new PDFExportManager(_document);
                        var viewSheets = selectedSheets.Select(s => _document.GetElement(s.Id) as ViewSheet).Where(vs => vs != null).ToList();
                        bool pdfResult = pdfManager.ExportSheetsToPDF(viewSheets, outputFolder, ExportSettings);
                        exportResults.Add($"PDF: {(pdfResult ? "Success" : "Failed")}");
                        exportSuccess |= pdfResult;
                        WriteDebugLog($"PDF export: {(pdfResult ? "Success" : "Failed")}");
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"PDF export error: {ex.Message}");
                        exportResults.Add($"PDF: Failed ({ex.Message})");
                    }
                }

                // Export DWG
                if (ExportSettings.IsDwgSelected && selectedSheets.Any())
                {
                    try
                    {
                        var dwgManager = new DWGExportManager(_document);
                        var viewSheets = selectedSheets.Select(s => _document.GetElement(s.Id) as ViewSheet).Where(vs => vs != null).ToList();
                        var dwgSettings = new PSDWGExportSettings { OutputFolder = outputFolder };
                        bool dwgResult = dwgManager.ExportToDWG(viewSheets, dwgSettings);
                        exportResults.Add($"DWG: {(dwgResult ? "Success" : "Failed")}");
                        exportSuccess |= dwgResult;
                        WriteDebugLog($"DWG export: {(dwgResult ? "Success" : "Failed")}");
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"DWG export error: {ex.Message}");
                        exportResults.Add($"DWG: Failed ({ex.Message})");
                    }
                }

                // Export IFC
                if (ExportSettings.IsIfcSelected)
                {
                    try
                    {
                        var ifcManager = new IFCExportManager();
                        var viewSheets = selectedSheets.Select(s => _document.GetElement(s.Id) as ViewSheet).Where(vs => vs != null).ToList();
                        var ifcSettings = new PSIFCExportSettings { OutputFolder = outputFolder };
                        ifcManager.ExportToIFC(viewSheets, ifcSettings);
                        exportResults.Add($"IFC: Success");
                        exportSuccess = true;
                        WriteDebugLog($"IFC export: Success");
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"IFC export error: {ex.Message}");
                        exportResults.Add($"IFC: Failed ({ex.Message})");
                    }
                }

                // Export Navisworks
                if (ExportSettings.IsNwcSelected)
                {
                    try
                    {
                        var nwcManager = new NavisworksExportManager(_document);
                        bool nwcResult = false;
                        
                        if (selectedViews.Any())
                        {
                            nwcResult = nwcManager.ExportToNavisworks(selectedViews, outputFolder);
                        }
                        else if (selectedSheets.Any())
                        {
                            nwcResult = nwcManager.ExportSheetsReference(selectedSheets, outputFolder);
                        }
                        
                        exportResults.Add($"Navisworks: {(nwcResult ? "Success" : "Failed")}");
                        exportSuccess |= nwcResult;
                        WriteDebugLog($"Navisworks export: {(nwcResult ? "Success" : "Failed")}");
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"Navisworks export error: {ex.Message}");
                        exportResults.Add($"Navisworks: Failed ({ex.Message})");
                    }
                }

                // Show results
                if (exportSuccess)
                {
                    var successMessage = $"Export hoàn tất!\n\n" +
                                       $"Items: {totalSelected} ({selectedSheets.Count} sheets, {selectedViews.Count} views)\n" +
                                       $"Output: {outputFolder}\n\n" +
                                       $"Results:\n{string.Join("\n", exportResults)}";
                    
                    MessageBox.Show(successMessage, "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    WriteDebugLog("Export completed successfully");
                }
                else
                {
                    MessageBox.Show($"Export failed or no files were exported.\n\nResults:\n{string.Join("\n", exportResults)}", 
                                   "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    WriteDebugLog("Export failed or no files exported");
                }
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

        #region Create Tab Event Handlers

        private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Browse output folder clicked");
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Chọn thư mục xuất file",
                    SelectedPath = OutputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    ShowNewFolderButton = true
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputFolder = dialog.SelectedPath;
                    WriteDebugLog($"Output folder selected: {OutputFolder}");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error browsing output folder: {ex.Message}");
                MessageBox.Show($"Lỗi chọn thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateFiles_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Create Files clicked");
            try
            {
                // Validate selections
                var sheetsSelected = Sheets?.Count(s => s.IsSelected) ?? 0;
                var viewsSelected = Views?.Count(v => v.IsSelected) ?? 0;
                var totalSelected = sheetsSelected + viewsSelected;

                if (totalSelected == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một sheet hoặc view để xuất.", 
                                   "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(OutputFolder) || !Directory.Exists(OutputFolder))
                {
                    MessageBox.Show("Vui lòng chọn thư mục xuất hợp lệ.", 
                                   "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if any format is selected
                var hasFormat = (ExportSettings?.IsPdfSelected == true) ||
                               (ExportSettings?.IsDwgSelected == true) ||
                               // (ExportSettings?.IsImageSelected == true) ||  // Remove until property exists
                               (ExportSettings?.IsIfcSelected == true);

                if (!hasFormat)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một định dạng xuất.", 
                                   "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show confirmation dialog
                var message = $"Bạn sắp xuất {totalSelected} item(s) ";
                if (sheetsSelected > 0 && viewsSelected > 0)
                {
                    message += $"({sheetsSelected} sheet(s) và {viewsSelected} view(s)) ";
                }
                else if (sheetsSelected > 0)
                {
                    message += $"({sheetsSelected} sheet(s)) ";
                }
                else
                {
                    message += $"({viewsSelected} view(s)) ";
                }
                message += $"vào thư mục:\n{OutputFolder}\n\nTiếp tục?";

                var result = MessageBox.Show(message, "Xác nhận xuất file", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Call the existing export method
                    WriteDebugLog("Starting export process from Create tab");
                    ExportButton_Click(sender, e); // Call existing export logic
                }
                else
                {
                    WriteDebugLog("User cancelled export from Create tab");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in CreateFiles_Click: {ex.Message}");
                MessageBox.Show($"Lỗi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Enhanced UI Event Handlers

        private void FilterByVSSet_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Filter by View/Sheet Set clicked");
            try
            {
                if (ViewSheetSetCombo?.SelectedItem is ComboBoxItem selectedItem)
                {
                    string selectedSet = selectedItem.Content.ToString();
                    WriteDebugLog($"Filtering by: {selectedSet}");
                    
                    if (selectedSet == "<None>")
                    {
                        // Show all items
                        LoadSheets();
                        LoadViews();
                    }
                    else
                    {
                        // Filter based on selected set
                        FilterSheetsBySet(selectedSet);
                    }
                    
                    MessageBox.Show($"Filtered by {selectedSet}", "Filter Applied", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in FilterByVSSet_Click: {ex.Message}");
                MessageBox.Show($"Lỗi khi lọc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Reset filter clicked");
            try
            {
                // Reset ComboBox to <None>
                if (ViewSheetSetCombo?.Items.Count > 0)
                {
                    ViewSheetSetCombo.SelectedIndex = 0; // Select "<None>"
                }
                
                // Reload all data
                LoadSheets();
                LoadViews();
                
                MessageBox.Show("Filter reset - showing all items", "Filter Reset", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in ResetFilter_Click: {ex.Message}");
                MessageBox.Show($"Lỗi khi reset filter: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetCustomFileName_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Set Custom File Name clicked");
            try
            {
                if (sender is Button button && button.Tag is SheetItem sheetItem)
                {
                    // Create a simple parameter selection dialog
                    var parameterDialog = new ParameterSelectionDialog(_document, sheetItem);
                    if (parameterDialog.ShowDialog() == true)
                    {
                        string newFileName = parameterDialog.GeneratedFileName;
                        if (!string.IsNullOrEmpty(newFileName))
                        {
                            sheetItem.CustomFileName = newFileName;
                            WriteDebugLog($"Custom file name set to: {newFileName}");
                        }
                    }
                }
                else if (sender is Button buttonView && buttonView.Tag is ViewItem viewItem)
                {
                    // Handle view item parameter selection
                    var parameterDialog = new ParameterSelectionDialog(_document, viewItem);
                    if (parameterDialog.ShowDialog() == true)
                    {
                        string newFileName = parameterDialog.GeneratedFileName;
                        if (!string.IsNullOrEmpty(newFileName))
                        {
                            viewItem.CustomFileName = newFileName;
                            WriteDebugLog($"Custom file name set for view: {newFileName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in SetCustomFileName_Click: {ex.Message}");
                MessageBox.Show($"Lỗi khi set custom file name: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetAllCustomFileName_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Set All Custom File Name clicked");
            try
            {
                // Determine which items are currently visible
                var targetItems = new List<object>();
                bool isSheetMode = SheetsRadio?.IsChecked == true;
                
                if (isSheetMode)
                {
                    // Get selected sheets first, if none selected then get all sheets
                    var selectedSheets = Sheets?.Where(s => s.IsSelected).ToList() ?? new List<SheetItem>();
                    if (selectedSheets.Any())
                    {
                        targetItems.AddRange(selectedSheets);
                        WriteDebugLog($"Found {selectedSheets.Count} selected sheets");
                    }
                    else
                    {
                        // No sheets selected, apply to all sheets
                        var allSheets = Sheets?.ToList() ?? new List<SheetItem>();
                        targetItems.AddRange(allSheets);
                        WriteDebugLog($"No sheets selected, applying to all {targetItems.Count} sheets");
                    }
                }
                else if (ViewsRadio?.IsChecked == true)
                {
                    // Get selected views first, if none selected then get all views
                    var selectedViews = Views?.Where(v => v.IsSelected).ToList() ?? new List<ViewItem>();
                    if (selectedViews.Any())
                    {
                        targetItems.AddRange(selectedViews);
                        WriteDebugLog($"Found {selectedViews.Count} selected views");
                    }
                    else
                    {
                        // No views selected, apply to all views
                        var allViews = Views?.ToList() ?? new List<ViewItem>();
                        targetItems.AddRange(allViews);
                        WriteDebugLog($"No views selected, applying to all {targetItems.Count} views");
                    }
                }

                if (!targetItems.Any())
                {
                    MessageBox.Show("No sheets or views available to configure.", 
                                   "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Use the first item to get available parameters
                var firstItem = targetItems.First();
                var parameterDialog = new ParameterSelectionDialog(_document, firstItem);
                
                string actionDescription = isSheetMode ? "sheets" : "views";
                bool hasSelection = false;
                
                if (isSheetMode)
                {
                    hasSelection = Sheets?.Any(s => s.IsSelected) == true;
                }
                else
                {
                    hasSelection = Views?.Any(v => v.IsSelected) == true;
                }
                
                string message = hasSelection 
                    ? $"Configure custom filename for {targetItems.Count} selected {actionDescription}?"
                    : $"No items selected. Configure custom filename for ALL {targetItems.Count} {actionDescription}?";
                    
                var result = MessageBox.Show(message, "Confirm Action", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
                
                if (parameterDialog.ShowDialog() == true)
                {
                    string pattern = parameterDialog.GeneratedFileName;
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        int updatedCount = 0;
                        
                        // Apply the pattern to all target items
                        foreach (var item in targetItems)
                        {
                            try
                            {
                                if (item is SheetItem sheet)
                                {
                                    // Generate filename based on sheet's parameters
                                    var sheetDialog = new ParameterSelectionDialog(_document, sheet);
                                    string fileName = sheetDialog.GenerateFilename(pattern, sheet);
                                    sheet.CustomFileName = fileName;
                                    updatedCount++;
                                    WriteDebugLog($"Set custom filename for sheet {sheet.SheetNumber}: {fileName}");
                                }
                                else if (item is ViewItem view)
                                {
                                    // Generate filename based on view's parameters
                                    var viewDialog = new ParameterSelectionDialog(_document, view);
                                    string fileName = viewDialog.GenerateFilename(pattern, view);
                                    view.CustomFileName = fileName;
                                    updatedCount++;
                                    WriteDebugLog($"Set custom filename for view {view.ViewName}: {fileName}");
                                }
                            }
                            catch (Exception itemEx)
                            {
                                WriteDebugLog($"Error setting filename for item: {itemEx.Message}");
                            }
                        }
                        
                        // Force UI update
                        if (isSheetMode && SheetsDataGrid != null)
                        {
                            SheetsDataGrid.Items.Refresh();
                        }
                        else if (ViewsDataGrid != null)
                        {
                            ViewsDataGrid.Items.Refresh();
                        }
                        
                        WriteDebugLog($"Applied custom filename pattern to {updatedCount} items");
                        MessageBox.Show($"Custom filename pattern applied to {updatedCount} {actionDescription} successfully!\n\nPattern: {pattern}", 
                                       "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in SetAllCustomFileName_Click: {ex.Message}");
                MessageBox.Show($"Error setting custom filename: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterSheetsBySet(string setName)
        {
            WriteDebugLog($"Filtering sheets by set: {setName}");
            
            try
            {
                // Get current sheets from the existing Sheets collection
                var allSheets = Sheets ?? new ObservableCollection<SheetItem>();
                var filteredSheets = new ObservableCollection<SheetItem>();
                
                foreach (var sheet in allSheets)
                {
                    bool includeSheet = false;
                    
                    // Filter based on sheet categorization
                    switch (setName.ToUpper())
                    {
                        case "ARCHITECTURAL":
                            includeSheet = IsArchitecturalSheet(sheet);
                            break;
                        case "STRUCTURAL":
                            includeSheet = IsStructuralSheet(sheet);
                            break;
                        case "MEP":
                            includeSheet = IsMEPSheet(sheet);
                            break;
                        case "ALL SHEETS":
                        case "<NONE>":
                        default:
                            includeSheet = true;
                            break;
                    }
                    
                    if (includeSheet)
                    {
                        filteredSheets.Add(sheet);
                    }
                }
                
                Sheets = filteredSheets;
                WriteDebugLog($"Filtered to {filteredSheets.Count} sheets");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error filtering sheets: {ex.Message}");
            }
        }

        private bool IsArchitecturalSheet(SheetItem sheet)
        {
            // Simple logic based on sheet number or name patterns
            string number = sheet.SheetNumber?.ToUpper() ?? "";
            string name = sheet.SheetName?.ToUpper() ?? "";
            
            return number.StartsWith("A") || 
                   name.Contains("ARCHITECTURAL") || 
                   name.Contains("FLOOR PLAN") ||
                   name.Contains("ELEVATION") ||
                   name.Contains("SECTION");
        }

        private bool IsStructuralSheet(SheetItem sheet)
        {
            string number = sheet.SheetNumber?.ToUpper() ?? "";
            string name = sheet.SheetName?.ToUpper() ?? "";
            
            return number.StartsWith("S") || 
                   name.Contains("STRUCTURAL") || 
                   name.Contains("FOUNDATION") ||
                   name.Contains("FRAMING");
        }

        private bool IsMEPSheet(SheetItem sheet)
        {
            string number = sheet.SheetNumber?.ToUpper() ?? "";
            string name = sheet.SheetName?.ToUpper() ?? "";
            
            return number.StartsWith("M") || 
                   number.StartsWith("E") ||
                   number.StartsWith("P") ||
                   name.Contains("MECHANICAL") || 
                   name.Contains("ELECTRICAL") ||
                   name.Contains("PLUMBING") ||
                   name.Contains("MEP");
        }

        #endregion

    }

    #region Parameter Selection Dialog

    public class ParameterSelectionDialog : Window
    {
        private readonly Document _document;
        private readonly object _item;
        private ComboBox _parameterCombo;
        private TextBox _previewTextBox;
        private CheckBox _includeRevisionCheck;
        private CheckBox _includeSheetNumberCheck;
        private CheckBox _includeSheetNameCheck;
        
        public string GeneratedFileName { get; private set; }

        public ParameterSelectionDialog(Document document, object item)
        {
            _document = document;
            _item = item;
            
            Title = "Set Custom File Name from Parameters";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            var grid = new WpfGrid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            grid.Margin = new Thickness(20, 20, 20, 20);

            // Title
            var titleBlock = new TextBlock
            {
                Text = "Configure Custom File Name",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            WpfGrid.SetRow(titleBlock, 0);
            grid.Children.Add(titleBlock);

            // Include options
            _includeSheetNumberCheck = new CheckBox
            {
                Content = "Include Sheet Number",
                IsChecked = true,
                Margin = new Thickness(0, 5, 0, 5)
            };
            _includeSheetNumberCheck.Checked += UpdatePreview;
            _includeSheetNumberCheck.Unchecked += UpdatePreview;
            WpfGrid.SetRow(_includeSheetNumberCheck, 1);
            grid.Children.Add(_includeSheetNumberCheck);

            _includeSheetNameCheck = new CheckBox
            {
                Content = "Include Sheet Name",
                IsChecked = true,
                Margin = new Thickness(0, 5, 0, 5)
            };
            _includeSheetNameCheck.Checked += UpdatePreview;
            _includeSheetNameCheck.Unchecked += UpdatePreview;
            WpfGrid.SetRow(_includeSheetNameCheck, 2);
            grid.Children.Add(_includeSheetNameCheck);

            _includeRevisionCheck = new CheckBox
            {
                Content = "Include Revision",
                IsChecked = false,
                Margin = new Thickness(0, 5, 0, 5)
            };
            _includeRevisionCheck.Checked += UpdatePreview;
            _includeRevisionCheck.Unchecked += UpdatePreview;
            WpfGrid.SetRow(_includeRevisionCheck, 3);
            grid.Children.Add(_includeRevisionCheck);

            // Parameter selection
            var paramLabel = new TextBlock
            {
                Text = "Additional Parameter:",
                Margin = new Thickness(0, 15, 0, 5)
            };
            WpfGrid.SetRow(paramLabel, 4);
            grid.Children.Add(paramLabel);

            _parameterCombo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 15)
            };
            _parameterCombo.SelectionChanged += UpdatePreview;
            LoadAvailableParameters();
            WpfGrid.SetRow(_parameterCombo, 5);
            grid.Children.Add(_parameterCombo);

            // Preview
            var previewLabel = new TextBlock
            {
                Text = "Preview:",
                Margin = new Thickness(0, 10, 0, 5)
            };
            WpfGrid.SetRow(previewLabel, 6);
            grid.Children.Add(previewLabel);

            _previewTextBox = new TextBox
            {
                IsReadOnly = true,
                Background = new SolidColorBrush(WpfColor.FromRgb(248, 248, 248)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            WpfGrid.SetRow(_previewTextBox, 6);
            grid.Children.Add(_previewTextBox);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            WpfGrid.SetRow(buttonPanel, 7);
            grid.Children.Add(buttonPanel);

            Content = grid;
            
            // Initial preview update
            UpdatePreview(null, null);
        }

        private void LoadAvailableParameters()
        {
            _parameterCombo.Items.Add("<None>");
            _parameterCombo.Items.Add("Project Number");
            _parameterCombo.Items.Add("Project Name");
            _parameterCombo.Items.Add("Current Date");
            _parameterCombo.Items.Add("Sheet Issue Date");
            _parameterCombo.SelectedIndex = 0;
        }

        private void UpdatePreview(object sender, RoutedEventArgs e)
        {
            try
            {
                var parts = new List<string>();

                if (_includeSheetNumberCheck?.IsChecked == true && _item is SheetItem sheet)
                {
                    parts.Add(sheet.SheetNumber);
                }

                if (_includeSheetNameCheck?.IsChecked == true && _item is SheetItem sheetForName)
                {
                    // Clean sheet name for filename
                    string cleanName = CleanFileName(sheetForName.SheetName);
                    parts.Add(cleanName);
                }

                if (_includeRevisionCheck?.IsChecked == true && _item is SheetItem sheetForRev)
                {
                    parts.Add($"Rev{sheetForRev.Revision ?? "A"}");
                }

                if (_parameterCombo?.SelectedItem?.ToString() != "<None>")
                {
                    string paramValue = GetParameterValue(_parameterCombo.SelectedItem.ToString());
                    if (!string.IsNullOrEmpty(paramValue))
                    {
                        parts.Add(CleanFileName(paramValue));
                    }
                }

                GeneratedFileName = string.Join("_", parts.Where(p => !string.IsNullOrEmpty(p)));
                
                if (_previewTextBox != null)
                {
                    _previewTextBox.Text = GeneratedFileName;
                }
            }
            catch (Exception ex)
            {
                if (_previewTextBox != null)
                {
                    _previewTextBox.Text = $"Error: {ex.Message}";
                }
            }
        }

        private string GetParameterValue(string parameterName)
        {
            switch (parameterName)
            {
                case "Project Number":
                    return _document.ProjectInformation.Number ?? "";
                case "Project Name":
                    return _document.ProjectInformation.Name ?? "";
                case "Current Date":
                    return DateTime.Now.ToString("yyyyMMdd");
                case "Sheet Issue Date":
                    return DateTime.Now.ToString("yyyyMMdd");
                default:
                    return "";
            }
        }

        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            
            // Remove invalid characters and clean up
            var invalidChars = Path.GetInvalidFileNameChars();
            string cleaned = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            // Replace spaces with underscores and limit length
            cleaned = cleaned.Replace(" ", "_").Replace("-", "_");
            
            // Remove consecutive underscores
            while (cleaned.Contains("__"))
            {
                cleaned = cleaned.Replace("__", "_");
            }
            
            return cleaned.Trim('_').Substring(0, Math.Min(cleaned.Length, 50));
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(GeneratedFileName))
            {
                MessageBox.Show("Please configure at least one parameter for the file name.", 
                               "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }

        // Method to generate filename for any item based on current dialog settings
        public string GenerateFilename(string pattern, object item)
        {
            try
            {
                var parts = new List<string>();

                if (_includeSheetNumberCheck?.IsChecked == true && item is SheetItem sheet)
                {
                    parts.Add(sheet.SheetNumber);
                }
                else if (item is ViewItem view)
                {
                    parts.Add(view.ViewType?.Replace(" ", "_"));
                }

                if (_includeSheetNameCheck?.IsChecked == true && item is SheetItem sheetForName)
                {
                    string cleanName = CleanFileName(sheetForName.SheetName);
                    parts.Add(cleanName);
                }
                else if (item is ViewItem viewForName)
                {
                    string cleanName = CleanFileName(viewForName.ViewName);
                    parts.Add(cleanName);
                }

                if (_includeRevisionCheck?.IsChecked == true && item is SheetItem sheetForRev)
                {
                    parts.Add($"Rev{sheetForRev.Revision ?? "A"}");
                }

                if (_parameterCombo?.SelectedItem?.ToString() != "<None>")
                {
                    string paramValue = GetParameterValue(_parameterCombo.SelectedItem.ToString());
                    if (!string.IsNullOrEmpty(paramValue))
                    {
                        parts.Add(CleanFileName(paramValue));
                    }
                }

                return string.Join("_", parts.Where(p => !string.IsNullOrEmpty(p)));
            }
            catch
            {
                return pattern; // Fallback to original pattern
            }
        }
    }

    #endregion
}