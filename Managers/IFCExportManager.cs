using System;
using System.Collections.Generic;
using System.Linq;
using ProSheetsAddin.Models;
using RevitDB = Autodesk.Revit.DB;

namespace ProSheetsAddin.Managers
{
    public class IFCExportManager
    {
        public void ExportToIFC(List<RevitDB.ViewSheet> sheets, PSIFCExportSettings settings)
        {
            var ifcOptions = new RevitDB.IFCExportOptions();
            
            try
            {
                // Configure basic IFC export options
                ifcOptions.FileVersion = GetIFCVersion(settings.IFCVersion);
                ifcOptions.ExportBaseQuantities = settings.ExportBaseQuantities;
                
                // Basic export
                var fileName = "ExportedModel";
                if (sheets.Any())
                {
                    var document = sheets.First().Document;
                    document.Export(settings.OutputFolder, fileName, ifcOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IFC Export Error: {ex.Message}");
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