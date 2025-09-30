using System.ComponentModel;

namespace ProSheetsAddin.Models
{
    public class SheetItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _number;
        private string _name;
        private string _customDrawingNumber;

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

        public string Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyChanged(nameof(Number));
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string CustomDrawingNumber
        {
            get => _customDrawingNumber;
            set
            {
                if (_customDrawingNumber != value)
                {
                    _customDrawingNumber = value;
                    OnPropertyChanged(nameof(CustomDrawingNumber));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}