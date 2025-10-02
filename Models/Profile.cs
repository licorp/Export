using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProSheetsAddin.Models
{
    /// <summary>
    /// Profile model for saving and loading ProSheets configurations
    /// </summary>
    public class Profile : INotifyPropertyChanged
    {
        private string _name;
        private DateTime _lastModified;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set { _lastModified = value; OnPropertyChanged(); }
        }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string Description { get; set; }

        // Profile Settings
        public ProfileSettings Settings { get; set; } = new ProfileSettings();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// All settings stored in a profile
    /// </summary>
    public class ProfileSettings
    {
        // Selection Settings
        public List<string> SelectedSheetNumbers { get; set; } = new List<string>();
        public string FilterBy { get; set; } = "All Sheets";
        public bool FilterByVSEnabled { get; set; } = false;

        // Format Settings - PDF
        public bool PDFEnabled { get; set; } = true;
        public string PDFPrinterName { get; set; } = "PDF24";
        public bool PaperPlacementCenter { get; set; } = true;
        public string MarginType { get; set; } = "No Margin";
        public double OffsetX { get; set; } = 0;
        public double OffsetY { get; set; } = 0;
        public bool FitToPage { get; set; } = false;
        public int ZoomPercent { get; set; } = 100;
        public bool VectorProcessing { get; set; } = true;
        public string RasterQuality { get; set; } = "High";
        public string ColorMode { get; set; } = "Color";
        
        // PDF Options
        public bool ViewLinksInBlue { get; set; } = false;
        public bool HideRefWorkPlanes { get; set; } = true;
        public bool HideUnreferencedViewTags { get; set; } = true;
        public bool HideScopeBoxes { get; set; } = true;
        public bool HideCropBoundaries { get; set; } = true;
        public bool ReplaceHalftone { get; set; } = false;
        public bool RegionEdgesMask { get; set; } = true;
        public bool CreateSeparateFiles { get; set; } = true;
        public bool KeepPaperSizeOrientation { get; set; } = false;

        // Format Settings - Other formats
        public bool DWGEnabled { get; set; } = false;
        public bool DGNEnabled { get; set; } = false;
        public bool DWFEnabled { get; set; } = false;
        public bool NWCEnabled { get; set; } = false;
        public bool IFCEnabled { get; set; } = false;
        public bool IMGEnabled { get; set; } = false;

        // Custom File Name Settings
        public List<string> CustomFileNameParameters { get; set; } = new List<string>();
        public string CustomFileNamePreview { get; set; } = "";

        // Create Settings
        public string OutputFolder { get; set; } = @"D:\OneDrive\Desktop\";
        public bool SaveAllInSameFolder { get; set; } = true;
        public string ReportType { get; set; } = "Don't Save Report";
        public bool SchedulingEnabled { get; set; } = false;
        public DateTime ScheduleStartDate { get; set; } = DateTime.Now;
        public string ScheduleTime { get; set; } = "10:21 AM";
        public string RepeatType { get; set; } = "Does not repeat";
    }
}
