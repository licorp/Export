using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSheetsAddin.Models;

namespace ProSheetsAddin.Managers
{
    public class ProfileManager
    {
        private readonly string _profilesPath;
        private List<ExportProfile> _profiles;

        public ProfileManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var proSheetsFolder = Path.Combine(appDataPath, "ProSheetsAddin");
            
            if (!Directory.Exists(proSheetsFolder))
                Directory.CreateDirectory(proSheetsFolder);
                
            _profilesPath = Path.Combine(proSheetsFolder, "profiles.json");
            LoadProfiles();
        }

        public List<ExportProfile> GetProfiles()
        {
            return _profiles ?? new List<ExportProfile>();
        }

        public void SaveProfile(ExportProfile profile)
        {
            if (_profiles == null)
                _profiles = new List<ExportProfile>();

            var existingProfile = _profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existingProfile != null)
            {
                _profiles.Remove(existingProfile);
            }

            _profiles.Add(profile);
            SaveProfiles();
        }

        public void SaveProfile(string profileName, ExportProfile profile)
        {
            profile.Name = profileName;
            SaveProfile(profile);
        }

        public void DeleteProfile(string profileName)
        {
            if (_profiles == null) return;

            var profile = _profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile != null)
            {
                _profiles.Remove(profile);
                SaveProfiles();
            }
        }

        public ExportProfile GetProfile(string name)
        {
            return _profiles?.FirstOrDefault(p => p.Name == name);
        }

        private void LoadProfiles()
        {
            try
            {
                _profiles = CreateDefaultProfiles();
            }
            catch (Exception ex)
            {
                _profiles = CreateDefaultProfiles();
            }
        }

        private void SaveProfiles()
        {
            try
            {
                // Simplified - just keep profiles in memory for now
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        private List<ExportProfile> CreateDefaultProfiles()
        {
            return new List<ExportProfile>
            {
                new ExportProfile
                {
                    Name = "Mặc định PDF",
                    Description = "Xuất PDF chất lượng cao",
                    ExportFormat = PSExportFormat.PDF,
                    PdfSettings = new PSPDFExportSettings
                    {
                        Resolution = 300,
                        ColorDepth = PSColorDepth.Color,
                        ZoomType = PSZoomType.FitToPage,
                        PaperSize = PSPaperSize.ISO_A3,
                        HideCropBoundaries = true,
                        HideReferencePlane = true,
                        HideScopeBoxes = true,
                        HideUnreferencedViewTags = true,
                        MaskCoincidentLines = true,
                        ExportLinks = true,
                        RasterQuality = PSRasterQualityType.High
                    }
                },
                new ExportProfile
                {
                    Name = "Mặc định DWG",
                    Description = "Xuất DWG AutoCAD 2018",
                    ExportFormat = PSExportFormat.DWG,
                    DwgSettings = new PSDWGExportSettings
                    {
                        DWGVersion = "2018",
                        UseSharedCoordinates = false
                    }
                }
            };
        }
    }
}