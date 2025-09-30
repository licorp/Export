using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using ProSheetsAddin.Views;
using ProSheetsAddin.Models;
using ProSheetsAddin.Managers;
using ProSheetsAddin.Utils;

namespace ProSheetsAddin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, 
                             ref string message, 
                             ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            // Kiểm tra có sheets trong project không
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> sheets = collector.OfClass(typeof(ViewSheet)).ToElements();
            
            if (sheets.Count == 0)
            {
                TaskDialog.Show("Lỗi", "Không tìm thấy sheet nào trong project.");
                return Result.Failed;
            }
            
            try
            {
                // Hiển thị Main Export Dialog
                MainWindow exportDialog = new MainWindow(doc, uidoc);
                
                if (exportDialog.ShowDialog() == true)
                {
                    // Lấy settings từ dialog
                    ExportSettings settings = exportDialog.GetExportSettings();
                    List<ViewSheet> selectedSheets = exportDialog.GetSelectedSheets();
                    
                    if (selectedSheets == null || selectedSheets.Count == 0)
                    {
                        TaskDialog.Show("Thông báo", "Không có sheet nào được chọn để xuất.");
                        return Result.Cancelled;
                    }
                    
                    // Thực hiện export
                    var batchExporter = new BatchExportManager();
                    bool success = batchExporter.ExecuteBatchExport(doc, selectedSheets, settings);
                    
                    if (success)
                    {
                        TaskDialog.Show("Thành công", 
                            $"Đã xuất thành công {selectedSheets.Count} sheets.\n" +
                            $"Vị trí lưu: {settings.OutputFolder}");
                    }
                    else
                    {
                        TaskDialog.Show("Lỗi", "Có lỗi xảy ra trong quá trình xuất file. Kiểm tra log để biết chi tiết.");
                    }
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Lỗi: {ex.Message}";
                TaskDialog.Show("Lỗi Export", message);
                return Result.Failed;
            }
        }
        
        private PDFExportOptions CreatePDFExportOptions(ExportSettings settings)
        {
            PDFExportOptions options = new PDFExportOptions();
            
            // Cấu hình cơ bản
            options.PaperFormat = ExportPaperFormat.Default; // Tự động phát hiện
            options.PaperOrientation = PageOrientationType.Auto;
            options.Combine = settings.CombinePDFs; 
            options.HideCropBoundaries = settings.HideCropBoundaries;
            options.HideScopeBoxes = settings.HideScopeBoxes;
            options.ColorDepth = settings.PDFColorDepth;
            options.ExportQuality = settings.PDFQuality;
            options.MaskedAreaFill = MaskedAreaFill.WhiteFill;
            
            // Cấu hình nâng cao
            if (settings.RasterQuality != ImageResolution.DPI_600)
            {
                options.RasterQuality = settings.RasterQuality;
            }
            
            return options;
        }
        
        private void ExportSheetToPDF(Document doc, ViewSheet sheet, ExportSettings settings)
        {
            // Tạo PDF export options
            PDFExportOptions options = CreatePDFExportOptions(settings);
            
            // Tạo tên file từ sheet parameters
            string fileName = FileNameGenerator.GenerateFileName(sheet, doc, settings.FileNameTemplate, "pdf");
            
            // Tạo list ElementId cho sheet
            List<ElementId> sheetIds = new List<ElementId> { sheet.Id };
            
            // Tạo thư mục nếu chưa có
            Directory.CreateDirectory(settings.OutputFolder);
            
            // Xuất PDF
            try
            {
                doc.Export(settings.OutputFolder, fileName, sheetIds, options);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi Export PDF", $"Không thể xuất {sheet.Name}: {ex.Message}");
            }
        }
    }
}