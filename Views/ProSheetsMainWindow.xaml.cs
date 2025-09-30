using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Export + Main Window - Professional Style Interface
    /// </summary>
    public partial class ProSheetsMainWindow : Window
    {
        private readonly Document _document;
        private ObservableCollection<SheetItem> _sheets;

        // Enhanced properties for data binding
        public int SelectedSheetsCount 
        { 
            get 
            { 
                return _sheets?.Count(s => s.IsSelected) ?? 0; 
            } 
        }
        
        public ObservableCollection<SheetItem> SheetItems => _sheets;

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
            
            // Set DataContext for binding
            this.DataContext = ExportSettings;
            WriteDebugLog("DataContext set to ExportSettings");
            
            LoadSheets();
            UpdateFormatSelection();
            InitializeProfiles();
            
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
                _sheets = new ObservableCollection<SheetItem>();
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
                    var sheetItem = new SheetItem
                    {
                        IsSelected = false,
                        Number = sheet.SheetNumber ?? "NO_NUMBER",
                        Name = sheet.Name ?? "NO_NAME",
                        CustomDrawingNumber = $"{sheet.SheetNumber ?? "UNKNOWN"}_{(sheet.Name ?? "UNKNOWN").Replace(" ", "_")}"
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
                    
                    _sheets.Add(sheetItem);
                    addedCount++;
                    WriteDebugLog($"Added sheet #{addedCount}: {sheet.SheetNumber} - {sheet.Name}");
                }
                
                WriteDebugLog($"Setting DataGrid ItemsSource with {_sheets.Count} items");
                if (SheetsDataGrid != null)
                {
                    SheetsDataGrid.ItemsSource = _sheets;
                    WriteDebugLog("DataGrid ItemsSource set successfully");
                }
                else
                {
                    WriteDebugLog("WARNING: SheetsDataGrid is NULL!");
                }
                
                UpdateStatusText();
                UpdateExportSummary();
                WriteDebugLog($"LoadSheets completed successfully - Total sheets loaded: {_sheets.Count}");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"CRITICAL ERROR in LoadSheets: {ex.Message}");
                WriteDebugLog($"StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error loading sheets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusText()
        {
            var selectedCount = _sheets?.Count(s => s.IsSelected) ?? 0;
            WriteDebugLog($"[Export +] UpdateStatusText called - Selected count: {selectedCount}");
            
            // Update status via data binding and direct control access
            if (SheetCountLabel != null)
            {
                SheetCountLabel.Text = $"Đã chọn {selectedCount} sheets";
            }
            
            if (StatusLabel != null)
            {
                var selectedFormats = string.Join(", ", ExportSettings?.GetSelectedFormatsList() ?? new List<string>());
                StatusLabel.Text = $"{selectedCount} sheets selected | Formats: {selectedFormats}";
                WriteDebugLog($"[Export +] Status updated: {StatusLabel.Text}");
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

        private void InitializeProfiles()
        {
            try
            {
                if (ProfileComboBox != null)
                {
                    ProfileComboBox.Items.Add("Default Profile");
                    ProfileComboBox.Items.Add("PDF Only");
                    ProfileComboBox.Items.Add("DWG Export");
                    ProfileComboBox.Items.Add("All Formats");
                    ProfileComboBox.SelectedIndex = 0;
                }
                WriteDebugLog("[Export +] Profiles initialized");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export +] ERROR initializing profiles: {ex.Message}");
            }
        }

        // Event Handlers for Export + Interface

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteDebugLog("[Export +] Profile selection changed");
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[Export +] Add profile clicked");
            MessageBox.Show("Tính năng tạo profile mới sẽ được thêm trong phiên bản tiếp theo!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleAll_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[Export +] Toggle All clicked");
            
            if (_sheets != null && _sheets.Any())
            {
                bool selectAll = !_sheets.All(s => s.IsSelected);
                foreach (var sheet in _sheets)
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
            
            var selectedSheets = _sheets?.Where(s => s.IsSelected).ToList();
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
                    // Start export process with document parameter
                    var exportManager = new BatchExportManager(_document);
                    
                    // Update status
                    if (StatusLabel != null)
                    {
                        StatusLabel.Text = "Exporting...";
                    }
                    
                    WriteDebugLog("[Export +] Export process started successfully");
                    MessageBox.Show($"Export started!\n{selectedSheets.Count} sheets\n{selectedFormats.Count} formats\nOutput: {outputPath}", 
                        "Export Started", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                    // Reset status
                    if (StatusLabel != null)
                    {
                        StatusLabel.Text = "Export completed";
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"[Export +] ERROR in export process: {ex.Message}");
                    MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    if (StatusLabel != null)
                    {
                        StatusLabel.Text = "Export failed";
                    }
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
    }
}