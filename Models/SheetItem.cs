using System.ComponentModel;
using Autodesk.Revit.DB;

namespace ProSheetsAddin.Models
{
    public class SheetItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _sheetNumber;
        private string _sheetName;
        private string _revision;
        private string _size;
        private string _customFileName;
        private ElementId _id;

        public ElementId Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

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

        public string SheetNumber
        {
            get => _sheetNumber;
            set
            {
                if (_sheetNumber != value)
                {
                    _sheetNumber = value;
                    OnPropertyChanged(nameof(SheetNumber));
                }
            }
        }

        public string SheetName
        {
            get => _sheetName;
            set
            {
                if (_sheetName != value)
                {
                    _sheetName = value;
                    OnPropertyChanged(nameof(SheetName));
                }
            }
        }

        public string Revision
        {
            get => _revision;
            set
            {
                if (_revision != value)
                {
                    _revision = value;
                    OnPropertyChanged(nameof(Revision));
                }
            }
        }

        public string Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnPropertyChanged(nameof(Size));
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

        // Legacy properties for backward compatibility
        public string Number
        {
            get => SheetNumber;
            set => SheetNumber = value;
        }

        public string Name
        {
            get => SheetName;
            set => SheetName = value;
        }

        public string CustomDrawingNumber
        {
            get => CustomFileName;
            set => CustomFileName = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}