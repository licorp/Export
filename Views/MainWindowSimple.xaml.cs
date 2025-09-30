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

        private void LoadSheets()
        {
            _allSheets = new FilteredElementCollector(_document)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsTemplate)
                .OrderBy(s => s.SheetNumber)
                .ToList();

            SheetsListBox.ItemsSource = _allSheets.Select(s => new { 
                Sheet = s, 
                DisplayName = $"{s.SheetNumber} - {s.Name}" 
            }).ToList();
            SheetsListBox.DisplayMemberPath = "DisplayName";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OutputFolderBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ProSheetsExports");
        }

        private void BrowseFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select output folder";
                dlg.SelectedPath = OutputFolderBox.Text;
                dlg.ShowNewFolderButton = true;
                
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputFolderBox.Text = dlg.SelectedPath;
                }
            }
        }

        private void SelectAllSheets_Click(object sender, RoutedEventArgs e)
        {
            SheetsListBox.SelectAll();
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedCount = SheetsListBox.SelectedItems.Count;
            System.Windows.MessageBox.Show($"Selected {selectedCount} sheets for export", "Preview");
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SheetsListBox.SelectedItems.Count == 0)
                {
                    System.Windows.MessageBox.Show("Please select at least one sheet to export.", "No Selection");
                    return;
                }

                if (string.IsNullOrEmpty(OutputFolderBox.Text))
                {
                    System.Windows.MessageBox.Show("Please select an output folder.", "No Output Folder");
                    return;
                }

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(OutputFolderBox.Text);

                var selectedSheets = SheetsListBox.SelectedItems
                    .Cast<dynamic>()
                    .Select(item => (ViewSheet)item.Sheet)
                    .ToList();

                bool exportSuccess = false;

                if (ExportPDF.IsChecked == true)
                {
                    exportSuccess = ExportToPDF(selectedSheets);
                }

                if (exportSuccess)
                {
                    System.Windows.MessageBox.Show($"Successfully exported {selectedSheets.Count} sheets to:\n{OutputFolderBox.Text}", "Export Complete");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error");
            }
        }

        private bool ExportToPDF(List<ViewSheet> sheets)
        {
            try
            {
                var sheetIds = sheets.Select(s => s.Id).ToList();

                var pdfOptions = new PDFExportOptions();
                pdfOptions.Combine = false;
                pdfOptions.ColorDepth = ColorDepthType.Color;
                pdfOptions.RasterQuality = RasterQualityType.High;
                pdfOptions.PaperOrientation = PageOrientationType.Auto;
                pdfOptions.HideCropBoundaries = true;

                using (var trans = new Transaction(_document, "Export PDF"))
                {
                    trans.Start();
                    _document.Export(OutputFolderBox.Text, sheetIds, pdfOptions);
                    trans.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"PDF Export failed: {ex.Message}", "PDF Export Error");
                return false;
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}