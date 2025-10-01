using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ProSheetsAddin.Models;
using Newtonsoft.Json;

namespace ProSheetsAddin.Managers
{
    public class ProfileManager
    {
        private readonly string _profilesPath;
        private readonly string _prosheeetsProfilesPath;
        private ObservableCollection<ProSheetsProfile> _profiles;

        public ObservableCollection<ProSheetsProfile> Profiles => _profiles ?? new ObservableCollection<ProSheetsProfile>();

        public ProfileManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var proSheetsFolder = Path.Combine(appDataPath, "ProSheetsAddin");
            var diRootsFolder = Path.Combine(appDataPath, "DiRoots", "ProSheets");
            
            if (!Directory.Exists(proSheetsFolder))
                Directory.CreateDirectory(proSheetsFolder);
                
            _profilesPath = Path.Combine(proSheetsFolder, "profiles.json");
            _prosheeetsProfilesPath = diRootsFolder; // Thư mục ProSheets gốc
            LoadProfiles();
        }

        public List<ProSheetsProfile> GetProfiles()
        {
            return _profiles?.ToList() ?? new List<ProSheetsProfile>();
        }

        public void SaveProfile(ProSheetsProfile profile)
        {
            if (_profiles == null)
                _profiles = new ObservableCollection<ProSheetsProfile>();

            var existingProfile = _profiles.FirstOrDefault(p => p.ProfileName == profile.ProfileName);
            if (existingProfile != null)
            {
                _profiles.Remove(existingProfile);
            }

            _profiles.Add(profile);
            SaveProfiles();
        }

        public void SaveProfile(string profileName, ProSheetsProfile profile)
        {
            profile.ProfileName = profileName;
            SaveProfile(profile);
        }

        public void DeleteProfile(string profileName)
        {
            if (_profiles == null) return;

            var profile = _profiles.FirstOrDefault(p => p.ProfileName == profileName);
            if (profile != null)
            {
                _profiles.Remove(profile);
                SaveProfiles();
            }
        }

        public ProSheetsProfile GetProfile(string name)
        {
            return _profiles?.FirstOrDefault(p => p.ProfileName == name);
        }

        /// <summary>
        /// Load ProSheets profiles from JSON files (compatible with DiRoots ProSheets)
        /// </summary>
        public void LoadProSheetsProfile(string jsonFilePath)
        {
            try
            {
                if (File.Exists(jsonFilePath))
                {
                    var json = File.ReadAllText(jsonFilePath);
                    var profile = JsonConvert.DeserializeObject<ProSheetsProfile>(json);
                    
                    if (profile != null && !string.IsNullOrEmpty(profile.ProfileName))
                    {
                        SaveProfile(profile);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error loading ProSheets profile: {ex.Message}");
            }
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(_profilesPath))
                {
                    var json = File.ReadAllText(_profilesPath);
                    var profilesList = JsonConvert.DeserializeObject<List<ProSheetsProfile>>(json);
                    _profiles = new ObservableCollection<ProSheetsProfile>(profilesList ?? new List<ProSheetsProfile>());
                }
                else
                {
                    _profiles = CreateDefaultProfiles();
                }
                
                // Tìm profiles từ DiRoots ProSheets nếu có
                LoadExistingProSheetsProfiles();
            }
            catch (Exception ex)
            {
                _profiles = CreateDefaultProfiles();
                System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");
            }
        }

        private void LoadExistingProSheetsProfiles()
        {
            try
            {
                if (Directory.Exists(_prosheeetsProfilesPath))
                {
                    var jsonFiles = Directory.GetFiles(_prosheeetsProfilesPath, "*.json");
                    foreach (var jsonFile in jsonFiles)
                    {
                        LoadProSheetsProfile(jsonFile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading existing ProSheets profiles: {ex.Message}");
            }
        }

        private void SaveProfiles()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_profiles?.ToList(), Formatting.Indented);
                File.WriteAllText(_profilesPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profiles: {ex.Message}");
            }
        }

        private ObservableCollection<ProSheetsProfile> CreateDefaultProfiles()
        {
            return new ObservableCollection<ProSheetsProfile>
            {
                new ProSheetsProfile
                {
                    ProfileName = "Mặc định PDF",
                    OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    SelectedFormats = new List<string> { "PDF" },
                    CreateSeparateFolders = false,
                    PaperSize = "A3",
                    Orientation = "Landscape"
                },
                new ProSheetsProfile
                {
                    ProfileName = "Xuất đầy đủ",
                    OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    SelectedFormats = new List<string> { "PDF", "DWG", "JPG" },
                    CreateSeparateFolders = true,
                    PaperSize = "A3",
                    Orientation = "Landscape"
                }
            };
        }
    }
}