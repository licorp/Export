using System;
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

        public string ViewId { get; set; }
        public string ViewName { get; set; }
        public string ViewType { get; set; }
        public string Scale { get; set; }
        public string DetailLevel { get; set; }
        public ElementId RevitViewId { get; set; }

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
                ViewType = revitView.ViewType.ToString();
                Scale = revitView.Scale.ToString();
                DetailLevel = revitView.DetailLevel.ToString();
                CustomFileName = ViewName;
                IsSelected = false;
            }
        }
    }
}