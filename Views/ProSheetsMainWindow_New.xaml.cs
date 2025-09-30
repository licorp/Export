using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Diagnostics;
using Autodesk.Revit.DB;
using ProSheetsAddin.Models;
using ProSheetsAddin.Managers;

namespace ProSheetsAddin.Views
{
    /// <summary>
    /// ProSheets Main Window - Professional Style Interface
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
            WriteDebugLog("[ProSheets] ===== PROSHEETS CONSTRUCTOR STARTED =====");
            
            _document = document;
            
            // Initialize export settings with data binding
            ExportSettings = new ExportSettings();
            
            InitializeComponent();
            
            // Set DataContext for binding
            this.DataContext = ExportSettings;
            
            LoadSheets();
            UpdateFormatSelection();
            InitializeProfiles();
            
            WriteDebugLog("[ProSheets] ===== PROSHEETS CONSTRUCTOR COMPLETED =====");
        }

        /// <summary>
        /// Enhanced debug logging method with multiple output streams
        /// </summary>
        private void WriteDebugLog(string message)
        {
            try
            {
                // Multiple debug output methods for maximum visibility
                System.Diagnostics.Debug.WriteLine(message);
                Console.WriteLine(message);
                System.Diagnostics.Trace.WriteLine(message);
                
                // Also try EventLog for system-level logging
                try
                {
                    using (var eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "ProSheetsAddin";
                        eventLog.WriteEntry(message, EventLogEntryType.Information);
                    }
                }
                catch
                {
                    // EventLog might fail in some environments, ignore silently
                }
            }
            catch (Exception ex)
            {
                // Fallback logging
                System.Diagnostics.Debug.WriteLine($"[ProSheets] Logging error: {ex.Message}");
            }
        }

        private void LoadSheets()
        {
            WriteDebugLog("[ProSheets] LoadSheets started");
            
            try
            {
                _sheets = new ObservableCollection<SheetItem>();
                
                var collector = new FilteredElementCollector(_document)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(sheet => !sheet.IsTemplate);
                
                WriteDebugLog($"[ProSheets] Found {collector.Count()} sheets in document");
                
                foreach (var sheet in collector)
                {
                    var sheetItem = new SheetItem
                    {
                        IsSelected = false,
                        Number = sheet.SheetNumber,
                        Name = sheet.Name,
                        CustomDrawingNumber = $"{sheet.SheetNumber}_{sheet.Name.Replace(" ", "_")}"
                    };
                    
                    // Subscribe to PropertyChanged to track selection changes
                    sheetItem.PropertyChanged += (s, e) => 
                    {
                        if (e.PropertyName == "IsSelected")
                        {
                            WriteDebugLog($"[ProSheets] Sheet selection changed: {sheetItem.Number}");
                            UpdateStatusText();
                            UpdateExportSummary();
                        }
                    };
                    
                    _sheets.Add(sheetItem);
                    WriteDebugLog($"[ProSheets] Added sheet: {sheet.SheetNumber} - {sheet.Name}");
                }
                
                WriteDebugLog($"[ProSheets] Setting DataGrid ItemsSource with {_sheets.Count} items");
                if (SheetsDataGrid != null)
                {
                    SheetsDataGrid.ItemsSource = _sheets;
                }
                
                UpdateStatusText();
                UpdateExportSummary();
                WriteDebugLog("[ProSheets] LoadSheets completed successfully");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[ProSheets] ERROR in LoadSheets: {ex.Message}");
                MessageBox.Show($"Error loading sheets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusText()
        {
            var selectedCount = _sheets?.Count(s => s.IsSelected) ?? 0;
            WriteDebugLog($"[ProSheets] UpdateStatusText called - Selected count: {selectedCount}");
            
            // Update status via data binding and direct control access
            if (SheetCountLabel != null)
            {
                SheetCountLabel.Text = $"Đã chọn {selectedCount} sheets";
            }
            
            if (StatusLabel != null)
            {
                var selectedFormats = string.Join(", ", ExportSettings?.GetSelectedFormatsList() ?? new List<string>());
                StatusLabel.Text = $"{selectedCount} sheets selected | Formats: {selectedFormats}";
                WriteDebugLog($"[ProSheets] Status updated: {StatusLabel.Text}");
            }
        }

        private void UpdateFormatSelection()
        {
            WriteDebugLog("[ProSheets] UpdateFormatSelection called");
            
            try
            {
                WriteDebugLog($"[ProSheets] Current format states: {string.Join(", ", ExportSettings?.GetSelectedFormatsList() ?? new List<string>())}");
                
                // Format selection is handled by data binding in XAML
                WriteDebugLog("[ProSheets] Format selection updated via data binding");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[ProSheets] ERROR in UpdateFormatSelection: {ex.Message}");
            }
        }

        private void UpdateExportSummary()
        {
            try
            {
                var selectedCount = SelectedSheetsCount;
                var selectedFormats = ExportSettings?.GetSelectedFormatsList() ?? new List<string>();
                var estimatedFiles = selectedCount * selectedFormats.Count;

                if (SelectedSheetsLabel != null)
                    SelectedSheetsLabel.Text = selectedCount.ToString();

                if (SelectedFormatsLabel != null)
                    SelectedFormatsLabel.Text = selectedFormats.Any() ? string.Join(", ", selectedFormats) : "No formats selected";

                if (OutputFolderLabel != null)
                    OutputFolderLabel.Text = ExportSettings?.OutputFolder ?? "Not set";

                if (EstimatedFilesLabel != null)
                    EstimatedFilesLabel.Text = estimatedFiles.ToString();

                WriteDebugLog($"[ProSheets] Export summary updated: {selectedCount} sheets, {selectedFormats.Count} formats, {estimatedFiles} files");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[ProSheets] ERROR in UpdateExportSummary: {ex.Message}");
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
                WriteDebugLog("[ProSheets] Profiles initialized");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[ProSheets] ERROR initializing profiles: {ex.Message}");
            }
        }

        // Event Handlers for ProSheets Interface

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Profile selection changed");
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Add profile clicked");
            MessageBox.Show("Tính năng tạo profile mới sẽ được thêm trong phiên bản tiếp theo!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleAll_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Toggle All clicked");
            
            if (_sheets != null && _sheets.Any())
            {
                bool selectAll = !_sheets.All(s => s.IsSelected);
                foreach (var sheet in _sheets)
                {
                    sheet.IsSelected = selectAll;
                }
                WriteDebugLog($"[ProSheets] Toggled all sheets to: {selectAll}");
                UpdateStatusText();
                UpdateExportSummary();
            }
        }

        private void EditCustomDrawingNumber_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Edit Custom Drawing Number clicked");
            MessageBox.Show("Custom Drawing Number Editor sẽ được thêm trong phiên bản tiếp theo!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FormatToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Tag is string format)
            {
                WriteDebugLog($"[ProSheets] Format {format} checked via ToggleButton");
                ExportSettings?.SetFormatSelection(format, true);
                UpdateExportSummary();
            }
        }

        private void FormatToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Tag is string format)
            {
                WriteDebugLog($"[ProSheets] Format {format} unchecked via ToggleButton");
                ExportSettings?.SetFormatSelection(format, false);
                UpdateExportSummary();
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] Browse folder clicked - ProSheets Enhanced version");
            
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            
            if (!string.IsNullOrEmpty(ExportSettings?.OutputFolder))
            {
                dialog.SelectedPath = ExportSettings.OutputFolder;
                WriteDebugLog($"[ProSheets] Current folder: {ExportSettings.OutputFolder}");
            }
            
            dialog.Description = "Chọn thư mục xuất file ProSheets";
            dialog.ShowNewFolderButton = true;
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExportSettings.OutputFolder = dialog.SelectedPath;
                WriteDebugLog($"[ProSheets] Folder updated: {dialog.SelectedPath}");
                UpdateExportSummary();
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] ===== CREATE BUTTON CLICKED (PROSHEETS ENHANCED) =====");
            
            var selectedSheets = _sheets?.Where(s => s.IsSelected).ToList();
            WriteDebugLog($"[ProSheets] Found {selectedSheets?.Count ?? 0} selected sheets");
            
            if (selectedSheets == null || !selectedSheets.Any())
            {
                WriteDebugLog("[ProSheets] ERROR: No sheets selected!");
                MessageBox.Show("Vui lòng chọn ít nhất một sheet để export!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var outputPath = ExportSettings?.OutputFolder ?? "";
            WriteDebugLog($"[ProSheets] Output path: '{outputPath}'");
            
            if (string.IsNullOrEmpty(outputPath))
            {
                WriteDebugLog("[ProSheets] ERROR: Empty output path!");
                MessageBox.Show("Vui lòng chọn thư mục xuất file!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var selectedFormats = ExportSettings?.GetSelectedFormatsList() ?? new List<string>();
            WriteDebugLog($"[ProSheets] Selected formats: {string.Join(", ", selectedFormats)}");
            
            if (!selectedFormats.Any())
            {
                WriteDebugLog("[ProSheets] ERROR: No formats selected!");
                MessageBox.Show("Vui lòng chọn ít nhất một định dạng file!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Show detailed export summary
            var summary = $@"PROSHEETS EXPORT SUMMARY
            
Sheets: {selectedSheets.Count}
Formats: {string.Join(", ", selectedFormats)}
Output: {outputPath}
Estimated Files: {selectedSheets.Count * selectedFormats.Count}

Template: {ExportSettings?.FileNameTemplate ?? "Default"}
Combine Files: {ExportSettings?.CombineFiles ?? false}
Include Revision: {ExportSettings?.IncludeRevision ?? false}

Tiếp tục xuất file?";
            
            WriteDebugLog($"[ProSheets] Showing export summary dialog");
            var result = MessageBox.Show(summary, "ProSheets Export Confirmation", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                WriteDebugLog("[ProSheets] User confirmed export - Starting export process...");
                
                try
                {
                    // Start export process
                    var exportManager = new BatchExportManager();
                    
                    // Update status
                    if (StatusLabel != null)
                    {
                        StatusLabel.Text = "Exporting...";
                    }
                    
                    WriteDebugLog("[ProSheets] Export process started successfully");
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
                    WriteDebugLog($"[ProSheets] ERROR in export process: {ex.Message}");
                    MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    if (StatusLabel != null)
                    {
                        StatusLabel.Text = "Export failed";
                    }
                }
            }
            else
            {
                WriteDebugLog("[ProSheets] User cancelled export");
            }
        }

        private void ViewDebugLog_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("[ProSheets] View Debug Log clicked");
            
            var debugInfo = $@"=== PROSHEETS DEBUG INFO ===

Output Folder: {ExportSettings?.OutputFolder ?? "Not set"}
Selected Sheets: {SelectedSheetsCount}
Selected Formats: {string.Join(", ", ExportSettings?.GetSelectedFormatsList() ?? new List<string>())}
Combine Files: {ExportSettings?.CombineFiles ?? false}
Create Separate Folders: {ExportSettings?.CreateSeparateFolders ?? false}

=== CURRENT SETTINGS ===
Paper Placement: {ExportSettings?.PaperPlacement}
Hidden Line Views: {ExportSettings?.HiddenLineViews}
Colors: {ExportSettings?.Colors}
Raster Quality: {ExportSettings?.RasterQuality}

Document: {_document?.Title ?? "No document"}
Sheets Count: {_sheets?.Count ?? 0}

=== END DEBUG INFO ===";

            MessageBox.Show(debugInfo, "ProSheets Debug Log", MessageBoxButton.OK, MessageBoxImage.Information);
        }

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