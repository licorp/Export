namespace ProSheetsAddin.Models
{
    public class IFCExportSettings
    {
        public string OutputFolder { get; set; }
        public IFCVersion IFCVersion { get; set; } = IFCVersion.IFC2x3;
        public int SpaceBoundaryLevel { get; set; } = 1;
        public bool ExportQuantities { get; set; } = false;
        public bool SplitWallsColumns { get; set; } = false;
        public bool Export2DElements { get; set; } = true;
        public bool ExportRevitProperties { get; set; } = true;
        public bool ExportIFCProperties { get; set; } = true;
        public bool ExportByView { get; set; } = false;
        public bool ExportLinkedFiles { get; set; } = false;
        public string FilePrefix { get; set; } = "";
        
        public IFCExportSettings()
        {
        }
        
        public IFCExportSettings Clone()
        {
            return new IFCExportSettings
            {
                OutputFolder = this.OutputFolder,
                IFCVersion = this.IFCVersion,
                SpaceBoundaryLevel = this.SpaceBoundaryLevel,
                ExportQuantities = this.ExportQuantities,
                SplitWallsColumns = this.SplitWallsColumns,
                Export2DElements = this.Export2DElements,
                ExportRevitProperties = this.ExportRevitProperties,
                ExportIFCProperties = this.ExportIFCProperties,
                ExportByView = this.ExportByView,
                ExportLinkedFiles = this.ExportLinkedFiles,
                FilePrefix = this.FilePrefix
            };
        }
    }
    
    public enum IFCVersion
    {
        IFC2x2,
        IFC2x3,
        IFC4,
        IFC4RV
    }
}