using System;
using System.Collections.Generic;
using System.ComponentModel;
using RevitDB = Autodesk.Revit.DB;

namespace ProSheetsAddin.Models
{
    // Custom enums with PS prefix to avoid conflicts with Revit API
    public enum PSExportFormat
    {
        PDF,
        DWG,
        DWF,
        IFC,
        Images,
        XML
    }

    public enum PSExportFrequency
    {
        Once,
        Daily,
        Weekly,
        Monthly
    }

    public enum PSPaperSize
    {
        ISO_A4,
        ISO_A3,
        ISO_A2,
        ISO_A1,
        ISO_A0,
        ANSI_A,
        ANSI_B,
        ANSI_C,
        ANSI_D,
        ANSI_E,
        Custom
    }

    public enum PSZoomType
    {
        FitToPage,
        Zoom
    }

    public enum PSRasterQualityType
    {
        Presentation,
        High,
        Medium,
        Low
    }

    public enum PSColorDepth
    {
        BlackLine,
        GrayScale,
        Color
    }

    public enum PSIFCVersion
    {
        IFC2x3,
        IFC4,
        IFC4x1
    }

    public enum ScheduleRepeatType
    {
        Once,
        Daily,
        Weekly,
        Monthly
    }

    // Base Export Settings
    public class PSExportSettings
    {
        public string OutputFolder { get; set; }
        public bool CreateSubfolders { get; set; }
        public string SubfolderTemplate { get; set; } = "{Discipline}\\{DrawingType}";
        public string FileNamingPattern { get; set; } = "{SheetNumber}_{SheetName}";
    }

