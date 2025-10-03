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
using Autodesk.Revit.UI;
using ProSheetsAddin.Models;
using ProSheetsAddin.Managers;
using ProSheetsAddin.Commands;
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
        private readonly UIApplication _uiApp;
        private ObservableCollection<SheetItem> _sheets;
        private ProfileManagerService _profileManager;
        private Models.Profile _selectedProfile;
        private ExternalEvent _exportEvent;
        private ExportHandler _exportHandler;
        
        // PDF Export External Event
        private ExternalEvent _pdfExportEvent;
        private Events.PDFExportEventHandler _pdfExportHandler;

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

        // Export settings với data binding
        public ExportSettings ExportSettings { get; set; }
        
        // Export Queue Items for Create tab
        private ObservableCollection<ExportQueueItem> _exportQueueItems;
        public ObservableCollection<ExportQueueItem> ExportQueueItems
        {
            get => _exportQueueItems;
            set
            {
                _exportQueueItems = value;
                OnPropertyChanged(nameof(ExportQueueItems));
            }
        }
        
        
        public ProSheetsMainWindow(Document document) : this(document, null)
        {
        }

        public ProSheetsMainWindow(Document document, UIApplication uiApp)
        {
            WriteDebugLog("===== EXPORT + CONSTRUCTOR STARTED =====");
            WriteDebugLog($"Document: {document?.Title ?? "NULL"}");
            
            _document = document;
            _uiApp = uiApp;
            
            // Initialize External Event for export operations
            if (_uiApp != null)
            {
                _exportHandler = new ExportHandler();
                _exportEvent = ExternalEvent.Create(_exportHandler);
                WriteDebugLog("ExternalEvent initialized for export operations");
                
                // Initialize PDF Export External Event
                _pdfExportHandler = new Events.PDFExportEventHandler();
                _pdfExportEvent = ExternalEvent.Create(_pdfExportHandler);
                WriteDebugLog("PDF Export ExternalEvent initialized");
            }
            
            // Initialize export settings with data binding
            ExportSettings = new ExportSettings();
            WriteDebugLog("ExportSettings initialized");
            
            // Initialize output folder to Desktop
            OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            WriteDebugLog($"Default output folder set to: {OutputFolder}");
            
            // Initialize Export Queue with empty collection
            ExportQueueItems = new ObservableCollection<ExportQueueItem>();
            WriteDebugLog("ExportQueueItems initialized");
            
            InitializeComponent();
            WriteDebugLog("InitializeComponent completed");
            
            // TODO: Wire up Browse buttons for IFC Settings AFTER InitializeComponent
            // WireUpIFCBrowseButtons();
            // WriteDebugLog("IFC Browse buttons wired up");
            
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

        /// <summary>
        /// Update ExportSettings from UI controls before export
        /// This ensures UI selections are properly synchronized with settings object
        /// </summary>
        private void UpdateExportSettingsFromUI()
        {
            try
            {
                WriteDebugLog("Updating ExportSettings from UI controls...");
                
                // Update Raster Quality from ComboBox
                if (RasterQualityCombo.SelectedItem is ComboBoxItem rasterItem)
                {
                    string rasterText = rasterItem.Content?.ToString() ?? "High";
                    WriteDebugLog($"UI Raster Quality: {rasterText}");
                    
                    switch (rasterText)
                    {
                        case "Low":
                            ExportSettings.RasterQuality = PSRasterQuality.Low;
                            WriteDebugLog("✓ RasterQuality set to LOW (72 DPI)");
                            break;
                        case "Medium":
                            ExportSettings.RasterQuality = PSRasterQuality.Medium;
                            WriteDebugLog("✓ RasterQuality set to MEDIUM (150 DPI)");
                            break;
                        case "High":
                            ExportSettings.RasterQuality = PSRasterQuality.High;
                            WriteDebugLog("✓ RasterQuality set to HIGH (300 DPI)");
                            break;
                        case "Presentation":
                            ExportSettings.RasterQuality = PSRasterQuality.Maximum;
                            WriteDebugLog("✓ RasterQuality set to PRESENTATION/MAXIMUM (600 DPI)");
                            break;
                        default:
                            ExportSettings.RasterQuality = PSRasterQuality.High;
                            WriteDebugLog("⚠ Unknown raster quality, defaulting to HIGH");
                            break;
                    }
                }
                
                // Update Colors from ComboBox
                if (ColorsCombo.SelectedItem is ComboBoxItem colorItem)
                {
                    string colorText = colorItem.Content?.ToString() ?? "Color";
                    WriteDebugLog($"UI Colors: {colorText}");
                    
                    switch (colorText)
                    {
                        case "Color":
                            ExportSettings.Colors = PSColors.Color;
                            WriteDebugLog("✓ Colors set to COLOR");
                            break;
                        case "Black and White":
                        case "Black and white":
                            ExportSettings.Colors = PSColors.BlackAndWhite;
                            WriteDebugLog("✓ Colors set to BLACK AND WHITE");
                            break;
                        case "Grayscale":
                            ExportSettings.Colors = PSColors.Grayscale;
                            WriteDebugLog("✓ Colors set to GRAYSCALE");
                            break;
                        default:
                            ExportSettings.Colors = PSColors.Color;
                            WriteDebugLog("⚠ Unknown color mode, defaulting to COLOR");
                            break;
                    }
                }
                
                // Update Output Folder
                if (!string.IsNullOrEmpty(CreateFolderPathTextBox?.Text))
                {
                    ExportSettings.OutputFolder = CreateFolderPathTextBox.Text;
                    WriteDebugLog($"✓ Output folder: {ExportSettings.OutputFolder}");
                }
                
                // Update Paper Placement settings
                if (CenterRadio?.IsChecked == true)
                {
                    ExportSettings.PaperPlacement = PSPaperPlacement.Center;
                    WriteDebugLog("✓ Paper Placement: CENTER");
                }
                else if (OffsetRadio?.IsChecked == true)
                {
                    ExportSettings.PaperPlacement = PSPaperPlacement.OffsetFromCorner;
                    WriteDebugLog("✓ Paper Placement: OFFSET FROM CORNER");
                }
                
                // Update Paper Margin
                if (MarginCombo.SelectedItem is ComboBoxItem marginItem)
                {
                    string marginText = marginItem.Content?.ToString() ?? "No Margin";
                    WriteDebugLog($"UI Margin: {marginText}");
                    
                    switch (marginText)
                    {
                        case "No Margin":
                            ExportSettings.PaperMargin = PSPaperMargin.NoMargin;
                            WriteDebugLog("✓ Paper Margin: NO MARGIN");
                            break;
                        case "Printer Limit":
                            ExportSettings.PaperMargin = PSPaperMargin.PrinterLimit;
                            WriteDebugLog("✓ Paper Margin: PRINTER LIMIT");
                            break;
                        case "User Defined":
                            ExportSettings.PaperMargin = PSPaperMargin.UserDefined;
                            WriteDebugLog("✓ Paper Margin: USER DEFINED");
                            break;
                        default:
                            ExportSettings.PaperMargin = PSPaperMargin.NoMargin;
                            WriteDebugLog("⚠ Unknown margin type, defaulting to NO MARGIN");
                            break;
                    }
                }
                
                // Update Offset X and Y values
                if (double.TryParse(OffsetXTextBox?.Text, out double offsetX))
                {
                    ExportSettings.OffsetX = offsetX;
                    WriteDebugLog($"✓ Offset X: {offsetX}");
                }
                
                if (double.TryParse(OffsetYTextBox?.Text, out double offsetY))
                {
                    ExportSettings.OffsetY = offsetY;
                    WriteDebugLog($"✓ Offset Y: {offsetY}");
                }
                
                // Update Combine Files setting
                if (CombineFilesRadio?.IsChecked == true)
                {
                    ExportSettings.CombineFiles = true;
                    WriteDebugLog("✓ File Mode: COMBINE multiple sheets into single file");
                }
                else if (SeparateFilesRadio?.IsChecked == true)
                {
                    ExportSettings.CombineFiles = false;
                    WriteDebugLog("✓ File Mode: CREATE SEPARATE files");
                }
                
                // Update Keep Paper Size & Orientation setting
                if (KeepPaperSizeCheckBox?.IsChecked == true)
                {
                    ExportSettings.KeepPaperSize = true;
                    WriteDebugLog("✓ Keep Paper Size & Orientation: ENABLED");
                }
                else
                {
                    ExportSettings.KeepPaperSize = false;
                    WriteDebugLog("✓ Keep Paper Size & Orientation: DISABLED");
                }
                
                WriteDebugLog("===== ExportSettings Updated Successfully =====");
                WriteDebugLog($"Final Settings: RasterQuality={ExportSettings.RasterQuality}, Colors={ExportSettings.Colors}");
                WriteDebugLog($"Paper Placement: {ExportSettings.PaperPlacement}, Margin: {ExportSettings.PaperMargin}, Offset: ({ExportSettings.OffsetX}, {ExportSettings.OffsetY})");
                WriteDebugLog($"Combine Files: {ExportSettings.CombineFiles}, Keep Paper Size: {ExportSettings.KeepPaperSize}");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"ERROR updating ExportSettings from UI: {ex.Message}");
            }
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

        /// <summary>
        /// Show export completed dialog with Open Folder button
        /// </summary>
        private void ShowExportCompletedDialog(string folderPath)
        {
            try
            {
                var dialog = new ExportCompletedDialog(folderPath);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error showing export completed dialog: {ex.Message}");
                // Fallback to simple message box
                MessageBox.Show($"Export completed.\n\nLocation: {folderPath}", 
                              "Export Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

                    // Get paper size using SheetSizeDetector
                    string sheetSize = Utils.SheetSizeDetector.GetSheetSize(sheet);
                    
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
                    
                    WriteDebugLog($"Created SheetItem: Number='{sheetItem.SheetNumber}', Name='{sheetItem.SheetName}', Size='{sheetSize}'");
                    
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
                
                // NOTE: SelectionSummaryText removed from new Create tab design
                // Status is shown in DataGrid instead
                
                // Update format summary
                // NOTE: FormatSummaryText removed from new Create tab design  
                // Formats shown in Export Queue DataGrid instead
                
                // Refresh SelectedItemsForExport binding
                OnPropertyChanged(nameof(SelectedItemsForExport));
                
                WriteDebugLog($"Create tab summary updated: {totalSelected} items");
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
                
                // Update Export Queue for Create tab
                UpdateExportQueue();
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export +] ERROR in UpdateExportSummary: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Export Queue DataGrid based on selected sheets/views and formats
        /// </summary>
        private void UpdateExportQueue()
        {
            try
            {
                if (ExportQueueItems == null) return;

                ExportQueueItems.Clear();

                var selectedFormats = ExportSettings?.GetSelectedFormatsList() ?? new List<string>();
                if (selectedFormats.Count == 0)
                {
                    WriteDebugLog("No formats selected, Export Queue cleared");
                    return;
                }

                // Add selected sheets to queue
                if (Sheets != null)
                {
                    foreach (var sheet in Sheets.Where(s => s.IsSelected))
                    {
                        foreach (var format in selectedFormats)
                        {
                            // Determine display name: use CustomFileName if available, else SheetName
                            string displayName = sheet.SheetName;
                            if (!string.IsNullOrWhiteSpace(sheet.CustomFileName))
                            {
                                displayName = sheet.CustomFileName;
                            }
                            
                            var queueItem = new ExportQueueItem
                            {
                                IsSelected = true,
                                ViewSheetNumber = sheet.SheetNumber,
                                ViewSheetName = displayName,
                                Format = format.ToUpper(),
                                Size = GetSheetSize(sheet),
                                Orientation = GetSheetOrientation(sheet),
                                Progress = 0,
                                Status = "Pending"
                            };
                            ExportQueueItems.Add(queueItem);
                        }
                    }
                }

                // Add selected views to queue
                if (Views != null)
                {
                    foreach (var view in Views.Where(v => v.IsSelected))
                    {
                        foreach (var format in selectedFormats)
                        {
                            // Determine display name: use CustomFileName if available, else ViewName
                            string displayName = view.ViewName;
                            if (!string.IsNullOrWhiteSpace(view.CustomFileName))
                            {
                                displayName = view.CustomFileName;
                            }
                            
                            var queueItem = new ExportQueueItem
                            {
                                IsSelected = true,
                                ViewSheetNumber = view.ViewType,
                                ViewSheetName = displayName,
                                Format = format.ToUpper(),
                                Size = "-",
                                Orientation = "-",
                                Progress = 0,
                                Status = "Pending"
                            };
                            ExportQueueItems.Add(queueItem);
                        }
                    }
                }

                WriteDebugLog($"Export Queue updated: {ExportQueueItems.Count} items");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"ERROR in UpdateExportQueue: {ex.Message}");
            }
        }

        /// <summary>
        /// Get sheet size (paper size) from sheet
        /// </summary>
        private string GetSheetSize(SheetItem sheet)
        {
            try
            {
                if (sheet?.Id == null || _document == null) return "-";

                var revitSheet = _document.GetElement(sheet.Id) as ViewSheet;
                if (revitSheet == null) return "-";

                var titleBlock = new FilteredElementCollector(_document, revitSheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .FirstOrDefault();

                if (titleBlock != null)
                {
                    // Try to get paper size from title block
                    var widthParam = titleBlock.get_Parameter(BuiltInParameter.SHEET_WIDTH);
                    var heightParam = titleBlock.get_Parameter(BuiltInParameter.SHEET_HEIGHT);

                    if (widthParam != null && heightParam != null)
                    {
                        double width = widthParam.AsDouble() * 304.8; // Convert to mm
                        double height = heightParam.AsDouble() * 304.8;

                        // Match common paper sizes
                        if (Math.Abs(width - 841) < 10 && Math.Abs(height - 1189) < 10) return "A0";
                        if (Math.Abs(width - 594) < 10 && Math.Abs(height - 841) < 10) return "A1";
                        if (Math.Abs(width - 420) < 10 && Math.Abs(height - 594) < 10) return "A2";
                        if (Math.Abs(width - 297) < 10 && Math.Abs(height - 420) < 10) return "A3";
                        if (Math.Abs(width - 210) < 10 && Math.Abs(height - 297) < 10) return "A4";

                        return $"{width:F0}x{height:F0}mm";
                    }
                }

                // Fallback to sheet.Size property if available
                return sheet.Size ?? "Custom";
            }
            catch
            {
                return "-";
            }
        }

        /// <summary>
        /// Get sheet orientation (Portrait/Landscape)
        /// </summary>
        private string GetSheetOrientation(SheetItem sheet)
        {
            try
            {
                if (sheet?.Id == null || _document == null) return "-";

                var revitSheet = _document.GetElement(sheet.Id) as ViewSheet;
                if (revitSheet == null) return "-";

                var titleBlock = new FilteredElementCollector(_document, revitSheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .FirstOrDefault();

                if (titleBlock != null)
                {
                    var widthParam = titleBlock.get_Parameter(BuiltInParameter.SHEET_WIDTH);
                    var heightParam = titleBlock.get_Parameter(BuiltInParameter.SHEET_HEIGHT);

                    if (widthParam != null && heightParam != null)
                    {
                        double width = widthParam.AsDouble();
                        double height = heightParam.AsDouble();

                        return width > height ? "Landscape" : "Portrait";
                    }
                }

                return "-";
            }
            catch
            {
                return "-";
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
            if (sender is System.Windows.Controls.Primitives.ToggleButton button && button.Tag is string format)
            {
                WriteDebugLog($"[Export +] Format {format} checked via ToggleButton");
                ExportSettings?.SetFormatSelection(format, true);
                UpdateExportSummary();
            }
        }

        private void FormatToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton button && button.Tag is string format)
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
                        // Show export completed dialog with Open Folder button
                        ShowExportCompletedDialog(outputPath);
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
                // Auto-filter if checkbox is checked
                if (FilterByVSCheckBox?.IsChecked == true)
                {
                    FilterSheetsBySet(item.Content.ToString());
                }
            }
        }

        private void FilterByVSCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Filter by V/S checkbox checked - enabling filter");
            // Apply filter based on current combo selection
            if (ViewSheetSetCombo?.SelectedItem is ComboBoxItem item)
            {
                FilterSheetsBySet(item.Content.ToString());
            }
        }

        private void FilterByVSCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Filter by V/S checkbox unchecked - showing all items");
            // Reset to show all sheets/views
            ResetFilter_Click(sender, e);
        }

        private void SaveVSSet_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Save View/Sheet Set clicked");
            
            int count = 0;
            if (SheetsRadio.IsChecked == true)
            {
                count = Sheets?.Where(s => s.IsSelected).Count() ?? 0;
            }
            else
            {
                count = Views?.Where(v => v.IsSelected).Count() ?? 0;
            }
            
            MessageBox.Show($"Save View/Sheet Set with {count} selected items", 
                           "Save V/S Set", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox searchBox = sender as TextBox;
            string searchText = searchBox?.Text?.ToLower() ?? "";
            WriteDebugLog($"Search text changed: '{searchText}'");
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all items when search is empty
                if (SheetsDataGrid.Visibility == System.Windows.Visibility.Visible)
                {
                    SheetsDataGrid.ItemsSource = Sheets;
                }
                else if (ViewsDataGrid.Visibility == System.Windows.Visibility.Visible)
                {
                    ViewsDataGrid.ItemsSource = Views;
                }
            }
            else
            {
                // Filter based on current view
                if (SheetsDataGrid.Visibility == System.Windows.Visibility.Visible && Sheets != null)
                {
                    var filtered = Sheets.Where(s => 
                        (s.SheetNumber?.ToLower().Contains(searchText) ?? false) ||
                        (s.SheetName?.ToLower().Contains(searchText) ?? false) ||
                        (s.CustomFileName?.ToLower().Contains(searchText) ?? false)
                    ).ToList();
                    
                    SheetsDataGrid.ItemsSource = filtered;
                    WriteDebugLog($"Filtered sheets: {filtered.Count} of {Sheets.Count}");
                }
                else if (ViewsDataGrid.Visibility == System.Windows.Visibility.Visible && Views != null)
                {
                    var filtered = Views.Where(v => 
                        (v.ViewName?.ToLower().Contains(searchText) ?? false) ||
                        (v.ViewType?.ToLower().Contains(searchText) ?? false) ||
                        (v.CustomFileName?.ToLower().Contains(searchText) ?? false)
                    ).ToList();
                    
                    ViewsDataGrid.ItemsSource = filtered;
                    WriteDebugLog($"Filtered views: {filtered.Count} of {Views.Count}");
                }
            }
            
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

        #region Profile Manager Methods - MOVED TO ProSheetsMainWindow.Profiles.cs

        // All Profile management methods have been moved to ProSheetsMainWindow.Profiles.cs
        // This includes:
        // - InitializeProfiles()
        // - OnProfileChanged()
        // - ApplyProfileToUI()
        // - SaveCurrentSettingsToProfile()
        // - ProfileComboBox_SelectionChanged()
        // - AddProfile_Click()
        // - SaveProfile_Click()
        // - DeleteProfile_Click()

        #endregion

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

        // ApplyTemplate_Click temporarily disabled - requires refactoring with new Profile system
        /*
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
        */

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

        #region Create Tab Event Handlers

        private void LearnMore_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) 
                { 
                    UseShellExecute = true 
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error opening link: {ex.Message}");
            }
        }

        private void SetPaperSizeButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Set Paper Size clicked");
            
            // Get selected items from ExportQueueDataGrid
            if (ExportQueueDataGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one item to set paper size.", 
                               "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show paper size options dialog
            var paperSizes = new[] { "A0", "A1", "A2", "A3", "A4", "Letter", "Tabloid", "Custom" };
            var selectedSize = "A3"; // Default

            // For now, just show a simple message
            // TODO: Implement proper paper size selection dialog
            MessageBox.Show($"Set Paper Size to {selectedSize} for {ExportQueueDataGrid.SelectedItems.Count} item(s).", 
                           "Paper Size", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetOrientationButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Set Orientation clicked");
            
            // Get selected items from ExportQueueDataGrid
            if (ExportQueueDataGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one item to set orientation.", 
                               "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show orientation options dialog
            var result = MessageBox.Show("Set orientation to Portrait (Yes) or Landscape (No)?", 
                                        "Set Orientation", 
                                        MessageBoxButton.YesNoCancel, 
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Set to Portrait
                foreach (var item in ExportQueueDataGrid.SelectedItems)
                {
                    if (item is ExportQueueItem queueItem)
                    {
                        queueItem.Orientation = "Portrait";
                    }
                }
                WriteDebugLog($"Set {ExportQueueDataGrid.SelectedItems.Count} items to Portrait");
            }
            else if (result == MessageBoxResult.No)
            {
                // Set to Landscape
                foreach (var item in ExportQueueDataGrid.SelectedItems)
                {
                    if (item is ExportQueueItem queueItem)
                    {
                        queueItem.Orientation = "Landscape";
                    }
                }
                WriteDebugLog($"Set {ExportQueueDataGrid.SelectedItems.Count} items to Landscape");
            }
        }

        private void ScheduleToggle_Checked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Schedule Toggle ON");
            if (ScheduleSettingsPanel != null)
            {
                ScheduleSettingsPanel.Visibility = System.Windows.Visibility.Visible;
            }
            if (ScheduleStatusText != null)
            {
                ScheduleStatusText.Text = "The Scheduling Assistant is on.";
                ScheduleStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Green);
            }
            
            // Initialize date picker to today if not set
            if (StartingDatePicker != null && !StartingDatePicker.SelectedDate.HasValue)
            {
                StartingDatePicker.SelectedDate = DateTime.Now;
            }
            
            // Initialize time combobox to current hour if not selected
            if (TimeComboBox != null && TimeComboBox.SelectedIndex < 0)
            {
                var currentHour = DateTime.Now.Hour;
                var ampm = currentHour >= 12 ? "PM" : "AM";
                var hour12 = currentHour % 12;
                if (hour12 == 0) hour12 = 12;
                var timeString = $"{hour12:00}:00 {ampm}";
                
                // Try to find matching time in combobox
                for (int i = 0; i < TimeComboBox.Items.Count; i++)
                {
                    if (TimeComboBox.Items[i] is ComboBoxItem item && 
                        item.Content.ToString() == timeString)
                    {
                        TimeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void ScheduleToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Schedule Toggle OFF");
            if (ScheduleSettingsPanel != null)
            {
                ScheduleSettingsPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
            if (ScheduleStatusText != null)
            {
                ScheduleStatusText.Text = "The Scheduling Assistant is off.";
                ScheduleStatusText.Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#666666"));
            }
        }

        private void RepeatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DaysOfWeekPanel == null || RepeatComboBox == null) return;

            // Show days of week panel only when "Weekly" is selected
            var selectedItem = RepeatComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Content?.ToString() == "Weekly")
            {
                DaysOfWeekPanel.Visibility = System.Windows.Visibility.Visible;
                WriteDebugLog("Days of week panel shown (Weekly repeat selected)");
            }
            else
            {
                DaysOfWeekPanel.Visibility = System.Windows.Visibility.Collapsed;
                WriteDebugLog($"Days of week panel hidden ({selectedItem?.Content} repeat selected)");
            }
        }

        private void RefreshScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Refresh Schedule clicked");
            
            // Refresh schedule settings display
            if (ScheduleToggle.IsChecked == true)
            {
                var date = StartingDatePicker.SelectedDate?.ToString("d") ?? "Not set";
                var time = (TimeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Not set";
                var repeat = (RepeatComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Does not repeat";
                
                var message = $"Current Schedule Settings:\n\n" +
                             $"Date: {date}\n" +
                             $"Time: {time}\n" +
                             $"Repeat: {repeat}";
                
                if (repeat == "Weekly")
                {
                    var days = new System.Text.StringBuilder();
                    if (MondayCheck.IsChecked == true) days.Append("Mon ");
                    if (TuesdayCheck.IsChecked == true) days.Append("Tue ");
                    if (WednesdayCheck.IsChecked == true) days.Append("Wed ");
                    if (ThursdayCheck.IsChecked == true) days.Append("Thu ");
                    if (FridayCheck.IsChecked == true) days.Append("Fri ");
                    if (SaturdayCheck.IsChecked == true) days.Append("Sat ");
                    if (SundayCheck.IsChecked == true) days.Append("Sun ");
                    
                    if (days.Length > 0)
                    {
                        message += $"\nDays: {days}";
                    }
                }
                
                MessageBox.Show(message, "Schedule Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Scheduling Assistant is currently off. Turn it on to configure schedule settings.", 
                               "Schedule Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void StartExportButton_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Start Export clicked");
            
            try
            {
                // Validate output folder
                if (string.IsNullOrEmpty(CreateFolderPathTextBox?.Text))
                {
                    MessageBox.Show("Please select an output folder.", 
                                   "No Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate export queue has items
                if (ExportQueueDataGrid.Items.Count == 0)
                {
                    MessageBox.Show("Export queue is empty. Please select items to export from the Selection tab.", 
                                   "Empty Queue", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Disable button during export
                StartExportButton.IsEnabled = false;
                StartExportButton.Content = "EXPORTING...";
                
                // Reset progress
                ExportProgressBar.Value = 0;
                ProgressPercentageText.Text = "Completed 0%";

                // Check if scheduling is enabled
                if (ScheduleToggle.IsChecked == true)
                {
                    // Schedule for later
                    var scheduleDate = StartingDatePicker.SelectedDate ?? DateTime.Now;
                    var scheduleTime = (TimeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "12:00 PM";
                    
                    MessageBox.Show($"Export scheduled for {scheduleDate:d} at {scheduleTime}.\n\n" +
                                   "The export will run automatically at the scheduled time.", 
                                   "Export Scheduled", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    WriteDebugLog($"Export scheduled for {scheduleDate:d} {scheduleTime}");
                }
                else
                {
                    // Export immediately
                    WriteDebugLog("Starting immediate export");
                    
                    var items = ExportQueueDataGrid.Items.Cast<ExportQueueItem>().ToList();
                    var totalItems = items.Count;
                    
                    // Get selected sheets from Selection tab with custom file names
                    var selectedSheets = Sheets.Where(s => s.IsSelected).ToList();
                    
                    if (selectedSheets.Count == 0)
                    {
                        MessageBox.Show("No sheets selected for export.", 
                                       "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Get selected formats
                    var selectedFormats = ExportSettings?.GetSelectedFormatsList() ?? new List<string>();
                    
                    if (selectedFormats.Count == 0)
                    {
                        MessageBox.Show("Please select at least one export format in the Format tab.", 
                                       "No Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    WriteDebugLog($"Exporting {selectedSheets.Count} sheets in {selectedFormats.Count} format(s)");
                    
                    int completedCount = 0;
                    string outputFolder = CreateFolderPathTextBox.Text;

                    // Export for each selected format
                    foreach (var format in selectedFormats)
                    {
                        WriteDebugLog($"Starting export for format: {format}");
                        
                        if (format.ToUpper() == "PDF")
                        {
                            // Use PDF Export External Event for proper API context
                            if (_pdfExportEvent != null && _pdfExportHandler != null)
                            {
                                WriteDebugLog("Using PDF Export External Event...");
                                
                                // CRITICAL: Update ExportSettings from UI controls BEFORE export
                                UpdateExportSettingsFromUI();
                                
                                // Set export parameters
                                _pdfExportHandler.Document = _document;
                                _pdfExportHandler.SheetItems = selectedSheets;
                                _pdfExportHandler.OutputFolder = outputFolder;
                                _pdfExportHandler.Settings = ExportSettings;
                                _pdfExportHandler.ProgressCallback = (current, total, sheetNumber, isFileCompleted) =>
                                {
                                    // Update UI on dispatcher thread
                                    Dispatcher.Invoke(() =>
                                    {
                                        // Find corresponding item in queue
                                        var queueItem = items.FirstOrDefault(i => 
                                            i.ViewSheetNumber == sheetNumber && 
                                            i.Format == format.ToUpper());
                                        
                                        if (queueItem != null)
                                        {
                                            if (isFileCompleted)
                                            {
                                                // File has been created and renamed - mark as completed
                                                queueItem.Status = "Completed";
                                                queueItem.Progress = 100;
                                                WriteDebugLog($"✓ Sheet {sheetNumber} - File created successfully");
                                            }
                                            else
                                            {
                                                // Export started but file not yet completed
                                                queueItem.Status = "Processing";
                                                queueItem.Progress = (current * 100.0) / total;
                                                WriteDebugLog($"⏳ Sheet {sheetNumber} - Exporting... {current}/{total}");
                                            }
                                        }
                                        
                                        // Update overall progress only when files are completed
                                        if (isFileCompleted)
                                        {
                                            completedCount++;
                                            var overallProgress = (completedCount * 100.0) / totalItems;
                                            ExportProgressBar.Value = overallProgress;
                                            ProgressPercentageText.Text = $"Completed {overallProgress:F0}%";
                                        }
                                        
                                        WriteDebugLog($"Progress: {current}/{total} - {sheetNumber} - Completed: {isFileCompleted}");
                                    });
                                };
                                
                                // Raise the external event to run export in API context
                                var raiseResult = _pdfExportEvent.Raise();
                                WriteDebugLog($"PDF Export Event raised with result: {raiseResult}");
                                
                                // Wait for export to complete (with timeout)
                                int waitCount = 0;
                                while (raiseResult == ExternalEventRequest.Pending && waitCount < 100)
                                {
                                    System.Threading.Thread.Sleep(100);
                                    waitCount++;
                                }
                                
                                bool exportResult = _pdfExportHandler.ExportResult;
                                
                                if (exportResult)
                                {
                                    WriteDebugLog("PDF export completed successfully");
                                }
                                else
                                {
                                    WriteDebugLog($"PDF export failed: {_pdfExportHandler.ErrorMessage}");
                                }
                            }
                            else
                            {
                                WriteDebugLog("ERROR: PDF Export Event not initialized (UIApplication is null)");
                                MessageBox.Show("Cannot export PDF: External Event not initialized.\n\nPlease restart Revit and try again.",
                                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else if (format.ToUpper() == "DWG")
                        {
                            WriteDebugLog("DWG export not yet implemented");
                            // TODO: Implement DWG export with custom names
                        }
                        else if (format.ToUpper() == "IFC")
                        {
                            WriteDebugLog("IFC export not yet implemented");
                            // TODO: Implement IFC export with custom names
                        }
                        else
                        {
                            WriteDebugLog($"Format {format} not yet implemented");
                        }
                    }
                    
                    // Mark any remaining items as completed
                    foreach (var item in items.Where(i => i.Status != "Completed"))
                    {
                        item.Status = "Completed";
                        item.Progress = 100;
                    }
                    
                    // Final progress update
                    ExportProgressBar.Value = 100;
                    ProgressPercentageText.Text = "Completed 100%";
                    
                    // Generate report if selected
                    var reportType = (ReportComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (reportType != "Don't Save Report")
                    {
                        WriteDebugLog($"Generating {reportType}");
                        // TODO: Implement report generation
                    }
                    
                    // Show export completed dialog with Open Folder button
                    ShowExportCompletedDialog(CreateFolderPathTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in StartExportButton_Click: {ex.Message}");
                MessageBox.Show($"Error during export: {ex.Message}", 
                               "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                StartExportButton.IsEnabled = true;
                StartExportButton.Content = "START EXPORT";
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

        private void EditSelectedFilenames_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Edit Selected Filenames button clicked - Opening CustomFileNameDialog");
            
            try
            {
                bool isSheetMode = SheetsRadio?.IsChecked == true;
                
                if (isSheetMode)
                {
                    // Get selected sheets
                    var selectedSheets = Sheets?.Where(s => s.IsSelected).ToList();
                    
                    if (selectedSheets == null || !selectedSheets.Any())
                    {
                        MessageBox.Show("Please select at least one sheet first.", "No Selection", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    // Open CustomFileNameDialog
                    var dialog = new CustomFileNameDialog(_document);
                    dialog.Owner = this;
                    
                    if (dialog.ShowDialog() == true)
                    {
                        // Apply custom file name configuration to selected sheets
                        int updatedCount = ApplyCustomFileNameToSheets(selectedSheets, dialog.SelectedParameters);
                        
                        WriteDebugLog($"Updated {updatedCount} sheets with custom filename configuration");
                        
                        // IMPORTANT: Update Export Queue to reflect new custom names
                        UpdateExportQueue();
                        WriteDebugLog("Export Queue refreshed with updated custom file names");
                        
                        MessageBox.Show($"Successfully applied custom filename to {updatedCount} sheet(s).", 
                                       "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Get selected views
                    var selectedViews = Views?.Where(v => v.IsSelected).ToList();
                    
                    if (selectedViews == null || !selectedViews.Any())
                    {
                        MessageBox.Show("Please select at least one view first.", "No Selection", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    // Open CustomFileNameDialog
                    var dialog = new CustomFileNameDialog(_document);
                    dialog.Owner = this;
                    
                    if (dialog.ShowDialog() == true)
                    {
                        // Apply custom file name configuration to selected views
                        int updatedCount = ApplyCustomFileNameToViews(selectedViews, dialog.SelectedParameters);
                        
                        WriteDebugLog($"Updated {updatedCount} views with custom filename configuration");
                        
                        // IMPORTANT: Update Export Queue to reflect new custom names
                        UpdateExportQueue();
                        WriteDebugLog("Export Queue refreshed with updated custom file names");
                        
                        MessageBox.Show($"Successfully applied custom filename to {updatedCount} view(s).", 
                                       "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error editing selected filenames: {ex.Message}");
                MessageBox.Show($"Error editing filenames: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Apply custom filename configuration to sheets
        /// </summary>
        private int ApplyCustomFileNameToSheets(List<SheetItem> sheets, ObservableCollection<SelectedParameterInfo> parameters)
        {
            int count = 0;
            
            foreach (var sheetItem in sheets)
            {
                try
                {
                    // Get the actual ViewSheet element
                    var sheet = _document.GetElement(sheetItem.Id) as ViewSheet;
                    if (sheet == null) continue;
                    
                    // Generate custom filename from parameters
                    string customFileName = GenerateCustomFileName(sheet, parameters);
                    
                    if (!string.IsNullOrWhiteSpace(customFileName))
                    {
                        sheetItem.CustomFileName = customFileName;
                        count++;
                        WriteDebugLog($"Sheet '{sheet.SheetNumber}' - Custom filename: {customFileName}");
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"Error applying custom filename to sheet: {ex.Message}");
                }
            }
            
            return count;
        }

        /// <summary>
        /// Apply custom filename configuration to views
        /// </summary>
        private int ApplyCustomFileNameToViews(List<ViewItem> views, ObservableCollection<SelectedParameterInfo> parameters)
        {
            int count = 0;
            
            foreach (var viewItem in views)
            {
                try
                {
                    // Get the actual View element
                    var view = _document.GetElement(viewItem.ViewId) as View;
                    if (view == null) continue;
                    
                    // Generate custom filename from parameters
                    string customFileName = GenerateCustomFileNameFromView(view, parameters);
                    
                    if (!string.IsNullOrWhiteSpace(customFileName))
                    {
                        viewItem.CustomFileName = customFileName;
                        count++;
                        WriteDebugLog($"View '{view.Name}' - Custom filename: {customFileName}");
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"Error applying custom filename to view: {ex.Message}");
                }
            }
            
            return count;
        }

        /// <summary>
        /// Generate custom filename from ViewSheet parameters
        /// </summary>
        private string GenerateCustomFileName(ViewSheet sheet, ObservableCollection<SelectedParameterInfo> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;
            
            var parts = new List<string>();
            
            foreach (var paramConfig in parameters)
            {
                string value = GetSheetParameterValue(sheet, paramConfig.ParameterName);
                
                if (!string.IsNullOrEmpty(value))
                {
                    string part = $"{paramConfig.Prefix}{value}{paramConfig.Suffix}";
                    parts.Add(part);
                }
            }
            
            string separator = parameters.FirstOrDefault()?.Separator ?? "-";
            return string.Join(separator, parts);
        }

        /// <summary>
        /// Generate custom filename from View parameters
        /// </summary>
        private string GenerateCustomFileNameFromView(View view, ObservableCollection<SelectedParameterInfo> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;
            
            var parts = new List<string>();
            
            foreach (var paramConfig in parameters)
            {
                string value = GetViewParameterValue(view, paramConfig.ParameterName);
                
                if (!string.IsNullOrEmpty(value))
                {
                    string part = $"{paramConfig.Prefix}{value}{paramConfig.Suffix}";
                    parts.Add(part);
                }
            }
            
            string separator = parameters.FirstOrDefault()?.Separator ?? "-";
            return string.Join(separator, parts);
        }

        /// <summary>
        /// Get parameter value from ViewSheet
        /// </summary>
        private string GetSheetParameterValue(ViewSheet sheet, string parameterName)
        {
            try
            {
                // Try built-in parameters first
                switch (parameterName)
                {
                    case "Sheet Number":
                        return sheet.SheetNumber;
                    case "Sheet Name":
                        return sheet.Name;
                    case "Current Revision":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION)?.AsString() ?? "";
                    case "Current Revision Date":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION_DATE)?.AsString() ?? "";
                    case "Current Revision Description":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION_DESCRIPTION)?.AsString() ?? "";
                    case "Approved By":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_APPROVED_BY)?.AsString() ?? "";
                    case "Checked By":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_CHECKED_BY)?.AsString() ?? "";
                    case "Designed By":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY)?.AsString() ?? "";
                    case "Drawn By":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_DRAWN_BY)?.AsString() ?? "";
                    case "Sheet Issue Date":
                        return sheet.get_Parameter(BuiltInParameter.SHEET_ISSUE_DATE)?.AsString() ?? "";
                }
                
                // Try to find parameter by name
                foreach (Parameter param in sheet.Parameters)
                {
                    if (param.Definition.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetParameterValueAsString(param);
                    }
                }
                
                // Try project information parameters
                var projectInfo = _document.ProjectInformation;
                if (projectInfo != null)
                {
                    foreach (Parameter param in projectInfo.Parameters)
                    {
                        if (param.Definition.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                        {
                            return GetParameterValueAsString(param);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error getting parameter '{parameterName}': {ex.Message}");
            }
            
            return "";
        }

        /// <summary>
        /// Get parameter value from View
        /// </summary>
        private string GetViewParameterValue(View view, string parameterName)
        {
            try
            {
                // Try built-in parameters first
                switch (parameterName)
                {
                    case "View Name":
                        return view.Name;
                    case "View Template":
                        var templateId = view.ViewTemplateId;
                        if (templateId != ElementId.InvalidElementId)
                        {
                            var template = _document.GetElement(templateId);
                            return template?.Name ?? "";
                        }
                        return "";
                }
                
                // Try to find parameter by name
                foreach (Parameter param in view.Parameters)
                {
                    if (param.Definition.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetParameterValueAsString(param);
                    }
                }
                
                // Try project information parameters
                var projectInfo = _document.ProjectInformation;
                if (projectInfo != null)
                {
                    foreach (Parameter param in projectInfo.Parameters)
                    {
                        if (param.Definition.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                        {
                            return GetParameterValueAsString(param);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error getting view parameter '{parameterName}': {ex.Message}");
            }
            
            return "";
        }

        /// <summary>
        /// Get parameter value as string regardless of storage type
        /// </summary>
        private string GetParameterValueAsString(Parameter param)
        {
            if (param == null || !param.HasValue)
                return "";
            
            switch (param.StorageType)
            {
                case StorageType.String:
                    return param.AsString() ?? "";
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                case StorageType.Double:
                    return param.AsValueString() ?? param.AsDouble().ToString();
                case StorageType.ElementId:
                    var elemId = param.AsElementId();
                    if (elemId != ElementId.InvalidElementId)
                    {
                        var elem = _document.GetElement(elemId);
                        return elem?.Name ?? "";
                    }
                    return "";
                default:
                    return "";
            }
        }
        
        private string PromptForFilename(string title, string defaultValue)
        {
            // Create a simple WPF dialog
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };
            
            var grid = new WpfGrid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var textBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14
            };
            WpfGrid.SetRow(textBox, 0);
            grid.Children.Add(textBox);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            
            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            WpfGrid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);
            
            dialog.Content = grid;
            
            textBox.Focus();
            textBox.SelectAll();
            
            return dialog.ShowDialog() == true ? textBox.Text : null;
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

        #endregion

        #region IFC Settings Event Handlers (DISABLED - WPF Build Issue)

        // NOTE: These methods are commented out due to WPF temporary assembly validation issues
        // The .g.cs file containing x:Name field declarations is not generated until AFTER
        // the temporary assembly compilation succeeds, creating a circular dependency.
        // 
        // SOLUTION: Implement Browse button functionality using:
        // 1. Behaviors/Attached Properties (no code-behind references)
        // 2. MVVM pattern with Commands
        // 3. Post-deployment event wiring (outside WPF build process)

        /*
        /// <summary>
        /// Wire up Browse button Click handlers in constructor
        /// This avoids WPF XAML compilation issues with x:Name controls
        /// </summary>
        private void WireUpIFCBrowseButtons()
        {
            try
            {
                if (BrowseUserPsetsButtonIFC != null)
                {
                    BrowseUserPsetsButtonIFC.Click += BrowseIFCFile_Click;
                }
                
                if (BrowseParamMappingButtonIFC != null)
                {
                    BrowseParamMappingButtonIFC.Click += BrowseIFCFile_Click;
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error wiring up IFC Browse buttons: {ex.Message}");
            }
        }

        /// <summary>
        /// Universal Browse button click handler for IFC file selection
        /// Uses Button.Tag to determine which TextBox to update
        /// </summary>
        private void BrowseIFCFile_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;

            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select IFC Configuration File",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    CheckFileExists = false
                };

                // Determine which TextBox to update based on Button.Tag
                TextBox targetTextBox = null;
                string fileType = button.Tag?.ToString() ?? "";

                if (fileType == "UserPsets")
                {
                    dialog.Title = "Select User-Defined Property Sets File";
                    targetTextBox = UserPsetsPathTextBoxIFC;
                }
                else if (fileType == "ParamMapping")
                {
                    dialog.Title = "Select Parameter Mapping Table File";
                    targetTextBox = ParamMappingPathTextBoxIFC;
                }

                if (targetTextBox == null)
                {
                    WriteDebugLog($"ERROR: Could not find target TextBox for button Tag='{fileType}'");
                    return;
                }

                // Set initial directory if path exists
                string currentPath = targetTextBox.Text;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    var directory = System.IO.Path.GetDirectoryName(currentPath);
                    if (!string.IsNullOrEmpty(directory) && System.IO.Directory.Exists(directory))
                    {
                        dialog.InitialDirectory = directory;
                    }
                }

                if (dialog.ShowDialog() == true)
                {
                    targetTextBox.Text = dialog.FileName;
                    WriteDebugLog($"IFC file selected ({fileType}): {dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error in BrowseIFCFile_Click: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error selecting file: {ex.Message}",
                    "Browse Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        */

        #endregion IFC Settings Event Handlers
    }
}