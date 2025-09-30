using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSheetsAddin.Models;
using ProSheetsAddin.Utils;
using RevitDB = Autodesk.Revit.DB;

namespace ProSheetsAddin.Managers
{
    public class DWGExportManager
    {
        private readonly RevitDB.Document _document;
        
        public DWGExportManager(RevitDB.Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public bool ExportToDWG(List<RevitDB.ViewSheet> sheets, PSDWGExportSettings settings)
        {
            try
            {
                var dwgOptions = new RevitDB.DWGExportOptions();
                dwgOptions.FileVersion = GetDWGVersion(settings.DWGVersion);
                dwgOptions.SharedCoords = settings.UseSharedCoordinates;
                
                foreach (var sheet in sheets)
                {
                    try
                    {
                        var fileName = FileNameGenerator.GenerateFileName(sheet, _document, settings.FileNamingPattern, "dwg");
                        var outputPath = settings.CreateSubfolders 
                            ? FileNameGenerator.GenerateSubfolderPath(sheet, _document, settings)
                            : settings.OutputFolder;
                        
                        if (!Directory.Exists(outputPath))
                        {
                            Directory.CreateDirectory(outputPath);
                        }
                        
                        var viewIds = new List<RevitDB.ElementId> { sheet.Id };
                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        
                        _document.Export(outputPath, fileNameWithoutExtension, viewIds, dwgOptions);
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
                System.Diagnostics.Debug.WriteLine($"DWG Export Manager Error: {ex.Message}");
                return false;
            }
        }

        private RevitDB.ACADVersion GetDWGVersion(string version)
        {
            switch (version?.ToLower())
            {
                case "2018":
                    return RevitDB.ACADVersion.R2018;
                case "2013":
                    return RevitDB.ACADVersion.R2013;
                default:
                    return RevitDB.ACADVersion.R2018;
            }
        }
    }
}
