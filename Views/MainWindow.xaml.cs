using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ProSheetsAddin.Views
{
    public partial class MainWindow : Window
    {
        private ExternalCommandData _commandData;
        private Document _document;
        private List<ViewSheet> _allSheets;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetRevitData(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;
            LoadSheets();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSheets();
            LoadProfiles();
            LoadDefaultSettings();
            UpdateFileNamePreview();
        }

        private void LoadSheets()
        {
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_document);
                var sheets = collector.OfClass(typeof(ViewSheet)).Cast<ViewSheet>();

                _allSheets.Clear();

                foreach (var sheet in sheets)
                {
                    var sheetItem = new SheetItem
                    {
                        ElementId = sheet.Id,
                        SheetNumber = sheet.SheetNumber ?? "",
                        SheetName = sheet.Name ?? "",
                        IsSelected = true
                    };

                    // Lấy revision
                    Parameter revParam = sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION);
                    sheetItem.Revision = revParam?.AsString() ?? "";

                    // Phát hiện paper size
                    var paperSizeManager = new PaperSizeManager();
                    var paperSize = paperSizeManager.DetectPaperSize(sheet);
                    sheetItem.PaperSize = paperSize.Name;

                    _allSheets.Add(sheetItem);
                }

                SheetsDataGrid.ItemsSource = _allSheets;
                UpdateSheetSelection();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi load sheets: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProfiles()
        {
            try
            {
                var profileManager = new ProfileManager();
                var profiles = profileManager.GetAvailableProfiles();
                
                cmbProfiles.Items.Clear();
                foreach (string profile in profiles)
                {
                    cmbProfiles.Items.Add(profile);
                }

                if (profiles.Contains("Default"))
                {
                    cmbProfiles.SelectedItem = "Default";
                }
                else if (profiles.Count > 0)
                {
                    cmbProfiles.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi load profiles: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDefaultSettings()
        {
            OutputFolderBox.Text = _currentSettings.OutputFolder;
            
            // Load UI từ settings
            chkExportPDF.IsChecked = _currentSettings.ExportPDF;
            chkExportDWG.IsChecked = _currentSettings.ExportDWG;
            chkExportDGN.IsChecked = _currentSettings.ExportDGN;
            chkExportDWF.IsChecked = _currentSettings.ExportDWF;
            chkExportNWC.IsChecked = _currentSettings.ExportNWC;
            chkExportIFC.IsChecked = _currentSettings.ExportIFC;
            chkExportImage.IsChecked = _currentSettings.ExportImage;
            chkExportXML.IsChecked = _currentSettings.ExportXML;
            
            txtFileNameTemplate.Text = _currentSettings.FileNameTemplate;
            chkIncludeRevision.IsChecked = _currentSettings.IncludeRevision;
            chkIncludeDate.IsChecked = _currentSettings.IncludeDate;
            chkReplaceSpaces.IsChecked = _currentSettings.ReplaceSpacesWithUnderscore;
            
            chkCombinePDFs.IsChecked = _currentSettings.CombinePDFs;
            chkUsePDF24.IsChecked = _currentSettings.UsePDF24;
            txtPDF24Path.Text = _currentSettings.PDF24Path;
            
            chkCreateSubfolders.IsChecked = _currentSettings.CreateSubfolders;
            txtSubfolderTemplate.Text = _currentSettings.SubfolderTemplate;
            
            chkAutoDetectPaperSize.IsChecked = _currentSettings.AutoDetectPaperSize;
            txtPaperSizeTolerance.Text = _currentSettings.PaperSizeTolerance.ToString();
        }

        private void BrowseFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục để xuất files";
                dialog.SelectedPath = OutputFolderBox.Text;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputFolderBox.Text = dialog.SelectedPath;
                    _currentSettings.OutputFolder = dialog.SelectedPath;
                }
            }
        }

        private void ChkSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var sheet in _allSheets)
            {
                sheet.IsSelected = true;
            }
            SheetsDataGrid.Items.Refresh();
        }

        private void ChkSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var sheet in _allSheets)
            {
                sheet.IsSelected = false;
            }
            SheetsDataGrid.Items.Refresh();
        }

        private void UpdateSheetSelection()
        {
            int selectedCount = _allSheets.Count(s => s.IsSelected);
            int totalCount = _allSheets.Count;
            
            if (selectedCount == 0)
            {
                chkSelectAll.IsChecked = false;
            }
            else if (selectedCount == totalCount)
            {
                chkSelectAll.IsChecked = true;
            }
            else
            {
                chkSelectAll.IsChecked = null; // Indeterminate state
            }
        }

        private void UpdateFileNamePreview()
        {
            try
            {
                if (_allSheets.Count > 0)
                {
                    var firstSheet = _allSheets.First();
                    // Simple preview generation without using actual ViewSheet
                    string preview = txtFileNameTemplate.Text
                        .Replace("{SheetNumber}", firstSheet.SheetNumber)
                        .Replace("{SheetName}", firstSheet.SheetName)
                        .Replace("{ProjectNumber}", "PROJ001")
                        .Replace("{Revision}", "A")
                        .Replace("{Date}", DateTime.Now.ToString("yyyy-MM-dd"))
                        + ".pdf";
                    txtFileNamePreview.Text = FileNameGenerator.SanitizeFileName(preview);
                }
            }
            catch (Exception ex)
            {
                txtFileNamePreview.Text = "Lỗi preview: " + ex.Message;
            }
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedSheets = GetSelectedSheets();
                if (selectedSheets.Count == 0)
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một sheet để preview.", 
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show simple preview message for now
                var settings = GetExportSettings();
                string message = $"Preview:\n" +
                    $"Selected Sheets: {selectedSheets.Count}\n" +
                    $"Output Folder: {settings.OutputFolder}\n" +
                    $"Formats: ";
                
                var formats = new List<string>();
                if (settings.ExportPDF) formats.Add("PDF");
                if (settings.ExportDWG) formats.Add("DWG");
                if (settings.ExportImage) formats.Add("Image");
                
                message += string.Join(", ", formats);
                
                System.Windows.MessageBox.Show(message, "Export Preview", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi preview: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedSheets = GetSelectedSheets();
                if (selectedSheets.Count == 0)
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một sheet để xuất.", 
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var settings = GetExportSettings();
                var validationErrors = settings.Validate();
                
                if (validationErrors.Count > 0)
                {
                    string errorMessage = "Có lỗi trong cài đặt:\n" + string.Join("\n", validationErrors);
                    System.Windows.MessageBox.Show(errorMessage, "Lỗi cài đặt", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi xuất: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TransmittalBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedSheets = GetSelectedSheets();
                if (selectedSheets.Count == 0)
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một sheet để tạo transmittal.", 
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show simple transmittal info for now  
                string message = $"Drawing Transmittal would be generated for:\n\n" +
                    $"Selected Sheets: {selectedSheets.Count}\n" +
                    $"Project: {_document.Title}\n\n" +
                    $"This feature will create Excel transmittal with:\n" +
                    $"- Sheet numbers and names\n" +
                    $"- Current revisions\n" +
                    $"- Issue dates\n" +
                    $"- Project information";
                
                System.Windows.MessageBox.Show(message, "Drawing Transmittal", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi tạo transmittal: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public List<ViewSheet> GetSelectedSheets()
        {
            var selectedSheets = new List<ViewSheet>();
            
            foreach (var sheetItem in _allSheets.Where(s => s.IsSelected))
            {
                var sheet = _document.GetElement(sheetItem.ElementId) as ViewSheet;
                if (sheet != null)
                {
                    selectedSheets.Add(sheet);
                }
            }
            
            return selectedSheets;
        }

        public ExportSettings GetExportSettings()
        {
            _currentSettings.OutputFolder = OutputFolderBox.Text;
            
            // Export formats
            _currentSettings.ExportPDF = chkExportPDF.IsChecked ?? false;
            _currentSettings.ExportDWG = chkExportDWG.IsChecked ?? false;
            _currentSettings.ExportDGN = chkExportDGN.IsChecked ?? false;
            _currentSettings.ExportDWF = chkExportDWF.IsChecked ?? false;
            _currentSettings.ExportNWC = chkExportNWC.IsChecked ?? false;
            _currentSettings.ExportIFC = chkExportIFC.IsChecked ?? false;
            _currentSettings.ExportImage = chkExportImage.IsChecked ?? false;
            _currentSettings.ExportXML = chkExportXML.IsChecked ?? false;
            
            // File naming
            _currentSettings.FileNameTemplate = txtFileNameTemplate.Text;
            _currentSettings.IncludeRevision = chkIncludeRevision.IsChecked ?? false;
            _currentSettings.IncludeDate = chkIncludeDate.IsChecked ?? false;
            _currentSettings.ReplaceSpacesWithUnderscore = chkReplaceSpaces.IsChecked ?? false;
            
            // PDF settings
            _currentSettings.CombinePDFs = chkCombinePDFs.IsChecked ?? false;
            _currentSettings.UsePDF24 = chkUsePDF24.IsChecked ?? false;
            _currentSettings.PDF24Path = txtPDF24Path.Text;
            
            // Subfolder settings
            _currentSettings.CreateSubfolders = chkCreateSubfolders.IsChecked ?? false;
            _currentSettings.SubfolderTemplate = txtSubfolderTemplate.Text;
            
            // Paper size settings
            _currentSettings.AutoDetectPaperSize = chkAutoDetectPaperSize.IsChecked ?? false;
            if (double.TryParse(txtPaperSizeTolerance.Text, out double tolerance))
            {
                _currentSettings.PaperSizeTolerance = tolerance;
            }
            
            return _currentSettings;
        }
    }

    // Helper classes
    public class SheetItem
    {
        public ElementId ElementId { get; set; }
        public bool IsSelected { get; set; }
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string Revision { get; set; }
        public string PaperSize { get; set; }
    }

    // Simple class thay cho DummyViewSheet
    public class SimpleSheet
    {
        public string SheetNumber { get; set; }
        public string Name { get; set; }
    }
}