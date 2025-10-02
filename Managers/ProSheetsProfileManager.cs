using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using ProSheetsAddin.Models;
using Autodesk.Revit.DB;

namespace ProSheetsAddin.Managers
{
    /// <summary>
    /// Profile Manager for loading and managing ProSheets profiles
    /// Compatible with original DiRoots ProSheets profile format
    /// </summary>
    public class ProSheetsProfileManager
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugStringA(string message);

        private static void WriteDebugLog(string message)
        {
            string logMessage = $"[Export+ ProfileManager] {DateTime.Now:HH:mm:ss.fff} - {message}";
            OutputDebugStringA(logMessage);
        }

        private readonly string _proSheetsProfileFolder;
        private readonly string _exportPlusProfileFolder;

        public ObservableCollection<ProSheetsProfile> Profiles { get; set; }

        public ProSheetsProfileManager()
        {
            WriteDebugLog("ProfileManager constructor started");

            // Original ProSheets profile folder
            _proSheetsProfileFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DiRoots", "ProSheets", "Profiles"
            );

            // Export+ profile folder (our custom folder)
            _exportPlusProfileFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DiRoots", "ExportPlus", "Profiles"
            );

            Profiles = new ObservableCollection<ProSheetsProfile>();
            
            WriteDebugLog($"ProSheets folder: {_proSheetsProfileFolder}");
            WriteDebugLog($"Export+ folder: {_exportPlusProfileFolder}");

            EnsureDirectoriesExist();
            LoadProfiles();
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(_exportPlusProfileFolder))
                {
                    Directory.CreateDirectory(_exportPlusProfileFolder);
                    WriteDebugLog($"Created Export+ profiles directory: {_exportPlusProfileFolder}");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error creating directories: {ex.Message}");
            }
        }

        /// <summary>
        /// Load profiles from both ProSheets and Export+ directories
        /// </summary>
        public void LoadProfiles()
        {
            WriteDebugLog("LoadProfiles started");
            Profiles.Clear();

            // Load original ProSheets profiles if available
            LoadProSheetsProfiles();

            // Load Export+ profiles
            LoadExportPlusProfiles();

            // Add default profile if no profiles found
            if (Profiles.Count == 0)
            {
                var defaultProfile = CreateDefaultProfile();
                Profiles.Add(defaultProfile);
                WriteDebugLog("Added default profile");
            }

            WriteDebugLog($"Total profiles loaded: {Profiles.Count}");
        }

        public void LoadProSheetsProfile(string jsonFilePath)
        {
            try
            {
                if (File.Exists(jsonFilePath))
                {
                    WriteDebugLog($"Loading ProSheets profile from: {jsonFilePath}");
                    
                    // Check if it's XML or JSON
                    string extension = Path.GetExtension(jsonFilePath).ToLower();
                    ProSheetsProfile profile = null;
                    
                    if (extension == ".xml")
                    {
                        // Load XML profile and convert to standard profile
                        var xmlProfile = XMLProfileManager.LoadProfileFromXML(jsonFilePath);
                        if (xmlProfile != null)
                        {
                            profile = XMLProfileManager.ConvertXMLToProfile(xmlProfile);
                        }
                    }
                    else if (extension == ".json")
                    {
                        // Load JSON profile
                        string json = File.ReadAllText(jsonFilePath);
                        profile = JsonConvert.DeserializeObject<ProSheetsProfile>(json);
                    }
                    
                    if (profile != null)
                    {
                        if (string.IsNullOrEmpty(profile.ProfileName))
                        {
                            profile.ProfileName = Path.GetFileNameWithoutExtension(jsonFilePath);
                        }
                        
                        // Check if profile already exists
                        var existingProfile = Profiles.FirstOrDefault(p => p.ProfileName == profile.ProfileName);
                        if (existingProfile != null)
                        {
                            Profiles.Remove(existingProfile);
                        }
                        
                        Profiles.Add(profile);
                        WriteDebugLog($"Profile loaded: {profile.ProfileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error loading ProSheets profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Load XML profile specifically and return SheetFileNameInfo for UI binding
        /// </summary>
        public List<SheetFileNameInfo> LoadXMLProfileWithSheets(string xmlFilePath, List<ViewSheet> sheets)
        {
            try
            {
                WriteDebugLog($"Loading XML profile with sheets: {xmlFilePath}");
                var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlFilePath);
                if (xmlProfile != null)
                {
                    return XMLProfileManager.GenerateCustomFileNames(xmlProfile, sheets);
                }
                return new List<SheetFileNameInfo>();
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error loading XML profile with sheets: {ex.Message}");
                return new List<SheetFileNameInfo>();
            }
        }

        /// <summary>
        /// Get available XML profiles from DiRoots and our folder
        /// </summary>
        public List<string> GetAvailableXMLProfiles()
        {
            return XMLProfileManager.GetAvailableXMLProfiles();
        }

        private void LoadProSheetsProfiles()
        {
            try
            {
                if (Directory.Exists(_proSheetsProfileFolder))
                {
                    var jsonFiles = Directory.GetFiles(_proSheetsProfileFolder, "*.json");
                    WriteDebugLog($"Found {jsonFiles.Length} ProSheets profile files");

                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            var profile = JsonConvert.DeserializeObject<ProSheetsProfile>(json);
                            
                            if (profile != null)
                            {
                                if (string.IsNullOrEmpty(profile.ProfileName))
                                {
                                    profile.ProfileName = Path.GetFileNameWithoutExtension(file);
                                }
                                
                                Profiles.Add(profile);
                                WriteDebugLog($"Loaded ProSheets profile: {profile.ProfileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteDebugLog($"Error loading ProSheets profile {file}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    WriteDebugLog("ProSheets profiles directory not found");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error accessing ProSheets profiles directory: {ex.Message}");
            }
        }

        private void LoadExportPlusProfiles()
        {
            try
            {
                if (Directory.Exists(_exportPlusProfileFolder))
                {
                    var jsonFiles = Directory.GetFiles(_exportPlusProfileFolder, "*.json");
                    WriteDebugLog($"Found {jsonFiles.Length} Export+ profile files");

                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            var profile = JsonConvert.DeserializeObject<ProSheetsProfile>(json);
                            
                            if (profile != null)
                            {
                                if (string.IsNullOrEmpty(profile.ProfileName))
                                {
                                    profile.ProfileName = Path.GetFileNameWithoutExtension(file);
                                }
                                
                                Profiles.Add(profile);
                                WriteDebugLog($"Loaded Export+ profile: {profile.ProfileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteDebugLog($"Error loading Export+ profile {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error accessing Export+ profiles directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Save profile to Export+ directory
        /// </summary>
        public void SaveProfile(ProSheetsProfile profile)
        {
            try
            {
                if (profile == null || string.IsNullOrEmpty(profile.ProfileName))
                {
                    WriteDebugLog("Cannot save profile: profile is null or name is empty");
                    return;
                }

                string fileName = $"{profile.ProfileName}.json";
                string filePath = Path.Combine(_exportPlusProfileFolder, fileName);
                
                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(filePath, json);
                
                WriteDebugLog($"Profile saved: {filePath}");

                // Add to collection if not already present
                if (!Profiles.Any(p => p.ProfileName == profile.ProfileName))
                {
                    Profiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error saving profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete profile (only Export+ profiles can be deleted)
        /// </summary>
        public void DeleteProfile(ProSheetsProfile profile)
        {
            try
            {
                if (profile == null || string.IsNullOrEmpty(profile.ProfileName))
                {
                    WriteDebugLog("Cannot delete profile: profile is null or name is empty");
                    return;
                }

                string fileName = $"{profile.ProfileName}.json";
                string filePath = Path.Combine(_exportPlusProfileFolder, fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Profiles.Remove(profile);
                    WriteDebugLog($"Profile deleted: {profile.ProfileName}");
                }
                else
                {
                    WriteDebugLog($"Profile file not found for deletion: {filePath}");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error deleting profile: {ex.Message}");
            }
        }

        private ProSheetsProfile CreateDefaultProfile()
        {
            return new ProSheetsProfile
            {
                ProfileName = "Default Export+",
                OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                SelectedFormats = new List<string> { "PDF" },
                CreateSeparateFolders = false,
                PaperSize = "Auto",
                Orientation = "Auto",
                PlaceCenterDrawing = true,
                ZoomTo100 = false,
                HideCropRegions = true,
                HideScopeBoxes = true
            };
        }

        /// <summary>
        /// Get profile by name
        /// </summary>
        public ProSheetsProfile GetProfile(string profileName)
        {
            return Profiles.FirstOrDefault(p => p.ProfileName == profileName);
        }

        /// <summary>
        /// Create new profile from current settings
        /// </summary>
        public ProSheetsProfile CreateProfileFromSettings(ExportSettings settings, string profileName)
        {
            var profile = new ProSheetsProfile
            {
                ProfileName = profileName,
                OutputFolder = settings?.OutputFolder ?? "",
                SelectedFormats = settings?.GetSelectedFormatsList() ?? new List<string>(),
                CreateSeparateFolders = settings?.CreateSeparateFolders ?? false,
                HideCropRegions = settings?.HideCropBoundaries ?? true,
                HideScopeBoxes = settings?.HideScopeBoxes ?? true
            };

            return profile;
        }

        /// <summary>
        /// Load profile from external JSON file (import)
        /// </summary>
        public ProSheetsProfile LoadProfileFromFile(string jsonFilePath)
        {
            try
            {
                if (!File.Exists(jsonFilePath))
                {
                    WriteDebugLog($"Profile file not found: {jsonFilePath}");
                    return null;
                }

                string json = File.ReadAllText(jsonFilePath);
                var profile = JsonConvert.DeserializeObject<ProSheetsProfile>(json);
                
                if (profile != null)
                {
                    WriteDebugLog($"Profile loaded from file: {profile.ProfileName}");
                    return profile;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error loading profile from file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Export profile to external JSON file
        /// </summary>
        public void ExportProfileToFile(ProSheetsProfile profile, string jsonFilePath)
        {
            try
            {
                if (profile == null)
                {
                    WriteDebugLog("Cannot export profile: profile is null");
                    return;
                }

                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(jsonFilePath, json);
                
                WriteDebugLog($"Profile exported to: {jsonFilePath}");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error exporting profile to file: {ex.Message}");
                throw;
            }
        }
    }
}