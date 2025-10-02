using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProSheetsAddin.Models;
using Autodesk.Revit.DB;

namespace ProSheetsAddin.Views
{
    /// <summary>
    /// Custom File Name Dialog for configuring parameter-based file naming
    /// Allows users to select parameters and configure prefix/suffix/separator
    /// </summary>
    public partial class CustomFileNameDialog : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ParameterInfo> _availableParameters = new ObservableCollection<ParameterInfo>();
        private ObservableCollection<ParameterInfo> _filteredParameters = new ObservableCollection<ParameterInfo>();
        private ObservableCollection<SelectedParameterInfo> _selectedParameters = new ObservableCollection<SelectedParameterInfo>();
        private ParameterInfo _selectedAvailableParameter;
        private string _previewText;
        private Document _document;

        public ObservableCollection<ParameterInfo> AvailableParameters
        {
            get => _filteredParameters;
            set { _filteredParameters = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SelectedParameterInfo> SelectedParameters
        {
            get => _selectedParameters;
            set { _selectedParameters = value; OnPropertyChanged(); }
        }

        public ParameterInfo SelectedAvailableParameter
        {
            get => _selectedAvailableParameter;
            set { _selectedAvailableParameter = value; OnPropertyChanged(); }
        }

        public string PreviewText
        {
            get => _previewText;
            set { _previewText = value; OnPropertyChanged(); }
        }

        public CustomFileNameDialog(Document document = null)
        {
            try
            {
                InitializeComponent();
                DataContext = this;
                _document = document;
                
                LoadAvailableParameters();
                LoadDefaultConfiguration();
                UpdatePreview();
                
                // Subscribe to preview changes
                _selectedParameters.CollectionChanged += (s, e) => UpdatePreview();
                
                System.Diagnostics.Debug.WriteLine("CustomFileNameDialog initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in CustomFileNameDialog constructor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error initializing Custom File Name Dialog:\n\n{ex.Message}\n\nPlease check the debug output for details.", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Load all available parameters from Revit
        /// </summary>
        private void LoadAvailableParameters()
        {
            try
            {
                _availableParameters.Clear();
                System.Diagnostics.Debug.WriteLine("Starting LoadAvailableParameters...");

                // Add common built-in sheet parameters
                var commonParams = new[]
                {
                    // Identity Data
                    new ParameterInfo { Name = "Sheet Number", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Sheet Name", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Current Revision", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Current Revision Date", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Current Revision Description", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Current Revision Issued By", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Current Revision Issued To", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Approved By", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Checked By", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Designed By", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Drawn By", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Sheet Issue Date", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Dependency", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Referencing Sheet", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "Scale", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    new ParameterInfo { Name = "View Template", Type = "Text", Category = "Identity Data", IsBuiltIn = true },
                    
                    // Project Information
                    new ParameterInfo { Name = "Project Name", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    new ParameterInfo { Name = "Project Number", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    new ParameterInfo { Name = "Client Name", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    new ParameterInfo { Name = "Project Address", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    new ParameterInfo { Name = "Author", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    new ParameterInfo { Name = "Building Name", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    new ParameterInfo { Name = "Issue Date", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    new ParameterInfo { Name = "Project Status", Type = "Text", Category = "Project Information", IsBuiltIn = false },
                    
                    // IFC Parameters
                    new ParameterInfo { Name = "IfcBuilding GUID", Type = "Text", Category = "IFC Parameters", IsBuiltIn = false },
                    new ParameterInfo { Name = "IfcProject GUID", Type = "Text", Category = "IFC Parameters", IsBuiltIn = false },
                    new ParameterInfo { Name = "IfcSite GUID", Type = "Text", Category = "IFC Parameters", IsBuiltIn = false }
                };

                foreach (var param in commonParams.OrderBy(p => p.Name))
                {
                    _availableParameters.Add(param);
                }
                
                System.Diagnostics.Debug.WriteLine($"Added {commonParams.Length} built-in parameters");

                // Load from Revit document if available
                if (_document != null)
                {
                    System.Diagnostics.Debug.WriteLine("Loading parameters from document...");
                    LoadParametersFromDocument();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No document provided, skipping document parameters");
                }

                // Initialize filtered list
                ApplyParameterFilter("");
                System.Diagnostics.Debug.WriteLine($"LoadAvailableParameters completed. Total: {_availableParameters.Count} parameters");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in LoadAvailableParameters: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Don't throw - allow dialog to open with default parameters
            }
        }

        /// <summary>
        /// Load project parameters from Revit document
        /// </summary>
        private void LoadParametersFromDocument()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Attempting to load parameters from document...");
                
                // Get all ViewSheets to extract their parameters
                var sheets = new FilteredElementCollector(_document)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .FirstOrDefault();

                if (sheets != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found sheet, extracting parameters...");
                    int paramCount = 0;
                    
                    foreach (Parameter param in sheets.Parameters)
                    {
                        try
                        {
                            if (param.Definition != null)
                            {
                                var paramInfo = new ParameterInfo
                                {
                                    Name = param.Definition.Name,
                                    Type = param.StorageType.ToString(),
                                    Category = param.IsShared ? "Shared Parameters" : "Project Parameters",
                                    IsBuiltIn = param.IsReadOnly
                                };

                                // Add if not already exists
                                if (!_availableParameters.Any(p => p.Name == paramInfo.Name))
                                {
                                    _availableParameters.Add(paramInfo);
                                    paramCount++;
                                }
                            }
                        }
                        catch (Exception paramEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing parameter: {paramEx.Message}");
                            // Continue with next parameter
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Loaded {paramCount} additional parameters from sheet");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No sheets found in document");
                }

                System.Diagnostics.Debug.WriteLine($"Total parameters available: {_availableParameters.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in LoadParametersFromDocument: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Don't throw - continue with built-in parameters only
            }
        }

        /// <summary>
        /// Load default configuration: Sheet Number - Current Revision - Sheet Name
        /// </summary>
        private void LoadDefaultConfiguration()
        {
            var defaultParams = new[]
            {
                new SelectedParameterInfo
                {
                    ParameterName = "Sheet Number",
                    Prefix = "",
                    Suffix = "",
                    Separator = "-",
                    SampleValue = "A101"
                },
                new SelectedParameterInfo
                {
                    ParameterName = "Current Revision",
                    Prefix = "",
                    Suffix = "",
                    Separator = "-",
                    SampleValue = "Rev A"
                },
                new SelectedParameterInfo
                {
                    ParameterName = "Sheet Name",
                    Prefix = "",
                    Suffix = "",
                    Separator = "",
                    SampleValue = "Floor Plan"
                }
            };

            foreach (var param in defaultParams)
            {
                param.PreviewChanged += UpdatePreview;
                _selectedParameters.Add(param);
            }
        }

        /// <summary>
        /// Apply search filter to available parameters
        /// </summary>
        private void ApplyParameterFilter(string searchText)
        {
            _filteredParameters.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText) 
                ? _availableParameters 
                : _availableParameters.Where(p => 
                    p.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Category.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var param in filtered.OrderBy(p => p.Category).ThenBy(p => p.Name))
            {
                _filteredParameters.Add(param);
            }

            OnPropertyChanged(nameof(AvailableParameters));
        }

        /// <summary>
        /// Update the preview text based on current configuration
        /// </summary>
        private void UpdatePreview()
        {
            if (_selectedParameters.Count == 0)
            {
                PreviewText = "Sheet Number-Rev A-Floor Plan";
                return;
            }

            var previewParts = _selectedParameters.Select(p =>
            {
                string value = string.IsNullOrEmpty(p.SampleValue) ? p.ParameterName : p.SampleValue;
                return $"{p.Prefix}{value}{p.Suffix}";
            }).Where(part => !string.IsNullOrEmpty(part));

            var separator = _selectedParameters.FirstOrDefault()?.Separator ?? "-";
            PreviewText = string.Join(separator, previewParts);
        }

        /// <summary>
        /// Get sample value for a parameter
        /// </summary>
        private string GetSampleValue(string parameterName)
        {
            switch (parameterName)
            {
                case "Sheet Number": return "A101";
                case "Sheet Name": return "Floor Plan";
                case "Current Revision": return "Rev A";
                case "Current Revision Date": return "2025-10-02";
                case "Project Name": return "Project Name";
                case "Project Number": return "2025-001";
                case "Drawn By": return "JDoe";
                case "Checked By": return "JSmith";
                case "Sheet Issue Date": return "2025-10-02";
                default: return parameterName;
            }
        }

        #region Event Handlers

        private void SearchParameters_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyParameterFilter(SearchParametersTextBox.Text);
        }

        private void AvailableParametersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddParameter_Click(sender, null);
        }

        private void AddParameter_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAvailableParameter != null)
            {
                // Check if already added
                if (_selectedParameters.Any(p => p.ParameterName == SelectedAvailableParameter.Name))
                {
                    MessageBox.Show($"Parameter '{SelectedAvailableParameter.Name}' is already added.",
                                  "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var selectedParam = new SelectedParameterInfo
                {
                    ParameterName = SelectedAvailableParameter.Name,
                    Prefix = "",
                    Suffix = "",
                    Separator = "-",
                    SampleValue = GetSampleValue(SelectedAvailableParameter.Name)
                };

                selectedParam.PreviewChanged += UpdatePreview;
                _selectedParameters.Add(selectedParam);
                UpdatePreview();
            }
        }

        private void RemoveParameter_Click(object sender, RoutedEventArgs e)
        {
            if (ParametersDataGrid.SelectedItem is SelectedParameterInfo selected)
            {
                _selectedParameters.Remove(selected);
                UpdatePreview();
            }
            else
            {
                MessageBox.Show("Please select a parameter to remove.",
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (ParametersDataGrid.SelectedItem is SelectedParameterInfo selected)
            {
                int index = _selectedParameters.IndexOf(selected);
                if (index > 0)
                {
                    _selectedParameters.Move(index, index - 1);
                    ParametersDataGrid.SelectedIndex = index - 1;
                    UpdatePreview();
                }
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (ParametersDataGrid.SelectedItem is SelectedParameterInfo selected)
            {
                int index = _selectedParameters.IndexOf(selected);
                if (index < _selectedParameters.Count - 1)
                {
                    _selectedParameters.Move(index, index + 1);
                    ParametersDataGrid.SelectedIndex = index + 1;
                    UpdatePreview();
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAvailableParameters();
            UpdatePreview();
            MessageBox.Show("Parameters list refreshed successfully!",
                          "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedParameters.Count == 0)
            {
                MessageBox.Show("Please add at least one parameter to the configuration.",
                              "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
