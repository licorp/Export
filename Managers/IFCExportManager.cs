using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ProSheetsAddin.Models;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.ExtensibleStorage;
using Newtonsoft.Json;

namespace ProSheetsAddin.Managers
{
    /// <summary>
    /// IFC Export Manager - Compatible with Revit API
    /// </summary>
    public class IFCExportManager
    {
        private Document _document;

        public IFCExportManager(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>
        /// Export sheets/views to IFC format
        /// </summary>
        public bool ExportToIFC(List<ViewSheet> sheets, IFCExportSettings settings, string outputPath, Action<string> logCallback = null)
        {
            try
            {
                logCallback?.Invoke($"Starting IFC export with {sheets.Count} sheets");
                
                // Create IFC export options from settings
                var ifcOptions = CreateIFCExportOptions(settings, logCallback);
                
                // Create output directory if needed
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                    logCallback?.Invoke($"Created output directory: {outputPath}");
                }

                using (Transaction trans = new Transaction(_document, "Export IFC"))
                {
                    trans.Start();

                    try
                    {
                        foreach (var sheet in sheets)
                        {
                            string fileName = SanitizeFileName(sheet.SheetNumber + "_" + sheet.Name);
                            
                            logCallback?.Invoke($"Exporting sheet: {sheet.SheetNumber} - {sheet.Name}");

                            // Export using Document.Export method for IFC
                            string fullPath = Path.Combine(outputPath, fileName + ".ifc");
                            
                            // Use IFC export with options
                            using (Transaction t = new Transaction(_document, "IFC Export"))
                            {
                                t.Start();
                                
                                // Note: Revit IFC export API is complex
                                // We'll use a simplified approach
                                bool success = ExportSingleSheet(sheet, fullPath, ifcOptions, logCallback);
                                
                                if (success)
                                {
                                    logCallback?.Invoke($"✓ Exported: {fileName}.ifc");
                                }
                                else
                                {
                                    logCallback?.Invoke($"✗ Failed to export: {fileName}");
                                }
                                
                                t.RollBack(); // Don't commit changes
                            }
                        }

                        trans.RollBack(); // Don't commit outer transaction
                        logCallback?.Invoke($"IFC export completed: {sheets.Count} sheets processed");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        logCallback?.Invoke($"ERROR during export: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"IFC Export failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"IFC Export Error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Export single sheet
        /// </summary>
        private bool ExportSingleSheet(ViewSheet sheet, string filePath, IFCExportOptions options, Action<string> logCallback)
        {
            try
            {
                // Create a set of views to export
                var viewIds = new List<ElementId> { sheet.Id };
                
                // Export using the IFC exporter
                // Note: This is a simplified implementation
                // Full implementation would use IFCExportConfigurationsMap
                
                _document.Export(Path.GetDirectoryName(filePath), 
                                Path.GetFileNameWithoutExtension(filePath), 
                                options);
                
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Error exporting sheet: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create IFCExportOptions from settings (using only available API properties)
        /// </summary>
        private IFCExportOptions CreateIFCExportOptions(IFCExportSettings settings, Action<string> logCallback = null)
        {
            var options = new IFCExportOptions();

            try
            {
                // IFC Version (CORE - Always available)
                options.FileVersion = ConvertIFCVersion(settings.IFCVersion);
                logCallback?.Invoke($"IFC Version: {settings.IFCVersion}");

                // Space Boundaries (CORE - Always available)
                options.SpaceBoundaryLevel = ConvertSpaceBoundaries(settings.SpaceBoundaries);
                logCallback?.Invoke($"Space Boundaries: {settings.SpaceBoundaries}");

                // Property Sets - ExportBaseQuantities (Always available)
                options.ExportBaseQuantities = settings.ExportBaseQuantities;
                logCallback?.Invoke($"Export Base Quantities: {settings.ExportBaseQuantities}");
                
                // Note: Many properties are not available in older Revit API versions
                // They are wrapped in try-catch to maintain compatibility
                
                // Wall and Column Splitting (if available)
                try { options.WallAndColumnSplitting = settings.SplitWallsByLevel; } catch { }

                logCallback?.Invoke("IFC export options configured successfully (using available API properties)");
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Warning: Some IFC options not set: {ex.Message}");
            }

            return options;
        }

        /// <summary>
        /// Convert IFC Version string to enum
        /// </summary>
        private Autodesk.Revit.DB.IFCVersion ConvertIFCVersion(string version)
        {
            switch (version)
            {
                case "IFC 2x3 Coordination View 2.0":
                case "IFC 2x3 Coordination View":
                    return Autodesk.Revit.DB.IFCVersion.IFC2x3CV2;
                    
                case "IFC 4 Reference View":
                    return Autodesk.Revit.DB.IFCVersion.IFC4RV;
                    
                case "IFC 4 Design Transfer View":
                    return Autodesk.Revit.DB.IFCVersion.IFC4DTV;
                    
                case "IFC 2x2":
                    return Autodesk.Revit.DB.IFCVersion.IFC2x2;
                    
                case "IFC 4":
                    return Autodesk.Revit.DB.IFCVersion.IFC4;
                    
                default:
                    return Autodesk.Revit.DB.IFCVersion.IFC2x3CV2; // Default
            }
        }

        /// <summary>
        /// Convert Space Boundaries string to level
        /// </summary>
        private int ConvertSpaceBoundaries(string spaceBoundaries)
        {
            switch (spaceBoundaries)
            {
                case "None":
                    return 0;
                case "1st Level":
                    return 1;
                case "2nd Level":
                    return 2;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Sanitize file name to remove invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            
            // Also replace some additional problematic characters
            fileName = fileName.Replace(':', '-');
            fileName = fileName.Replace('/', '-');
            fileName = fileName.Replace('\\', '-');
            
            return fileName;
        }

        /// <summary>
        /// Get all 3D views from document (for View-based export)
        /// </summary>
        public List<View3D> Get3DViews()
        {
            var views3D = new FilteredElementCollector(_document)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(v => !v.IsTemplate)
                .ToList();

            return views3D;
        }

        /// <summary>
        /// Export 3D views to IFC
        /// </summary>
        public bool Export3DViewsToIFC(List<View3D> views, IFCExportSettings settings, string outputPath, Action<string> logCallback = null)
        {
            try
            {
                logCallback?.Invoke($"Starting IFC export with {views.Count} 3D views");
                
                var ifcOptions = CreateIFCExportOptions(settings, logCallback);
                
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                foreach (var view in views)
                {
                    string fileName = SanitizeFileName(view.Name);
                    string fullPath = Path.Combine(outputPath, fileName + ".ifc");
                    
                    logCallback?.Invoke($"Exporting 3D view: {view.Name}");

                    try
                    {
                        _document.Export(Path.GetDirectoryName(fullPath), 
                                        Path.GetFileNameWithoutExtension(fullPath), 
                                        ifcOptions);
                        
                        logCallback?.Invoke($"✓ Exported: {fileName}.ifc");
                    }
                    catch (Exception ex)
                    {
                        logCallback?.Invoke($"✗ Failed to export {view.Name}: {ex.Message}");
                    }
                }

                logCallback?.Invoke($"IFC export completed: {views.Count} 3D views processed");
                return true;
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"IFC Export failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get list of IFC Export Configuration names from Revit
        /// This includes both built-in setups and user-created custom setups
        /// READS FROM: Document's ExtensibleStorage (same as Autodesk IFC Exporter)
        /// </summary>
        public static List<string> GetAvailableIFCSetups(Document document)
        {
            var setupNames = new List<string>();

            // DEBUG: Write to file for verification
            string debugLogPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                "ProSheets_IFC_Debug.txt");
            System.IO.File.AppendAllText(debugLogPath, $"\n\n========== {DateTime.Now} ==========\n");
            System.IO.File.AppendAllText(debugLogPath, "GetAvailableIFCSetups() CALLED - Using ExtensibleStorage\n");

            try
            {
                // Always add In-Session Setup first (default Revit behavior)
                setupNames.Add("<In-Session Setup>");
                System.IO.File.AppendAllText(debugLogPath, "Added: <In-Session Setup>\n");

                // METHOD: Read from Document's ExtensibleStorage (like Autodesk IFC Exporter does)
                // Schema GUID used by Autodesk IFC Exporter: DCB88B13-594F-44F6-8F5D-AE9477305AC3
                System.IO.File.AppendAllText(debugLogPath, "========== READING FROM EXTENSIBLE STORAGE ==========\n");
                
                try
                {
                    // Try to find IFC Configuration schema in document
                    Guid jsonSchemaId = new Guid("C2A3E6FE-CE51-4F35-8FF1-20C34567B687"); // Latest JSON-based schema
                    Guid oldSchemaId = new Guid("DCB88B13-594F-44F6-8F5D-AE9477305AC3");  // Older MapField schema
                    
                    Schema jsonSchema = Schema.Lookup(jsonSchemaId);
                    Schema oldSchema = Schema.Lookup(oldSchemaId);
                    
                    System.IO.File.AppendAllText(debugLogPath, $"JSON Schema found: {jsonSchema != null}\n");
                    System.IO.File.AppendAllText(debugLogPath, $"Old Schema found: {oldSchema != null}\n");
                    
                    int customCount = 0;
                    
                    // Try JSON schema first (Revit 2020+)
                    if (jsonSchema != null)
                    {
                        System.IO.File.AppendAllText(debugLogPath, "Using JSON Schema...\n");
                        
                        // Get all DataStorage elements with this schema
                        FilteredElementCollector collector = new FilteredElementCollector(document);
                        var dataStorages = collector.OfClass(typeof(DataStorage)).Cast<DataStorage>();
                        
                        foreach (DataStorage storage in dataStorages)
                        {
                            Entity entity = storage.GetEntity(jsonSchema);
                            if (entity != null && entity.IsValid())
                            {
                                try
                                {
                                    // Get configuration data from JSON field
                                    string configData = entity.Get<string>("MapField");
                                    if (!string.IsNullOrEmpty(configData))
                                    {
                                        // Parse JSON to get configuration name
                                        var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(configData);
                                        if (configDict != null && configDict.ContainsKey("Name"))
                                        {
                                            string configName = configDict["Name"].ToString();
                                            if (!setupNames.Contains(configName))
                                            {
                                                setupNames.Add(configName);
                                                customCount++;
                                                System.IO.File.AppendAllText(debugLogPath, $"  [{customCount}] {configName} (from JSON)\n");
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.IO.File.AppendAllText(debugLogPath, $"  Error parsing JSON config: {ex.Message}\n");
                                }
                            }
                        }
                    }
                    
                    // Try old schema (Revit 2019 and earlier)
                    if (oldSchema != null && customCount == 0)
                    {
                        System.IO.File.AppendAllText(debugLogPath, "Using Old MapField Schema...\n");
                        
                        FilteredElementCollector collector = new FilteredElementCollector(document);
                        var dataStorages = collector.OfClass(typeof(DataStorage)).Cast<DataStorage>();
                        
                        foreach (DataStorage storage in dataStorages)
                        {
                            Entity entity = storage.GetEntity(oldSchema);
                            if (entity != null && entity.IsValid())
                            {
                                try
                                {
                                    // Get configuration map (Dictionary)
                                    var configMap = entity.Get<IDictionary<string, string>>("MapField");
                                    if (configMap != null && configMap.ContainsKey("Name"))
                                    {
                                        string configName = configMap["Name"];
                                        if (!setupNames.Contains(configName))
                                        {
                                            setupNames.Add(configName);
                                            customCount++;
                                            System.IO.File.AppendAllText(debugLogPath, $"  [{customCount}] {configName} (from MapField)\n");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.IO.File.AppendAllText(debugLogPath, $"  Error reading MapField config: {ex.Message}\n");
                                }
                            }
                        }
                    }
                    
                    System.IO.File.AppendAllText(debugLogPath, $"✓ Found {customCount} custom configurations in document\n");
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(debugLogPath, $"✗ ERROR reading ExtensibleStorage: {ex.Message}\n");
                    System.IO.File.AppendAllText(debugLogPath, $"   Type: {ex.GetType().Name}\n");
                    System.IO.File.AppendAllText(debugLogPath, $"   Stack: {ex.StackTrace}\n");
                }

                // ALWAYS add built-in configurations (even if custom ones found)
                System.IO.File.AppendAllText(debugLogPath, "========== ADDING BUILT-IN CONFIGURATIONS ==========\n");
                
                List<string> builtInSetups = new List<string>
                {
                    "IFC 2x3 Coordination View 2.0",
                    "IFC 2x3 Coordination View",
                    "IFC 2x3 GSA Concept Design BIM 2010",
                    "IFC 2x3 Basic FM Handover View",
                    "IFC 2x2 Coordination View",
                    "IFC 2x2 Singapore BCA e-Plan Check",
                    "IFC 2x3 COBie 2.4 Design Deliverable View",
                    "IFC4 Reference View",
                    "IFC4 Design Transfer View"
                };
                
                foreach (string builtIn in builtInSetups)
                {
                    if (!setupNames.Contains(builtIn))
                    {
                        setupNames.Add(builtIn);
                    }
                }
                
                System.IO.File.AppendAllText(debugLogPath, $"✓ Added {builtInSetups.Count} built-in setups\n");
                System.IO.File.AppendAllText(debugLogPath, $"\n========== FINAL TOTAL: {setupNames.Count} setups ==========\n");
                
                // Log all final setups
                for (int i = 0; i < setupNames.Count; i++)
                {
                    System.IO.File.AppendAllText(debugLogPath, $"  [{i+1}] {setupNames[i]}\n");
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(debugLogPath, $"\n✗ OUTER ERROR: {ex.Message}\n");
                System.IO.File.AppendAllText(debugLogPath, $"   Stack: {ex.StackTrace}\n");
                
                // Ensure at least basic setups
                if (setupNames.Count == 0)
                {
                    setupNames.Add("<In-Session Setup>");
                }
            }

            return setupNames;
        }

        /// <summary>
        /// Load IFC Export Configuration from Revit by name
        /// Returns settings object populated with the configuration values
        /// </summary>
        public static IFCExportSettings LoadIFCSetupFromRevit(Document document, string setupName)
        {
            var settings = new IFCExportSettings();

            try
            {
                // If In-Session Setup, return default settings
                if (setupName == "<In-Session Setup>")
                {
                    return settings;
                }

                // Try to load configuration from Revit
                try
                {
                    var ifcExportConfigType = Type.GetType("BIM.IFC.Export.UI.IFCExportConfigurationsMap, RevitIFCUI");
                    
                    if (ifcExportConfigType != null)
                    {
                        var getMethod = ifcExportConfigType.GetMethod("GetStoredConfigurations",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        
                        if (getMethod != null)
                        {
                            var configs = getMethod.Invoke(null, new object[] { document }) as IDictionary<string, object>;
                            
                            if (configs != null && configs.ContainsKey(setupName))
                            {
                                var config = configs[setupName];
                                
                                // Use reflection to read configuration properties
                                var configType = config.GetType();
                                
                                // Try to read common properties
                                var ifcVersionProp = configType.GetProperty("IFCVersion");
                                if (ifcVersionProp != null)
                                {
                                    var versionValue = ifcVersionProp.GetValue(config);
                                    settings.IFCVersion = versionValue?.ToString() ?? "IFC 2x3 Coordination View 2.0";
                                }

                                var spaceBoundariesProp = configType.GetProperty("SpaceBoundaries");
                                if (spaceBoundariesProp != null)
                                {
                                    var spaceBoundValue = spaceBoundariesProp.GetValue(config);
                                    settings.SpaceBoundaries = spaceBoundValue?.ToString() ?? "None";
                                }

                                var exportBaseQtyProp = configType.GetProperty("ExportBaseQuantities");
                                if (exportBaseQtyProp != null)
                                {
                                    var baseQtyValue = exportBaseQtyProp.GetValue(config);
                                    settings.ExportBaseQuantities = baseQtyValue is bool ? (bool)baseQtyValue : false;
                                }

                                // Add more property mappings as needed...
                                
                                return settings;
                            }
                        }
                    }
                }
                catch
                {
                    // Failed to load from Revit API, use defaults
                }

                // If we couldn't load from Revit, create sensible defaults based on setup name
                settings = CreateDefaultSetupSettings(setupName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading IFC setup '{setupName}': {ex.Message}");
            }

            return settings;
        }

        /// <summary>
        /// Create default settings for known setup types
        /// </summary>
        private static IFCExportSettings CreateDefaultSetupSettings(string setupName)
        {
            var settings = new IFCExportSettings();

            // Remove " Setup>" suffix if present for matching
            var cleanName = setupName.Replace(" Setup>", "").Replace("Setup>", "");

            // Match based on clean name or original name
            if (cleanName.Contains("IFC 2x3 Coordination View 2.0") || setupName.Contains("IFC 2x3 Coordination View 2.0"))
            {
                settings.IFCVersion = "IFC 2x3 Coordination View 2.0";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = false;
                settings.SplitWallsByLevel = true;
            }
            else if (cleanName.Contains("IFC 2x3 GSA") || setupName.Contains("IFC 2x3 GSA"))
            {
                settings.IFCVersion = "IFC 2x3 GSA Concept Design BIM 2010";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = true;
                settings.ExportBoundingBox = true;
            }
            else if (cleanName.Contains("IFC 2x3 Basic FM") || setupName.Contains("IFC 2x3 Basic FM"))
            {
                settings.IFCVersion = "IFC 2x3 Basic FM Handover View";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = true;
                settings.SpaceBoundaries = "1st Level";
                settings.ExportRoomsIn3DViews = true;
            }
            else if (cleanName.Contains("IFC 2x3 COBie") || setupName.Contains("IFC 2x3 COBie"))
            {
                settings.IFCVersion = "IFC 2x3 COBie 2.4 Design Deliverable View";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = true;
                settings.SpaceBoundaries = "2nd Level";
                settings.ExportRoomsIn3DViews = true;
            }
            else if (cleanName.Contains("IFC4 Reference View") || setupName.Contains("IFC4 Reference View"))
            {
                settings.IFCVersion = "IFC4 Reference View";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = false;
            }
            else if (cleanName.Contains("IFC4 Design Transfer") || setupName.Contains("IFC4 Design Transfer"))
            {
                settings.IFCVersion = "IFC4 Design Transfer View";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = true;
            }
            else if (cleanName.Contains("IFC 2x3 Coordination View") || setupName.Contains("IFC 2x3 Coordination View"))
            {
                settings.IFCVersion = "IFC 2x3 Coordination View";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = false;
            }
            else if (cleanName.Contains("IFC 2x2 Coordination View") || setupName.Contains("IFC 2x2 Coordination View"))
            {
                settings.IFCVersion = "IFC 2x2 Coordination View";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = false;
            }
            else if (cleanName.Contains("IFC 2x2 Singapore") || setupName.Contains("IFC 2x2 Singapore"))
            {
                settings.IFCVersion = "IFC 2x2 Singapore BCA e-Plan Check";
                settings.FileType = "IFC";
                settings.ExportBaseQuantities = true;
            }
            else if (cleanName.Contains("Typical") || setupName.Contains("Typical"))
            {
                settings.IFCVersion = "IFC 2x3 Coordination View 2.0";
                settings.FileType = "IFC";
                settings.DetailLevel = "Medium";
            }
            else
            {
                // Default fallback for unknown/custom setups
                settings.IFCVersion = "IFC 2x3 Coordination View 2.0";
                settings.FileType = "IFC";
            }

            return settings;
        }
    }
}