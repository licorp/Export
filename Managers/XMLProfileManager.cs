using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using Autodesk.Revit.DB;
using ProSheetsAddin.Models;

namespace ProSheetsAddin.Managers
{
    public class XMLProfileManager
    {
        private static string ProfilesFolder => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                         "ProSheetsAddin", "Profiles");

        private static string DiRootsProfilesFolder => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                         "DiRoots", "ProSheets");

        static XMLProfileManager()
        {
            if (!Directory.Exists(ProfilesFolder))
                Directory.CreateDirectory(ProfilesFolder);
        }

        public static ProSheetsXMLProfile LoadProfileFromXML(string filePath)
        {
            try
            {
                WriteDebugLog($"Loading XML profile from: {filePath}");
                var serializer = new XmlSerializer(typeof(ProSheetsProfileList));
                using (var reader = new StreamReader(filePath))
                {
                    var profileList = (ProSheetsProfileList)serializer.Deserialize(reader);
                    var profile = profileList.Profiles.FirstOrDefault();
                    if (profile != null)
                    {
                        WriteDebugLog($"XML profile loaded: {profile.Name}");
                    }
                    return profile;
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error loading XML profile: {ex.Message}");
                throw new Exception($"Error loading profile: {ex.Message}");
            }
        }

        public static void SaveProfileToXML(ProSheetsXMLProfile profile, string filePath)
        {
            try
            {
                WriteDebugLog($"Saving XML profile to: {filePath}");
                var profileList = new ProSheetsProfileList();
                profileList.Profiles.Add(profile);

                var serializer = new XmlSerializer(typeof(ProSheetsProfileList));
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, profileList);
                }
                WriteDebugLog($"XML profile saved successfully");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error saving XML profile: {ex.Message}");
                throw new Exception($"Error saving profile: {ex.Message}");
            }
        }

        public static List<string> GetAvailableXMLProfiles()
        {
            var profiles = new List<string>();
            
            // Load from our folder
            if (Directory.Exists(ProfilesFolder))
            {
                var ourProfiles = Directory.GetFiles(ProfilesFolder, "*.xml")
                                           .Select(Path.GetFileNameWithoutExtension);
                profiles.AddRange(ourProfiles);
            }

            // Load from DiRoots ProSheets folder
            if (Directory.Exists(DiRootsProfilesFolder))
            {
                var diRootsProfiles = Directory.GetFiles(DiRootsProfilesFolder, "*.xml")
                                              .Select(f => "DiRoots: " + Path.GetFileNameWithoutExtension(f));
                profiles.AddRange(diRootsProfiles);
            }

            WriteDebugLog($"Found {profiles.Count} XML profiles");
            return profiles;
        }

        public static List<SheetFileNameInfo> GenerateCustomFileNames(
            ProSheetsXMLProfile profile, 
            List<ViewSheet> sheets)
        {
            WriteDebugLog($"Generating custom file names for {sheets.Count} sheets");
            var result = new List<SheetFileNameInfo>();
            var parameters = profile.TemplateInfo.CustomFileNameParameters.CombineParameters.ParameterModels;
            
            foreach (var sheet in sheets)
            {
                var fileName = BuildCustomFileName(sheet, parameters, profile.TemplateInfo.SelectionSheets.FieldSeparator);
                
                result.Add(new SheetFileNameInfo
                {
                    SheetId = sheet.Id.IntegerValue.ToString(),
                    SheetNumber = sheet.SheetNumber,
                    SheetName = sheet.Name,
                    Revision = GetSheetRevision(sheet),
                    Size = GetSheetPaperSize(sheet),
                    CustomFileName = fileName,
                    IsSelected = true
                });
            }
            
            WriteDebugLog($"Generated {result.Count} custom file names");
            return result;
        }

        private static string BuildCustomFileName(ViewSheet sheet, List<ParameterModel> parameters, string separator)
        {
            var parts = new List<string>();
            
            foreach (var param in parameters)
            {
                string value = GetParameterValue(sheet, param.ParameterName);
                if (!string.IsNullOrEmpty(value))
                {
                    string part = $"{param.Prefix}{value}{param.Suffix}";
                    parts.Add(part);
                }
            }
            
            var fileName = string.Join(separator, parts.Where(p => !string.IsNullOrEmpty(p)));
            
            // Fallback to sheet number if no custom name generated
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = sheet.SheetNumber;
            }
            
            return fileName;
        }

        private static string GetParameterValue(ViewSheet sheet, string parameterName)
        {
            try
            {
                switch (parameterName)
                {
                    case "Sheet Number":
                        return sheet.SheetNumber;
                    case "Sheet Name":
                        return sheet.Name;
                    case "Current Revision":
                        return GetSheetRevision(sheet);
                    default:
                        var param = sheet.LookupParameter(parameterName);
                        if (param != null)
                        {
                            switch (param.StorageType)
                            {
                                case StorageType.String:
                                    return param.AsString() ?? "";
                                case StorageType.Integer:
                                    return param.AsInteger().ToString();
                                case StorageType.Double:
                                    return param.AsDouble().ToString();
                                default:
                                    return param.AsValueString() ?? "";
                            }
                        }
                        return "";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string GetSheetRevision(ViewSheet sheet)
        {
            try
            {
                var revParam = sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION);
                return revParam?.AsString() ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string GetSheetPaperSize(ViewSheet sheet)
        {
            try
            {
                // Try to get paper size from title block
                var titleBlocks = new FilteredElementCollector(sheet.Document, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsNotElementType()
                    .ToElements();

                if (titleBlocks.Any())
                {
                    var titleBlock = titleBlocks.First();
                    var sizeParam = titleBlock.LookupParameter("Sheet Size");
                    if (sizeParam != null)
                    {
                        return sizeParam.AsString() ?? "A3";
                    }
                }

                // Fallback: detect from sheet dimensions
                var outline = sheet.Outline;
                var width = outline.Max.U - outline.Min.U;
                var height = outline.Max.V - outline.Min.V;
                
                // Convert to mm (assuming feet)
                var widthMm = width * 304.8;
                var heightMm = height * 304.8;
                
                // Common paper sizes in mm
                if (Math.Abs(widthMm - 420) < 50 && Math.Abs(heightMm - 297) < 50) return "A3";
                if (Math.Abs(widthMm - 297) < 50 && Math.Abs(heightMm - 210) < 50) return "A4";
                if (Math.Abs(widthMm - 594) < 50 && Math.Abs(heightMm - 420) < 50) return "A2";
                if (Math.Abs(widthMm - 841) < 50 && Math.Abs(heightMm - 594) < 50) return "A1";
                if (Math.Abs(widthMm - 1189) < 50 && Math.Abs(heightMm - 841) < 50) return "A0";
                
                return "A3"; // Default
            }
            catch (Exception)
            {
                return "A3";
            }
        }

        public static ProSheetsProfile ConvertXMLToProfile(ProSheetsXMLProfile xmlProfile)
        {
            WriteDebugLog($"Converting XML profile to standard profile: {xmlProfile.Name}");
            var profile = new ProSheetsProfile
            {
                ProfileName = xmlProfile.Name,
                OutputFolder = xmlProfile.FilePath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                CreateSeparateFolders = xmlProfile.TemplateInfo.IsSeparateFile,
                HideCropRegions = xmlProfile.TemplateInfo.HideCropBoundaries,
                HideScopeBoxes = xmlProfile.TemplateInfo.HideScopeBox,
                PaperSize = xmlProfile.TemplateInfo.PaperSize,
                SelectedFormats = new List<string>()
            };

            // Convert format flags
            if (xmlProfile.TemplateInfo.IsPDFChecked) profile.SelectedFormats.Add("PDF");
            if (xmlProfile.TemplateInfo.IsDWGChecked) profile.SelectedFormats.Add("DWG");
            if (xmlProfile.TemplateInfo.IsDGNChecked) profile.SelectedFormats.Add("DGN");
            if (xmlProfile.TemplateInfo.IsIFCChecked) profile.SelectedFormats.Add("IFC");
            if (xmlProfile.TemplateInfo.IsIMGChecked) profile.SelectedFormats.Add("JPG");
            if (xmlProfile.TemplateInfo.IsNWCChecked) profile.SelectedFormats.Add("NWC");
            if (xmlProfile.TemplateInfo.IsDWFChecked) profile.SelectedFormats.Add("DWF");

            WriteDebugLog($"Converted profile with {profile.SelectedFormats.Count} formats");
            return profile;
        }

        private static void WriteDebugLog(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string fullMessage = $"[XMLProfileManager] {timestamp} - {message}";
                
                System.Diagnostics.Debug.WriteLine(fullMessage);
                Console.WriteLine(fullMessage);
                
                // Output for DebugView
                OutputDebugStringA(fullMessage + "\r\n");
            }
            catch (Exception)
            {
                // Ignore logging errors
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        private static extern void OutputDebugStringA(string lpOutputString);
    }
}