    // Main Models
    public class ExportProfile : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public PSExportFormat ExportFormat { get; set; }
        public PSPDFExportSettings PdfSettings { get; set; }
        public PSDWGExportSettings DwgSettings { get; set; }
        public PSIFCExportSettings IfcSettings { get; set; }
        public PSImageExportSettings ImageSettings { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PSPDFExportSettings : PSExportSettings
    {
        public int Resolution { get; set; } = 300;
        public PSColorDepth ColorDepth { get; set; } = PSColorDepth.Color;
        public PSZoomType ZoomType { get; set; } = PSZoomType.FitToPage;
        public int ZoomPercentage { get; set; } = 100;
        public PSPaperSize PaperSize { get; set; } = PSPaperSize.ISO_A3;
        public bool HideCropBoundaries { get; set; } = true;
        public bool HideReferencePlane { get; set; } = true;
        public bool HideScopeBoxes { get; set; } = true;
        public bool HideUnreferencedViewTags { get; set; } = true;
        public bool MaskCoincidentLines { get; set; } = true;
        public bool ExportLinks { get; set; } = true;
        public PSRasterQualityType RasterQuality { get; set; } = PSRasterQualityType.High;
        public bool CombineMultipleSheets { get; set; }
    }

    public class PSDWGExportSettings : PSExportSettings
    {
        public string DWGVersion { get; set; } = "2018";
        public bool UseSharedCoordinates { get; set; }
        public bool ExportObjectData { get; set; }
        public bool ExportMaterials { get; set; }
        public string LayerMapping { get; set; }
        public bool ShowLayerMapping { get; set; }
        public bool LinetypeScaling { get; set; }
    }

    public class PSIFCExportSettings : PSExportSettings
    {
        public PSIFCVersion IFCVersion { get; set; } = PSIFCVersion.IFC2x3;
        public bool ExportBaseQuantities { get; set; } = true;
        public bool Export2DElements { get; set; } = false;
        public bool ExportInternalRevitPropertySets { get; set; } = false;
        public bool ExportIFCCommonPropertySets { get; set; } = true;
        public bool ExportSchedulesAsPsets { get; set; } = false;
        public bool ExportUserDefinedPsets { get; set; } = false;
        public bool ExportLinkedFiles { get; set; } = false;
        public List<RevitDB.Element> Sheets { get; set; }
    }

    public class PSImageExportSettings : PSExportSettings
    {
        public int Resolution { get; set; } = 300;
        public RevitDB.ImageFileType ImageFormat { get; set; } = RevitDB.ImageFileType.PNG;
        public bool UseCustomPixelSize { get; set; }
        public int PixelSize { get; set; }
        public int ZoomPercentage { get; set; }
    }



    public class ScheduledExport : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string ProfileName { get; set; }
        public ScheduleRepeatType Frequency { get; set; }
        public DateTime NextRunTime { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<string> SelectedSheetIds { get; set; } = new List<string>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastRunTime { get; set; }
        public string LastRunStatus { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DrawingTransmittal
    {
        public string Id { get; set; }
        public string ProjectName { get; set; }
        public string ProjectNumber { get; set; }
        public string TransmittalNumber { get; set; }
        public DateTime DateIssued { get; set; }
        public string IssuedBy { get; set; }
        public string IssuedTo { get; set; }
        public string Purpose { get; set; }
        public List<TransmittalSheet> Sheets { get; set; } = new List<TransmittalSheet>();
    }

    public class TransmittalSheet
    {
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string Revision { get; set; }
        public DateTime DateIssued { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class ExportProgress
    {
        public int CurrentSheet { get; set; }
        public int TotalSheets { get; set; }
        public string CurrentSheetName { get; set; }
        public string Status { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasErrors { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        
        public double ProgressPercentage => TotalSheets > 0 ? (double)CurrentSheet / TotalSheets * 100 : 0;
    }

    // SheetItem class moved to separate Models/SheetItem.cs file

    public class ExportBatch
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public List<string> SelectedSheetIds { get; set; } = new List<string>();
        public string ProfileName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public PSExportFormat Format { get; set; }
        public string OutputPath { get; set; }
    }

    public class ExportHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime ExportDate { get; set; }
        public string ProfileName { get; set; }
        public PSExportFormat Format { get; set; }
        public int SheetCount { get; set; }
        public string OutputPath { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class CustomPaperSize
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        
        public static CustomPaperSize GetDefaultPaperSize()
        {
            return new CustomPaperSize { Name = "A3", Width = 297, Height = 420 };
        }
    }

    /// <summary>
    /// Export settings model with INotifyPropertyChanged support for data binding
    /// </summary>
    public class ExportSettings : INotifyPropertyChanged
    {
        private string _outputFolder = @"C:\Export_Plus_Output\";
        private bool _combineFiles = false;
        private string _fileNameTemplate = "{SheetNumber}_{SheetName}";
        private PSQualityType _quality = PSQualityType.High;
        private bool _includeRevision = true;
        private bool _createSeparateFolders = false;
        private PSPaperPlacement _paperPlacement = PSPaperPlacement.Center;
        private PSPaperMargin _paperMargin = PSPaperMargin.NoMargin;
        private double _offsetX = 0.0;
        private double _offsetY = 0.0;
        private PSHiddenLineViews _hiddenLineViews = PSHiddenLineViews.VectorProcessing;
        private bool _viewLinks = false;
        private bool _viewLinksInBlue = false;
        private bool _hideRefWorkPlanes = true;
        private bool _hideUnreferencedViewTags = true;
        private bool _hideScopeBoxes = true;
        private bool _hideCropBoundaries = true;
        private bool _replaceHalftone = false;
        private bool _regionEdgesMask = true;
        private PSZoomType _zoom = PSZoomType.Zoom;
        private PSRasterQuality _rasterQuality = PSRasterQuality.High;
        private PSColors _colors = PSColors.Color;
        private bool _keepPaperSize = false;

        // Output Settings
        public string OutputFolder
        {
            get => _outputFolder;
            set { _outputFolder = value; OnPropertyChanged(); }
        }

        public bool CombineFiles
        {
            get => _combineFiles;
            set { _combineFiles = value; OnPropertyChanged(); }
        }

        public string FileNameTemplate
        {
            get => _fileNameTemplate;
            set { _fileNameTemplate = value; OnPropertyChanged(); }
        }

        public PSQualityType Quality
        {
            get => _quality;
            set { _quality = value; OnPropertyChanged(); }
        }

        public bool IncludeRevision
        {
            get => _includeRevision;
            set { _includeRevision = value; OnPropertyChanged(); }
        }

        public bool CreateSeparateFolders
        {
            get => _createSeparateFolders;
            set { _createSeparateFolders = value; OnPropertyChanged(); }
        }

        // Hidden Line Views Settings  
        public PSHiddenLineViews HiddenLineViews
        {
            get => _hiddenLineViews;
            set { _hiddenLineViews = value; OnPropertyChanged(); }
        }

        public bool ViewLinks
        {
            get => _viewLinks;
            set { _viewLinks = value; OnPropertyChanged(); }
        }

        public bool ViewLinksInBlue
        {
            get => _viewLinksInBlue;
            set { _viewLinksInBlue = value; OnPropertyChanged(); }
        }

        public bool HideRefWorkPlanes
        {
            get => _hideRefWorkPlanes;
            set { _hideRefWorkPlanes = value; OnPropertyChanged(); }
        }

        public bool HideUnreferencedViewTags
        {
            get => _hideUnreferencedViewTags;
            set { _hideUnreferencedViewTags = value; OnPropertyChanged(); }
        }

        public bool HideScopeBoxes
        {
            get => _hideScopeBoxes;
            set { _hideScopeBoxes = value; OnPropertyChanged(); }
        }

        public bool HideCropBoundaries
        {
            get => _hideCropBoundaries;
            set { _hideCropBoundaries = value; OnPropertyChanged(); }
        }

        public bool ReplaceHalftone
        {
            get => _replaceHalftone;
            set { _replaceHalftone = value; OnPropertyChanged(); }
        }

        public bool RegionEdgesMask
        {
            get => _regionEdgesMask;
            set { _regionEdgesMask = value; OnPropertyChanged(); }
        }

        // Additional Settings
        public PSZoomType Zoom
        {
            get => _zoom;
            set { _zoom = value; OnPropertyChanged(); }
        }

        public PSRasterQuality RasterQuality
        {
            get => _rasterQuality;
            set { _rasterQuality = value; OnPropertyChanged(); }
        }

        public PSColors Colors
        {
            get => _colors;
            set { _colors = value; OnPropertyChanged(); }
        }

        public bool KeepPaperSize
        {
            get => _keepPaperSize;
            set { _keepPaperSize = value; OnPropertyChanged(); }
        }

        // Paper Placement Settings
        public PSPaperPlacement PaperPlacement
        {
            get => _paperPlacement;
            set { _paperPlacement = value; OnPropertyChanged(); }
        }

        public PSPaperMargin PaperMargin
        {
            get => _paperMargin;
            set { _paperMargin = value; OnPropertyChanged(); }
        }

        public double OffsetX
        {
            get => _offsetX;
            set { _offsetX = value; OnPropertyChanged(); }
        }

        public double OffsetY
        {
            get => _offsetY;
            set { _offsetY = value; OnPropertyChanged(); }
        }

        // Selected formats tracking with binding support
        private Dictionary<string, bool> _selectedFormats = new Dictionary<string, bool>
        {
            {"PDF", true}, {"DWG", false}, {"DGN", true}, {"DWF", false}, 
            {"NWC", false}, {"IFC", true}, {"IMG", false}
        };

        public Dictionary<string, bool> SelectedFormats
        {
            get => _selectedFormats;
            set { _selectedFormats = value; OnPropertyChanged(); }
        }

        public bool IsPdfSelected
        {
            get => _selectedFormats.ContainsKey("PDF") && _selectedFormats["PDF"];
            set { _selectedFormats["PDF"] = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFormats)); }
        }

        public bool IsDwgSelected
        {
            get => _selectedFormats.ContainsKey("DWG") && _selectedFormats["DWG"];
            set { _selectedFormats["DWG"] = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFormats)); }
        }

        public bool IsDgnSelected
        {
            get => _selectedFormats.ContainsKey("DGN") && _selectedFormats["DGN"];
            set { _selectedFormats["DGN"] = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFormats)); }
        }

        public bool IsDwfSelected
        {
            get => _selectedFormats.ContainsKey("DWF") && _selectedFormats["DWF"];
            set { _selectedFormats["DWF"] = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFormats)); }
        }

        public bool IsNwcSelected
        {
            get => _selectedFormats.ContainsKey("NWC") && _selectedFormats["NWC"];
            set { _selectedFormats["NWC"] = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFormats)); }
        }

        public bool IsIfcSelected
        {
            get => _selectedFormats.ContainsKey("IFC") && _selectedFormats["IFC"];
            set { _selectedFormats["IFC"] = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFormats)); }
        }

        public bool IsImgSelected
        {
            get => _selectedFormats.ContainsKey("IMG") && _selectedFormats["IMG"];
            set { _selectedFormats["IMG"] = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFormats)); OnPropertyChanged(nameof(IsImageSelected)); }
        }

        // Alias for XAML binding compatibility
        public bool IsImageSelected
        {
            get => IsImgSelected;
            set => IsImgSelected = value;
        }

        // Data binding properties for UI
        private int _selectedSheetsCount = 0;
        public int SelectedSheetsCount
        {
            get => _selectedSheetsCount;
            set { _selectedSheetsCount = value; OnPropertyChanged(); }
        }

        public List<string> SelectedFormatsList => GetSelectedFormatsList();

        // Methods
        public List<string> GetSelectedFormatsList()
        {
            var formats = new List<string>();
            foreach (var format in _selectedFormats)
            {
                if (format.Value)
                    formats.Add(format.Key);
            }
            return formats;
        }

        public void SetFormatSelection(string format, bool isSelected)
        {
            if (_selectedFormats.ContainsKey(format))
            {
                _selectedFormats[format] = isSelected;
                OnPropertyChanged(nameof(SelectedFormats));
                OnPropertyChanged($"Is{format}Selected");
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Supporting enums for ExportSettings
    public enum PSQualityType
    {
        Low,
        Medium, 
        High,
        Maximum
    }

    public enum PSPaperPlacement
    {
        Center,
        OffsetFromCorner
    }

    public enum PSPaperMargin
    {
        NoMargin,
        PrinterLimit,
        UserDefined
    }

    public enum PSHiddenLineViews
    {
        VectorProcessing,
        RasterProcessing
    }

    public enum PSRasterQuality
    {
        Low = 72,
        Medium = 150,
        High = 300,
        Maximum = 600
    }

    public enum PSColors
    {
        BlackAndWhite,
        Grayscale,
        Color
    }
}