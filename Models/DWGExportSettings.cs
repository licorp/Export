namespace ProSheetsAddin.Models
{
    public class DWGExportSettings
    {
        public string SetupName { get; set; } = "Default";
        public string OutputFolder { get; set; }
        public bool BindImagesAsOLE { get; set; } = false;
        public bool ExportViewsAsXrefs { get; set; } = false;
        public bool MergeViews { get; set; } = false;
        public string LayerSettings { get; set; } = "AIA";
        public string LineWeights { get; set; } = "ByLayer";
        public string Colors { get; set; } = "ByLayer";
        public string Units { get; set; } = "Millimeter";
        public bool ExportRoomBoundaries { get; set; } = true;
        public bool ExportAreaBoundaries { get; set; } = true;
        public string FileVersion { get; set; } = "2018"; // AutoCAD version
        
        public DWGExportSettings()
        {
        }
        
        public DWGExportSettings Clone()
        {
            return new DWGExportSettings
            {
                SetupName = this.SetupName,
                OutputFolder = this.OutputFolder,
                BindImagesAsOLE = this.BindImagesAsOLE,
                ExportViewsAsXrefs = this.ExportViewsAsXrefs,
                MergeViews = this.MergeViews,
                LayerSettings = this.LayerSettings,
                LineWeights = this.LineWeights,
                Colors = this.Colors,
                Units = this.Units,
                ExportRoomBoundaries = this.ExportRoomBoundaries,
                ExportAreaBoundaries = this.ExportAreaBoundaries,
                FileVersion = this.FileVersion
            };
        }
    }
}