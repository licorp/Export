using System;
using System.Linq;
using System.Windows;
using ProSheetsAddin.Models;
using ProSheetsAddin.Managers;

namespace ProSheetsAddin.Views
{
    /// <summary>
    /// Profile Management functionality for ProSheetsMainWindow
    /// </summary>
    public partial class ProSheetsMainWindow
    {
        /// <summary>
        /// Initialize Profile Manager and load profiles
        /// </summary>
        private void InitializeProfiles()
        {
            WriteDebugLog("Initializing Profile Manager Service");
            try
            {
                _profileManager = new Managers.ProfileManagerService();
                
                // Wire up profile changed event
                _profileManager.ProfileChanged += OnProfileChanged;
                
                // Bind profiles to ComboBox
                ProfileComboBox.ItemsSource = _profileManager.Profiles;
                ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
                
                WriteDebugLog($"Profile Manager initialized with {_profileManager.Profiles.Count} profiles");
                WriteDebugLog($"Current profile: {_profileManager.CurrentProfile?.Name}");
                
                // Apply current profile to UI
                if (_profileManager.CurrentProfile != null)
                {
                    ApplyProfileToUI(_profileManager.CurrentProfile);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error initializing Profile Manager: {ex.Message}");
                MessageBox.Show($"Error initializing profiles: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle profile change event
        /// </summary>
        private void OnProfileChanged(Profile profile)
        {
            WriteDebugLog($"Profile changed event: {profile?.Name}");
            if (profile != null)
            {
                ApplyProfileToUI(profile);
            }
        }

        /// <summary>
        /// Apply profile settings to UI
        /// </summary>
        private void ApplyProfileToUI(Profile profile)
        {
            if (profile?.Settings == null) return;

            WriteDebugLog($"Applying profile '{profile.Name}' to UI");
            try
            {
                var settings = profile.Settings;

                // Apply Create tab settings
                if (!string.IsNullOrEmpty(settings.OutputFolder))
                {
                    OutputFolder = settings.OutputFolder;
                }

                // Apply Format settings
                if (ExportSettings != null)
                {
                    ExportSettings.IsPdfSelected = settings.PDFEnabled;
                    ExportSettings.IsDwgSelected = settings.DWGEnabled;
                    ExportSettings.IsDgnSelected = settings.DGNEnabled;
                    ExportSettings.IsIfcSelected = settings.IFCEnabled;
                    ExportSettings.IsImgSelected = settings.IMGEnabled;
                    
                    ExportSettings.HideCropBoundaries = settings.HideCropBoundaries;
                    ExportSettings.HideScopeBoxes = settings.HideScopeBoxes;
                    ExportSettings.CreateSeparateFolders = !settings.SaveAllInSameFolder;
                }

                WriteDebugLog($"Profile '{profile.Name}' applied successfully");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error applying profile '{profile?.Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Save current UI settings to profile
        /// </summary>
        private void SaveCurrentSettingsToProfile(Profile profile)
        {
            if (profile?.Settings == null) return;

            WriteDebugLog($"Saving current settings to profile '{profile.Name}'");
            try
            {
                var settings = profile.Settings;

                // Save Create tab settings
                settings.OutputFolder = OutputFolder ?? "";
                settings.SaveAllInSameFolder = !(ExportSettings?.CreateSeparateFolders ?? false);

                // Save Format settings
                if (ExportSettings != null)
                {
                    settings.PDFEnabled = ExportSettings.IsPdfSelected;
                    settings.DWGEnabled = ExportSettings.IsDwgSelected;
                    settings.DGNEnabled = ExportSettings.IsDgnSelected;
                    settings.IFCEnabled = ExportSettings.IsIfcSelected;
                    settings.IMGEnabled = ExportSettings.IsImgSelected;
                    
                    settings.HideCropBoundaries = ExportSettings.HideCropBoundaries;
                    settings.HideScopeBoxes = ExportSettings.HideScopeBoxes;
                }

                // Save Selection settings
                settings.SelectedSheetNumbers = Sheets?
                    .Where(s => s.IsSelected)
                    .Select(s => s.SheetNumber)
                    .ToList() ?? new System.Collections.Generic.List<string>();

                _profileManager.SaveProfile(profile);
                WriteDebugLog($"Profile '{profile.Name}' saved successfully");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error saving settings to profile '{profile?.Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Profile ComboBox selection changed
        /// </summary>
        private void ProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is Profile selectedProfile)
            {
                WriteDebugLog($"Profile selected: {selectedProfile.Name}");
                _selectedProfile = selectedProfile;
                _profileManager.SwitchProfile(selectedProfile);
            }
        }

        /// <summary>
        /// Add new profile button clicked
        /// </summary>
        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Add Profile clicked");
            try
            {
                var dialog = new ProfileNameDialog
                {
                    Owner = this
                };
                
                if (dialog.ShowDialog() == true)
                {
                    string profileName = dialog.ProfileName;
                    var mode = dialog.SelectedMode;
                    WriteDebugLog($"Creating new profile: {profileName}, Mode: {mode}");
                    
                    Models.Profile newProfile;
                    
                    switch (mode)
                    {
                        case ProfileNameDialog.ProfileCreationMode.CopyCurrent:
                            // Create profile and copy current settings
                            newProfile = _profileManager.CreateNewProfile(profileName);
                            if (newProfile != null)
                            {
                                SaveCurrentSettingsToProfile(newProfile);
                                WriteDebugLog($"Profile '{profileName}' created with current settings");
                            }
                            break;
                            
                        case ProfileNameDialog.ProfileCreationMode.UseDefault:
                            // Create profile with default settings (empty)
                            newProfile = _profileManager.CreateNewProfile(profileName);
                            if (newProfile != null)
                            {
                                _profileManager.SaveProfile(newProfile);
                                WriteDebugLog($"Profile '{profileName}' created with default settings");
                            }
                            break;
                            
                        case ProfileNameDialog.ProfileCreationMode.ImportFile:
                            // Import from XML file
                            WriteDebugLog($"ImportFile mode - Starting import from: {dialog.ImportFilePath}");
                            newProfile = _profileManager.CreateNewProfile(profileName);
                            if (newProfile != null)
                            {
                                WriteDebugLog($"New profile created with ID: {newProfile.Id}");
                                try
                                {
                                    // Load settings from XML file
                                    WriteDebugLog($"Loading XML profile from: {dialog.ImportFilePath}");
                                    var xmlProfile = XMLProfileManager.LoadProfileFromXML(dialog.ImportFilePath);
                                    WriteDebugLog($"XML profile loaded: {(xmlProfile != null ? "Success" : "NULL")}");
                                    
                                    if (xmlProfile != null && xmlProfile.TemplateInfo != null && newProfile.Settings != null)
                                    {
                                        var template = xmlProfile.TemplateInfo;
                                        WriteDebugLog($"TemplateInfo found - PDF:{template.IsPDFChecked}, DWG:{template.IsDWGChecked}, IFC:{template.IsIFCChecked}");
                                        
                                        // Copy settings from ProSheetsXMLProfile.TemplateInfo
                                        newProfile.Settings.PDFEnabled = template.IsPDFChecked;
                                        newProfile.Settings.DWGEnabled = template.IsDWGChecked;
                                        newProfile.Settings.DGNEnabled = template.IsDGNChecked;
                                        newProfile.Settings.IFCEnabled = template.IsIFCChecked;
                                        newProfile.Settings.IMGEnabled = template.IsIMGChecked;
                                        newProfile.Settings.HideCropBoundaries = template.HideCropBoundaries;
                                        newProfile.Settings.HideScopeBoxes = template.HideScopeBox;
                                        newProfile.Settings.SaveAllInSameFolder = !template.IsSeparateFile;
                                        
                                        WriteDebugLog($"Settings copied to profile '{profileName}'");
                                        _profileManager.SaveProfile(newProfile);
                                        WriteDebugLog($"Profile '{profileName}' saved successfully");
                                    }
                                    else
                                    {
                                        WriteDebugLog($"Import failed - xmlProfile:{(xmlProfile != null)}, TemplateInfo:{(xmlProfile?.TemplateInfo != null)}, Settings:{(newProfile.Settings != null)}");
                                        MessageBox.Show("Failed to read settings from XML file.", "Import Error",
                                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }
                                }
                                catch (Exception importEx)
                                {
                                    WriteDebugLog($"EXCEPTION importing XML profile: {importEx.Message}");
                                    WriteDebugLog($"Stack trace: {importEx.StackTrace}");
                                    MessageBox.Show($"Error importing profile: {importEx.Message}", "Import Error",
                                                   MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                            else
                            {
                                WriteDebugLog($"Failed to create new profile '{profileName}'");
                            }
                            break;
                            
                        default:
                            return;
                    }
                    
                    if (newProfile != null)
                    {
                        // Switch to the new profile
                        _profileManager.SwitchProfile(newProfile);
                        ProfileComboBox.SelectedItem = newProfile;
                        
                        WriteDebugLog($"New profile '{profileName}' created and selected");
                        MessageBox.Show($"Profile '{profileName}' created successfully!", 
                                       "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error creating profile: {ex.Message}");
                MessageBox.Show($"Error creating profile: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save profile button clicked
        /// </summary>
        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Save Profile clicked");
            try
            {
                var currentProfile = ProfileComboBox.SelectedItem as Profile;
                if (currentProfile != null)
                {
                    SaveCurrentSettingsToProfile(currentProfile);
                    
                    WriteDebugLog($"Profile '{currentProfile.Name}' saved");
                    MessageBox.Show($"Profile '{currentProfile.Name}' saved successfully!", 
                                   "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Please select a profile first.", "Information",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error saving profile: {ex.Message}");
                MessageBox.Show($"Error saving profile: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Delete profile button clicked
        /// </summary>
        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            WriteDebugLog("Delete Profile clicked");
            try
            {
                var currentProfile = ProfileComboBox.SelectedItem as Profile;
                if (currentProfile != null)
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete profile '{currentProfile.Name}'?",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _profileManager.DeleteProfile(currentProfile);
                        WriteDebugLog($"Profile '{currentProfile.Name}' deleted");
                        MessageBox.Show($"Profile '{currentProfile.Name}' deleted successfully!",
                                       "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a profile first.", "Information",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error deleting profile: {ex.Message}");
                MessageBox.Show($"Error deleting profile: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
