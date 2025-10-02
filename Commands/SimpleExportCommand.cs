using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ProSheetsAddin.Models;
using ProSheetsAddin.Managers;
using ProSheetsAddin.Views;
using System.Diagnostics;

namespace ProSheetsAddin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class SimpleExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Debug.WriteLine("[Export +] SimpleExportCommand started");
                
                // Tạo một window đơn giản với giao diện đẹp
                var window = CreateBeautifulWindow(commandData);
                Debug.WriteLine("[Export +] Main export window created");
                
                // Show as non-modal window so user can interact with both Revit and ProSheets
                window.Show();
                Debug.WriteLine("[Export +] Export window shown (non-modal)");
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Export +] SimpleExportCommand error: {ex.Message}");
                Debug.WriteLine($"[ProSheets] Stack trace: {ex.StackTrace}");
                
                TaskDialog.Show("Error", $"Lỗi: {ex.Message}");
                return Result.Failed;
            }
        }

        private System.Windows.Window CreateBeautifulWindow(ExternalCommandData commandData)
        {
            try
            {
                // Tạo ProSheets Main Window với đầy đủ tính năng
                var mainWindow = new ProSheetsMainWindow(
                    commandData.Application.ActiveUIDocument.Document,
                    commandData.Application);
                return mainWindow;
            }
            catch (Exception)
            {
                // Fallback to simple window if ProSheetsMainWindow fails
                return CreateSimpleWindow(commandData);
            }
        }

        private System.Windows.Window CreateSimpleWindow(ExternalCommandData commandData)
        {
            var window = new System.Windows.Window()
            {
                Title = "ProSheets Export Manager",
                Width = 1000,
                Height = 700,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.CanResize
            };

            // Tạo giao diện đẹp bằng code
            var mainGrid = new System.Windows.Controls.Grid();
            
            // Add header
            var header = new System.Windows.Controls.TextBlock()
            {
                Text = "ProSheets Export Manager",
                FontSize = 24,
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(20),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            
            var exportButton = new System.Windows.Controls.Button()
            {
                Content = "Launch ProSheets Manager",
                Width = 250,
                Height = 60,
                FontSize = 16,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)),
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new System.Windows.Thickness(20),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            exportButton.Click += (s, e) => LaunchProSheetsManager(commandData, window);

            var stackPanel = new System.Windows.Controls.StackPanel()
            {
                Orientation = System.Windows.Controls.Orientation.Vertical
            };
            
            stackPanel.Children.Add(header);
            stackPanel.Children.Add(exportButton);
            
            mainGrid.Children.Add(stackPanel);
            window.Content = mainGrid;

            return window;
        }

        private void LaunchProSheetsManager(ExternalCommandData commandData, System.Windows.Window parentWindow)
        {
            try
            {
                var doc = commandData.Application.ActiveUIDocument.Document;
                
                // Launch the full ProSheets Manager
                var mainWindow = new ProSheetsMainWindow(doc);
                parentWindow.Close();
                mainWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Không thể khởi động ProSheets Manager:\n{ex.Message}\n\nSẽ sử dụng chế độ đơn giản.",
                    "Lỗi",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                
                // Fallback to simple export
                ExportAllSheetsSimple(commandData, parentWindow);
            }
        }

        private void ExportAllSheetsSimple(ExternalCommandData commandData, System.Windows.Window parentWindow)
        {
            try
            {
                var doc = commandData.Application.ActiveUIDocument.Document;
                
                // Lấy tất cả sheets
                var sheets = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(s => !s.IsTemplate)
                    .ToList();
                
                if (!sheets.Any())
                {
                    System.Windows.MessageBox.Show("Không có sheet nào trong project.", "Thông báo");
                    return;
                }

                // Chọn folder xuất
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Chọn thư mục để xuất PDF";
                    dialog.ShowNewFolderButton = true;
                    
                    if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        return;
                        
                    var outputFolder = dialog.SelectedPath;
                    
                    // Export PDF theo đúng API
                    var pdfOptions = new PDFExportOptions();
                    pdfOptions.Combine = false;
                    pdfOptions.ColorDepth = ColorDepthType.Color;
                    pdfOptions.RasterQuality = RasterQualityType.High;
                    pdfOptions.PaperOrientation = PageOrientationType.Auto;
                    pdfOptions.HideCropBoundaries = true;
                    
                    var sheetIds = sheets.Select(s => s.Id).ToList();
                    
                    using (var trans = new Transaction(doc, "Export PDF"))
                    {
                        trans.Start();
                        doc.Export(outputFolder, sheetIds, pdfOptions);
                        trans.Commit();
                        
                        System.Windows.MessageBox.Show(
                            $"Đã xuất thành công {sheets.Count} sheets!\n\nThư mục: {outputFolder}", 
                            "Hoàn thành",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                }
                
                parentWindow.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }
    }
}