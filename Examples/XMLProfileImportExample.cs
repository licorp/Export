using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using ProSheetsAddin.Managers;
using ProSheetsAddin.Models;

namespace ProSheetsAddin.Examples
{
    /// <summary>
    /// Example class demonstrating how to use XML Profile Import functionality
    /// </summary>
    public class XMLProfileImportExample
    {
        private Document _document;
        
        public XMLProfileImportExample(Document document)
        {
            _document = document;
        }
        
        /// <summary>
        /// Example 1: Load and display XML profile information
        /// </summary>
        public void Example1_LoadAndDisplayProfile()
        {
            string xmlPath = @"D:\OneDrive\Desktop\profile diroot _crr\PROJECTWISE EXPORTS.xml";
            
            try
            {
                // Load XML profile
                var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);
                
                if (xmlProfile != null)
                {
                    Console.WriteLine("=== XML PROFILE LOADED ===");
                    Console.WriteLine($"Profile Name: {xmlProfile.Name}");
                    Console.WriteLine($"Is Current: {xmlProfile.IsCurrent}");
                    Console.WriteLine($"File Path: {xmlProfile.FilePath}");
                    Console.WriteLine();
                    
                    var template = xmlProfile.TemplateInfo;
                    
                    Console.WriteLine("=== PDF SETTINGS ===");
                    Console.WriteLine($"Vector Processing: {template.IsVectorProcessing}");
                    Console.WriteLine($"Raster Quality: {template.RasterQuality}");
                    Console.WriteLine($"Color Mode: {template.Color}");
                    Console.WriteLine($"Is Center: {template.IsCenter}");
                    Console.WriteLine($"Fit To Page: {template.IsFitToPage}");
                    Console.WriteLine($"Margin Type: {template.SelectedMarginType}");
                    Console.WriteLine();
                    
                    Console.WriteLine("=== VIEW OPTIONS ===");
                    Console.WriteLine($"Hide Planes: {template.HidePlanes}");
                    Console.WriteLine($"Hide Scope Boxes: {template.HideScopeBox}");
                    Console.WriteLine($"Hide Unreferenced Tags: {template.HideUnreferencedTags}");
                    Console.WriteLine($"Hide Crop Boundaries: {template.HideCropBoundaries}");
                    Console.WriteLine($"Mask Coincident Lines: {template.MaskCoincidentLines}");
                    Console.WriteLine();
                    
                    if (template.NWC != null)
                    {
                        Console.WriteLine("=== NWC SETTINGS ===");
                        Console.WriteLine($"Convert Element Properties: {template.NWC.ConvertElementProperties}");
                        Console.WriteLine($"Coordinates: {template.NWC.Coordinates}");
                        Console.WriteLine($"Divide Into Levels: {template.NWC.DivideFileIntoLevels}");
                        Console.WriteLine($"Export Element IDs: {template.NWC.ExportElementIds}");
                        Console.WriteLine($"Faceting Factor: {template.NWC.FacetingFactor}");
                        Console.WriteLine();
                    }
                    
                    if (template.IFC != null)
                    {
                        Console.WriteLine("=== IFC SETTINGS ===");
                        Console.WriteLine($"File Version: {template.IFC.FileVersion}");
                        Console.WriteLine($"Space Boundaries: {template.IFC.SpaceBoundaries}");
                        Console.WriteLine($"Site Placement: {template.IFC.SitePlacement}");
                        Console.WriteLine($"Export Common Property Sets: {template.IFC.ExportIFCCommonPropertySets}");
                        Console.WriteLine($"Tessellation Level: {template.IFC.TessellationLevelOfDetail}");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Example 2: Apply XML profile settings to UI
        /// </summary>
        public void Example2_ApplyProfileToUI()
        {
            string xmlPath = @"D:\OneDrive\Desktop\profile diroot _crr\PROJECTWISE EXPORTS.xml";
            
            try
            {
                var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);
                
                if (xmlProfile != null)
                {
                    Console.WriteLine("=== APPLYING PROFILE TO UI ===");
                    
                    // Apply all settings to UI using callback
                    XMLProfileManager.ApplyXMLProfileToUI(xmlProfile, (propertyName, value) =>
                    {
                        Console.WriteLine($"Setting UI Property: {propertyName} = {value}");
                        
                        // In real application, you would set actual UI controls here:
                        // Example:
                        // switch (propertyName)
                        // {
                        //     case "IsVectorProcessing":
                        //         VectorProcessingCheckBox.IsChecked = (bool)value;
                        //         break;
                        //     case "RasterQuality":
                        //         RasterQualityComboBox.SelectedItem = value.ToString();
                        //         break;
                        //     // ... etc
                        // }
                    });
                    
                    Console.WriteLine("=== PROFILE APPLIED SUCCESSFULLY ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 3: Get format-specific settings
        /// </summary>
        public void Example3_GetFormatSettings()
        {
            string xmlPath = @"D:\OneDrive\Desktop\profile diroot _crr\PROJECTWISE EXPORTS.xml";
            
            try
            {
                var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);
                
                if (xmlProfile != null)
                {
                    Console.WriteLine("=== FORMAT-SPECIFIC SETTINGS ===");
                    Console.WriteLine();
                    
                    // Get PDF settings
                    var pdfSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "PDF");
                    Console.WriteLine("PDF Settings:");
                    foreach (var kvp in pdfSettings)
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    Console.WriteLine();
                    
                    // Get NWC settings
                    var nwcSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "NWC");
                    Console.WriteLine("NWC Settings:");
                    foreach (var kvp in nwcSettings)
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    Console.WriteLine();
                    
                    // Get IFC settings
                    var ifcSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "IFC");
                    Console.WriteLine("IFC Settings:");
                    foreach (var kvp in ifcSettings)
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 4: Generate custom file names for sheets
        /// </summary>
        public void Example4_GenerateCustomFileNames()
        {
            string xmlPath = @"D:\OneDrive\Desktop\profile diroot _crr\PROJECTWISE EXPORTS.xml";
            
            try
            {
                var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);
                
                if (xmlProfile != null)
                {
                    // Get all sheets from document
                    var sheets = GetAllSheets();
                    
                    if (sheets.Any())
                    {
                        Console.WriteLine("=== CUSTOM FILE NAMES ===");
                        Console.WriteLine($"Total Sheets: {sheets.Count}");
                        Console.WriteLine();
                        
                        // Generate custom file names
                        var customNames = XMLProfileManager.GenerateCustomFileNames(xmlProfile, sheets);
                        
                        Console.WriteLine("Generated File Names:");
                        foreach (var nameInfo in customNames.Take(10)) // Show first 10
                        {
                            Console.WriteLine($"  {nameInfo.SheetNumber} | {nameInfo.SheetName}");
                            Console.WriteLine($"    → Custom Name: {nameInfo.CustomFileName}");
                            Console.WriteLine($"    → Size: {nameInfo.Size} | Revision: {nameInfo.Revision}");
                            Console.WriteLine();
                        }
                        
                        if (customNames.Count > 10)
                        {
                            Console.WriteLine($"  ... and {customNames.Count - 10} more sheets");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No sheets found in document");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 5: Convert XML to standard profile and save
        /// </summary>
        public void Example5_ConvertAndSaveProfile()
        {
            string xmlPath = @"D:\OneDrive\Desktop\profile diroot _crr\PROJECTWISE EXPORTS.xml";
            
            try
            {
                var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);
                
                if (xmlProfile != null)
                {
                    Console.WriteLine("=== CONVERTING XML TO STANDARD PROFILE ===");
                    
                    // Convert to standard profile
                    var standardProfile = XMLProfileManager.ConvertXMLToProfile(xmlProfile);
                    
                    Console.WriteLine($"Profile Name: {standardProfile.ProfileName}");
                    Console.WriteLine($"Output Folder: {standardProfile.OutputFolder}");
                    Console.WriteLine($"Create Separate Folders: {standardProfile.CreateSeparateFolders}");
                    Console.WriteLine($"Paper Size: {standardProfile.PaperSize}");
                    Console.WriteLine($"Selected Formats: {string.Join(", ", standardProfile.SelectedFormats)}");
                    Console.WriteLine();
                    
                    // Save to profile manager
                    var profileManager = new ProSheetsProfileManager();
                    profileManager.LoadProSheetsProfile(xmlPath);
                    
                    Console.WriteLine("Profile loaded into ProfileManager");
                    Console.WriteLine($"Total profiles: {profileManager.Profiles.Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 6: Full workflow - Import, Apply, Export
        /// </summary>
        public void Example6_FullWorkflow()
        {
            string xmlPath = @"D:\OneDrive\Desktop\profile diroot _crr\PROJECTWISE EXPORTS.xml";
            
            try
            {
                Console.WriteLine("=== FULL WORKFLOW EXAMPLE ===");
                Console.WriteLine();
                
                // Step 1: Load XML profile
                Console.WriteLine("Step 1: Loading XML profile...");
                var xmlProfile = XMLProfileManager.LoadProfileFromXML(xmlPath);
                Console.WriteLine($"✓ Loaded: {xmlProfile.Name}");
                Console.WriteLine();
                
                // Step 2: Apply settings to UI (simulated)
                Console.WriteLine("Step 2: Applying settings to UI...");
                int settingsApplied = 0;
                XMLProfileManager.ApplyXMLProfileToUI(xmlProfile, (name, value) =>
                {
                    settingsApplied++;
                    // In real app: Set actual UI controls here
                });
                Console.WriteLine($"✓ Applied {settingsApplied} settings");
                Console.WriteLine();
                
                // Step 3: Get format-specific settings
                Console.WriteLine("Step 3: Getting format-specific settings...");
                var pdfSettings = XMLProfileManager.GetFormatSettings(xmlProfile, "PDF");
                Console.WriteLine($"✓ PDF Settings: {pdfSettings.Count} options");
                Console.WriteLine($"  - Vector Processing: {pdfSettings["VectorProcessing"]}");
                Console.WriteLine($"  - Raster Quality: {pdfSettings["RasterQuality"]}");
                Console.WriteLine();
                
                // Step 4: Generate custom file names
                Console.WriteLine("Step 4: Generating custom file names...");
                var sheets = GetAllSheets();
                var customNames = XMLProfileManager.GenerateCustomFileNames(xmlProfile, sheets);
                Console.WriteLine($"✓ Generated {customNames.Count} custom file names");
                if (customNames.Any())
                {
                    var sample = customNames.First();
                    Console.WriteLine($"  Sample: {sample.SheetNumber} → {sample.CustomFileName}");
                }
                Console.WriteLine();
                
                // Step 5: Convert to standard profile
                Console.WriteLine("Step 5: Converting to standard profile...");
                var standardProfile = XMLProfileManager.ConvertXMLToProfile(xmlProfile);
                Console.WriteLine($"✓ Converted profile: {standardProfile.ProfileName}");
                Console.WriteLine($"  Formats: {string.Join(", ", standardProfile.SelectedFormats)}");
                Console.WriteLine();
                
                Console.WriteLine("=== WORKFLOW COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Helper method to get all sheets from document
        /// </summary>
        private List<ViewSheet> GetAllSheets()
        {
            var sheets = new FilteredElementCollector(_document)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsTemplate)
                .OrderBy(s => s.SheetNumber)
                .ToList();
                
            return sheets;
        }
        
        /// <summary>
        /// Run all examples
        /// </summary>
        public void RunAllExamples()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║       XML PROFILE IMPORT - EXAMPLES DEMONSTRATION              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            
            Console.WriteLine("Example 1: Load and Display Profile");
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Example1_LoadAndDisplayProfile();
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("Example 2: Apply Profile to UI");
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Example2_ApplyProfileToUI();
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("Example 3: Get Format Settings");
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Example3_GetFormatSettings();
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("Example 4: Generate Custom File Names");
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Example4_GenerateCustomFileNames();
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("Example 5: Convert and Save Profile");
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Example5_ConvertAndSaveProfile();
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("Example 6: Full Workflow");
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Example6_FullWorkflow();
            Console.WriteLine();
            
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  ALL EXAMPLES COMPLETED                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }
    }
}
