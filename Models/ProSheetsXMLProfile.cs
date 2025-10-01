using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;

namespace ProSheetsAddin.Models
{
    [XmlRoot("Profiles")]
    public class ProSheetsProfileList
    {
        [XmlElement("Profile")]
        public List<ProSheetsXMLProfile> Profiles { get; set; } = new List<ProSheetsXMLProfile>();
    }

    public class ProSheetsXMLProfile
    {
        public string Name { get; set; }
        public bool IsCurrent { get; set; }
        public string FilePath { get; set; }
        public string Version { get; set; }
        
        [XmlElement("TemplateInfo")]
        public TemplateInfo TemplateInfo { get; set; } = new TemplateInfo();
    }

    public class TemplateInfo
    {
        // Format Settings
        [XmlElement("DWF")]
        public DWFSettings DWF { get; set; } = new DWFSettings();
        
        [XmlElement("IFC")]
        public IFCSettings IFC { get; set; } = new IFCSettings();
        
        [XmlElement("IMG")]
        public IMGSettings IMG { get; set; } = new IMGSettings();
        
        // Export Format Flags
        public bool IsPDFChecked { get; set; }
        public bool IsDWGChecked { get; set; }
        public bool IsDGNChecked { get; set; }
        public bool IsIFCChecked { get; set; }
        public bool IsIMGChecked { get; set; }
        public bool IsNWCChecked { get; set; }
        public bool IsDWFChecked { get; set; }
        
        // Selection Settings
        [XmlElement("SelectionSheets")]
        public SelectionSettings SelectionSheets { get; set; } = new SelectionSettings();
        
        [XmlElement("CustomFileNameParameters")]
        public CustomFileNameParameters CustomFileNameParameters { get; set; } = new CustomFileNameParameters();
        
        // Common Export Settings
        public bool IsSeparateFile { get; set; } = true;
        public bool IsFileNameSet { get; set; }
        public bool HideCropBoundaries { get; set; } = true;
        public bool HideScopeBox { get; set; } = true;
        public string PaperSize { get; set; } = "Default";
        public string Color { get; set; } = "Color";
    }

    public class DWFSettings
    {
        public bool IsDwfx { get; set; }
        public string PaperSize { get; set; } = "Default";
        public string optImageFormat { get; set; } = "Lossless";
        public string optImageQuality { get; set; } = "Default";
        public bool optCropBoxVisible { get; set; }
        public bool optExportingAreas { get; set; }
        public bool IsFitToPage { get; set; } = true;
        public string RasterQuality { get; set; } = "Low";
    }

    public class IFCSettings
    {
        public string FileVersion { get; set; } = "IFC2x3CV2";
        public string IFCFileType { get; set; } = "Ifc";
        public string SpaceBoundaries { get; set; } = "None";
        public string SitePlacement { get; set; } = "Current Shared Coordinates";
        public bool WallAndColumnSplitting { get; set; }
        public bool IncludeSteelElements { get; set; } = true;
        public bool Export2DElements { get; set; }
        public bool ExportLinkedFiles { get; set; }
        public bool ExportBaseQuantities { get; set; }
        public bool ExportIFCCommonPropertySets { get; set; } = true;
        public string TessellationLevelOfDetail { get; set; } = "Low";
    }

    public class IMGSettings
    {
        public bool IsCombineHTML { get; set; }
        public string objCombineFilename { get; set; }
        public string FitDirection { get; set; } = "Horizontal";
        public string HLRandWFViewsFileType { get; set; } = "PNG";
        public string ImageResolution { get; set; } = "DPI_72";
        public string PixelSize { get; set; } = "2048";
        public string ShadowViewsFileType { get; set; } = "PNG";
        public string Zoom { get; set; } = "50";
        public string ZoomType { get; set; } = "FitToPage";
    }

    public class SelectionSettings
    {
        public string SelectionType { get; set; } = "Sheet";
        public bool IsLableCheked { get; set; }
        public bool IsFieldSeparatorChecked { get; set; } = true;
        public string FieldSeparator { get; set; } = "-";
        
        [XmlArray("ViewIds")]
        [XmlArrayItem("ViewId")]
        public List<string> ViewIds { get; set; } = new List<string>();
    }

    public class CustomFileNameParameters
    {
        [XmlElement("CombineParameters")]
        public CombineParameters CombineParameters { get; set; } = new CombineParameters();
    }

    public class CombineParameters
    {
        [XmlElement("ParameterModel")]
        public List<ParameterModel> ParameterModels { get; set; } = new List<ParameterModel>();
    }

    public class ParameterModel
    {
        public string ParameterName { get; set; }
        public string StorageType { get; set; }
        public string ParameterId { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public bool IsProjectParameter { get; set; }
        public bool IsCustomParameter { get; set; }
    }

    public class SheetFileNameInfo : INotifyPropertyChanged
    {
        public string SheetId { get; set; }
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string Revision { get; set; }
        public string Size { get; set; }
        
        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set 
            { 
                _isSelected = value; 
                OnPropertyChanged(nameof(IsSelected));
            }
        }
        
        private string _customFileName;
        public string CustomFileName
        {
            get => _customFileName;
            set 
            { 
                _customFileName = value; 
                OnPropertyChanged(nameof(CustomFileName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}