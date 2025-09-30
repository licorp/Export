using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProSheetsAddin.Models;
using ProSheetsAddin.Utils;
using RevitDB = Autodesk.Revit.DB;

namespace ProSheetsAddin.Managers
{
    public class BatchExportManager
    {
        private readonly RevitDB.Document _document;
        
        public BatchExportManager(RevitDB.Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public async Task<bool> ExportToPDF(List<RevitDB.ViewSheet> sheets, PSPDFExportSettings settings, IProgress<int> progress = null)
        {
            try
            {
                // Use the new PDFExportManager
                var pdfManager = new PDFExportManager(_document);
                
                // Convert settings to ExportSettings
                var exportSettings = new ExportSettings
                {
                    OutputFolder = settings.OutputFolder,
                    CreateSeparateFolders = settings.CreateSubfolders
                };
                
                // Export using native PDF manager
                bool result = pdfManager.ExportSheetsToPDF(sheets, settings.OutputFolder, exportSettings);
                
                // Report 100% completion
                progress?.Report(100);
                await Task.Yield();
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF Export Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExportToDWG(List<RevitDB.ViewSheet> sheets, PSDWGExportSettings settings, IProgress<int> progress = null)
        {
            try
            {
                var dwgOptions = new RevitDB.DWGExportOptions();
                
                for (int i = 0; i < sheets.Count; i++)
                {
                    var sheet = sheets[i];
                    var fileName = FileNameGenerator.GenerateFileName(sheet, _document, settings.FileNamingPattern, "dwg");
                    var outputPath = settings.CreateSubfolders 
                        ? FileNameGenerator.GenerateSubfolderPath(sheet, _document, settings)
                        : settings.OutputFolder;
                    
                    try
                    {
                        var singleViewIds = new List<RevitDB.ElementId> { sheet.Id };
                        _document.Export(outputPath, fileName.Replace(".dwg", ""), singleViewIds, dwgOptions);
                        
                        progress?.Report(((i + 1) * 100) / sheets.Count);
                        await Task.Yield();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DWG Export Error for sheet {sheet.SheetNumber}: {ex.Message}");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DWG Export Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExportToIFC(List<RevitDB.ViewSheet> sheets, PSIFCExportSettings settings, IProgress<int> progress = null)
        {
            try
            {
                var ifcOptions = new RevitDB.IFCExportOptions();
                ifcOptions.FileVersion = GetIFCVersion(settings.IFCVersion);
                ifcOptions.ExportBaseQuantities = settings.ExportBaseQuantities;

                var fileName = "ExportedModel.ifc";
                var filePath = Path.Combine(settings.OutputFolder, fileName);
                
                _document.Export(settings.OutputFolder, fileName, ifcOptions);
                
                progress?.Report(100);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IFC Export Error: {ex.Message}");
                return false;
            }
        }

        private RevitDB.ColorDepthType ConvertColorDepth(PSColorDepth colorDepth)
        {
            switch (colorDepth)
            {
                case PSColorDepth.BlackLine:
                    return RevitDB.ColorDepthType.BlackLine;
                case PSColorDepth.GrayScale:
                    return RevitDB.ColorDepthType.GrayScale;
                case PSColorDepth.Color:
                    return RevitDB.ColorDepthType.Color;
                default:
                    return RevitDB.ColorDepthType.Color;
            }
        }



        private RevitDB.ACADVersion GetDWGVersion(string version)
        {
            switch (version)
            {
                case "2018": return RevitDB.ACADVersion.R2018;
                case "2013": return RevitDB.ACADVersion.R2013;
                default: return RevitDB.ACADVersion.R2018;
            }
        }

        private RevitDB.IFCVersion GetIFCVersion(PSIFCVersion version)
        {
            switch (version)
            {
                case PSIFCVersion.IFC2x3: return RevitDB.IFCVersion.IFC2x3CV2;
                case PSIFCVersion.IFC4: return RevitDB.IFCVersion.IFC4RV;
                default: return RevitDB.IFCVersion.IFC2x3CV2;
            }
        }
    }
}
