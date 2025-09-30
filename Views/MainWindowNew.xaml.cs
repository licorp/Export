using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        private void LoadSheets()
        {
            _allSheets = new FilteredElementCollector(_document)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsTemplate)
                .OrderBy(s => s.SheetNumber)
                .ToList();

            // Tạo datagrid items với thông tin chi tiết
            var sheetItems = _allSheets.Select(s => new SheetItem
            {
                Sheet = s,
                IsSelected = true,
                SheetNumber = s.SheetNumber ?? "",
                SheetName = s.Name ?? "",
                Revision = GetSheetRevision(s),
                PaperSize = GetSheetPaperSize(s)
            }).ToList();

            SheetsDataGrid.ItemsSource = sheetItems;
            UpdateSheetCount();
        }

        private string GetSheetRevision(ViewSheet sheet)
        {
            try
            {
                var revParam = sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION);
                return revParam?.AsString() ?? "No Revision";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetSheetPaperSize(ViewSheet sheet)
        {
            try
            {
                // Cố gắng lấy thông tin paper size từ title block
                var titleBlocks = new FilteredElementCollector(_document, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .FirstOrDefault();

                if (titleBlocks != null)
                {
                    var familySymbol = titleBlocks.Symbol;
                    return familySymbol?.Family?.Name ?? "Standard";
                }
                return "Standard";
            }
            catch
            {
                return "Unknown";
            }
        }

        private void UpdateSheetCount()
        {
            if (SheetsDataGrid.ItemsSource is List<SheetItem> items)
            {
                var selectedCount = items.Count(i => i.IsSelected);
                // Cập nhật text hiển thị số lượng sheets (nếu có control)
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set default output folder
            var defaultFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ProSheetsExports");
            
            txtOutputFolder.Text = defaultFolder;
            
            // Set default export formats
            chkExportPDF.IsChecked = true;
            chkExportDWG.IsChecked = false;
            chkExportIFC.IsChecked = false;
            chkExportImages.IsChecked = false;
            
            // Load profiles
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            cmbProfiles.Items.Clear();
            cmbProfiles.Items.Add("Default Profile");
            cmbProfiles.Items.Add("Architectural Sheets");
            cmbProfiles.Items.Add("Structural Sheets");  
            cmbProfiles.Items.Add("MEP Sheets");
            cmbProfiles.SelectedIndex = 0;
        }

        private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục xuất file";
                dialog.ShowNewFolderButton = true;
                dialog.SelectedPath = txtOutputFolder.Text;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtOutputFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void SelectAllSheets_Click(object sender, RoutedEventArgs e)
        {
            if (SheetsDataGrid.ItemsSource is List<SheetItem> items)
            {
                foreach (var item in items)
                {
                    item.IsSelected = true;
                }
                SheetsDataGrid.Items.Refresh();
                UpdateSheetCount();
            }
        }

        private void DeselectAllSheets_Click(object sender, RoutedEventArgs e)
        {
            if (SheetsDataGrid.ItemsSource is List<SheetItem> items)
            {
                foreach (var item in items)
                {
                    item.IsSelected = false;
                }
                SheetsDataGrid.Items.Refresh();
                UpdateSheetCount();
            }
        }

        private void PreviewExport_Click(object sender, RoutedEventArgs e)
        {
            if (SheetsDataGrid.ItemsSource is List<SheetItem> items)
            {
                var selectedItems = items.Where(i => i.IsSelected).ToList();
                
                if (!selectedItems.Any())
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một sheet để preview.", 
                        "Chưa chọn sheet", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var previewText = $"Sẽ xuất {selectedItems.Count} sheets:\n\n";
                previewText += string.Join("\n", selectedItems.Take(10).Select(s => $"• {s.SheetNumber} - {s.SheetName}"));
                
                if (selectedItems.Count > 10)
                {
                    previewText += $"\n... và {selectedItems.Count - 10} sheets khác";
                }

                previewText += $"\n\nĐịnh dạng:\n";
                if (chkExportPDF.IsChecked == true) previewText += "• PDF\n";
                if (chkExportDWG.IsChecked == true) previewText += "• DWG\n"; 
                if (chkExportIFC.IsChecked == true) previewText += "• IFC\n";
                if (chkExportImages.IsChecked == true) previewText += "• Images\n";

                previewText += $"\nThư mục xuất: {txtOutputFolder.Text}";

                System.Windows.MessageBox.Show(previewText, "Preview Export", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void StartExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SheetsDataGrid.ItemsSource is List<SheetItem> items)
                {
                    var selectedItems = items.Where(i => i.IsSelected).ToList();
                    
                    if (!selectedItems.Any())
                    {
                        System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một sheet để xuất.", 
                            "Chưa chọn sheet", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
                    {
                        System.Windows.MessageBox.Show("Vui lòng chọn thư mục xuất file.", 
                            "Chưa chọn thư mục", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Tạo thư mục nếu chưa có
                    Directory.CreateDirectory(txtOutputFolder.Text);

                    bool hasExportFormat = chkExportPDF.IsChecked == true || 
                                          chkExportDWG.IsChecked == true || 
                                          chkExportIFC.IsChecked == true || 
                                          chkExportImages.IsChecked == true;

                    if (!hasExportFormat)
                    {
                        System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một định dạng xuất.", 
                            "Chưa chọn định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var selectedSheets = selectedItems.Select(i => i.Sheet).ToList();
                    int exportedCount = 0;

                    // Export PDF
                    if (chkExportPDF.IsChecked == true)
                    {
                        if (ExportToPDF(selectedSheets))
                            exportedCount++;
                    }

                    // Các format khác có thể thêm sau
                    if (chkExportDWG.IsChecked == true)
                    {
                        System.Windows.MessageBox.Show("DWG export sẽ được thêm vào phiên bản sau.", 
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    if (exportedCount > 0)
                    {
                        System.Windows.MessageBox.Show($"Xuất thành công {selectedItems.Count} sheets!\n\nThư mục: {txtOutputFolder.Text}", 
                            "Hoàn thành", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi trong quá trình xuất: {ex.Message}", 
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ExportToPDF(List<ViewSheet> sheets)
        {
            try
            {
                var sheetIds = sheets.Select(s => s.Id).ToList();

                var pdfOptions = new PDFExportOptions();
                pdfOptions.Combine = false; // Tạo file PDF riêng cho mỗi sheet
                pdfOptions.ColorDepth = ColorDepthType.Color;
                pdfOptions.RasterQuality = RasterQualityType.High;
                pdfOptions.PaperOrientation = PageOrientationType.Auto;
                pdfOptions.HideCropBoundaries = true;
                pdfOptions.HideScopeBoxes = true;

                using (var trans = new Transaction(_document, "Export PDF"))
                {
                    trans.Start();
                    _document.Export(txtOutputFolder.Text, sheetIds, pdfOptions);
                    trans.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", 
                    "Lỗi PDF", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UpdateFileNamePreview()
        {
            // Cập nhật preview tên file dựa trên template
            if (txtFileNameTemplate != null)
            {
                var template = txtFileNameTemplate.Text;
                if (!string.IsNullOrEmpty(template))
                {
                    var preview = template
                        .Replace("{ProjectNumber}", "P001")
                        .Replace("{SheetNumber}", "A-101")
                        .Replace("{SheetName}", "Floor Plan")
                        .Replace("{Revision}", "Rev01")
                        .Replace("{Date}", DateTime.Now.ToString("yyyyMMdd"));
                    
                    // Hiển thị preview (nếu có control tương ứng)
                }
            }
        }
    }

    // Class để bind với DataGrid
    public class SheetItem
    {
        public ViewSheet Sheet { get; set; }
        public bool IsSelected { get; set; }
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string Revision { get; set; }
        public string PaperSize { get; set; }
    }
}