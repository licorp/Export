using System;
using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace ProSheetsAddin.Models
{
    /// <summary>
    /// Model for representing Revit Views in the UI
    /// </summary>
    public class ViewItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _customFileName;
        private string _viewType;

        public string ViewId { get; set; }
        public string ViewName { get; set; }
        
        public string ViewType
        {
            get => _viewType;
            set
            {
                if (_viewType != value)
                {
                    _viewType = value;
                    OnPropertyChanged(nameof(ViewType));
                }
            }
        }
        
        public string Scale { get; set; }
        public string DetailLevel { get; set; }
        public string Discipline { get; set; }
        public ElementId RevitViewId { get; set; }
        
        // For dropdown in DataGrid
        public List<string> AvailableViewTypes { get; set; } = new List<string>
        {
            "3D", "Rendering", "Section", "Elevation", "Floor Plan", "Detail", "Legend"
        };

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string CustomFileName
        {
            get => _customFileName;
            set
            {
                if (_customFileName != value)
                {
                    _customFileName = value;
                    OnPropertyChanged(nameof(CustomFileName));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ViewItem()
        {
            IsSelected = false;
            CustomFileName = ViewName;
        }

        public ViewItem(View revitView)
        {
            if (revitView != null)
            {
                RevitViewId = revitView.Id;
                ViewId = revitView.Id.IntegerValue.ToString();
                ViewName = revitView.Name;
                ViewType = GetViewTypeString(revitView.ViewType);
                Scale = GetViewScale(revitView);
                DetailLevel = revitView.DetailLevel.ToString();
                Discipline = GetViewDiscipline(revitView);
                CustomFileName = ViewName;
                IsSelected = false;
            }
        }
        
        private string GetViewTypeString(ViewType viewType)
        {
            switch (viewType)
            {
                case Autodesk.Revit.DB.ViewType.ThreeD: return "3D";
                case Autodesk.Revit.DB.ViewType.FloorPlan: return "Floor Plan";
                case Autodesk.Revit.DB.ViewType.Elevation: return "Elevation";
                case Autodesk.Revit.DB.ViewType.Section: return "Section";
                case Autodesk.Revit.DB.ViewType.Detail: return "Detail";
                case Autodesk.Revit.DB.ViewType.Rendering: return "Rendering";
                case Autodesk.Revit.DB.ViewType.Legend: return "Legend";
                default: return viewType.ToString();
            }
        }
        
        private string GetViewScale(View view)
        {
            try
            {
                var scale = view.Scale;
                return scale > 0 ? $"1 : {scale}" : "Custom";
            }
            catch
            {
                return "Custom";
            }
        }
        
        private string GetViewDiscipline(View view)
        {
            try
            {
                var disciplineParam = view.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE);
                return disciplineParam?.AsValueString() ?? "Architectural";
            }
            catch
            {
                return "Architectural";
            }
        }
    }
}