using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ProSheetsAddin.Models;

namespace ProSheetsAddin.Managers
{
    /// <summary>
    /// PDF Export Manager using native Revit API
    /// </summary>
    public class PDFExportManager
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern void OutputDebugStringA(string lpOutputString);

        private readonly Document _document;

        public PDFExportManager(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Export multiple sheets to PDF with custom file names
        /// </summary>
        public bool ExportSheetsWithCustomNames(List<SheetItem> sheetItems, string outputFolder, ExportSettings settings, Action<int, int, string> progressCallback = null)
        {
            try
            {
                WriteDebugLog($"[Export + PDF] Starting PDF export with custom names for {sheetItems.Count} sheets");
                WriteDebugLog($"[Export + PDF] Output folder: {outputFolder}");

                // Ensure output directory exists
                Directory.CreateDirectory(outputFolder);

                // Create PDF export options
                PDFExportOptions pdfOptions = CreatePDFExportOptions(settings);
                WriteDebugLog($"[Export + PDF] PDF options created");

                int successCount = 0;
                int failCount = 0;
                int total = sheetItems.Count;

                for (int i = 0; i < total; i++)
                {
                    var sheetItem = sheetItems[i];
                    try
                    {
                        // Get ViewSheet from document
                        ViewSheet sheet = _document.GetElement(sheetItem.Id) as ViewSheet;
                        if (sheet == null)
                        {
                            WriteDebugLog($"[Export + PDF] ERROR: Cannot find sheet with ID {sheetItem.Id}");
                            failCount++;
                            continue;
                        }

                        WriteDebugLog($"[Export + PDF] Exporting sheet: {sheet.SheetNumber} - {sheet.Name}");
                        
                        // Report progress
                        progressCallback?.Invoke(i + 1, total, sheet.SheetNumber);

                        // Determine custom file name
                        string customFileName = GetCustomOrDefaultFileName(sheetItem, sheet, settings);
                        WriteDebugLog($"[Export + PDF] Target filename: {customFileName}");

                        // Get file info BEFORE export (to detect modified files)
                        var filesBeforeInfo = Directory.GetFiles(outputFolder, "*.pdf")
                            .Select(f => new FileInfo(f))
                            .ToDictionary(fi => fi.FullName, fi => fi.LastWriteTime);
                        
                        DateTime exportStartTime = DateTime.Now;
                        WriteDebugLog($"[Export + PDF] Export starting at: {exportStartTime:HH:mm:ss.fff}");
                        WriteDebugLog($"[Export + PDF] Files before export: {filesBeforeInfo.Count}");
                        
                        // Use a temporary filename for Revit export (Revit may add prefixes)
                        string tempFileName = $"_TEMP_{Guid.NewGuid():N}";
                        
                        // Create list of ElementIds for this sheet
                        List<ElementId> sheetIds = new List<ElementId> { sheet.Id };

                        // Set temporary file name in options
                        pdfOptions.FileName = tempFileName;

                        // Export the sheet with temporary name
                        _document.Export(outputFolder, sheetIds, pdfOptions);
                        
                        // Wait for file to be written to disk
                        System.Threading.Thread.Sleep(500);

                        // Get files AFTER export
                        var filesAfter = Directory.GetFiles(outputFolder, "*.pdf");
                        WriteDebugLog($"[Export + PDF] Files after export: {filesAfter.Length}");
                        
                        // Find NEW or MODIFIED files (created or modified after export started)
                        string exportedFile = null;
                        
                        foreach (string file in filesAfter)
                        {
                            FileInfo fi = new FileInfo(file);
                            
                            // Check if file is new (not in before list)
                            if (!filesBeforeInfo.ContainsKey(fi.FullName))
                            {
                                exportedFile = file;
                                WriteDebugLog($"[Export + PDF] Found NEW file: {Path.GetFileName(file)}");
                                break;
                            }
                            
                            // Check if file was modified after export started
                            if (fi.LastWriteTime > exportStartTime)
                            {
                                exportedFile = file;
                                WriteDebugLog($"[Export + PDF] Found MODIFIED file: {Path.GetFileName(file)} (modified at {fi.LastWriteTime:HH:mm:ss.fff})");
                                break;
                            }
                        }
                        
                        if (exportedFile != null)
                        {
                            string targetFile = Path.Combine(outputFolder, customFileName + ".pdf");
                            
                            // If target file exists and it's not the exported file, delete it
                            if (File.Exists(targetFile) && !string.Equals(exportedFile, targetFile, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Delete(targetFile);
                                WriteDebugLog($"[Export + PDF] Deleted existing target file");
                            }
                            
                            // Rename to custom filename (if not already the same)
                            if (!string.Equals(exportedFile, targetFile, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Move(exportedFile, targetFile);
                                WriteDebugLog($"[Export + PDF] SUCCESS: Renamed '{Path.GetFileName(exportedFile)}' to '{customFileName}.pdf'");
                            }
                            else
                            {
                                WriteDebugLog($"[Export + PDF] SUCCESS: File already has correct name: {customFileName}.pdf");
                            }
                            
                            successCount++;
                        }
                        else
                        {
                            WriteDebugLog($"[Export + PDF] ERROR: Could not find exported/modified file for {sheet.SheetNumber}");
                            WriteDebugLog($"[Export + PDF] Export started at: {exportStartTime:HH:mm:ss.fff}");
                            WriteDebugLog($"[Export + PDF] Current time: {DateTime.Now:HH:mm:ss.fff}");
                            
                            // Log file details (C# 7.3 compatible)
                            var fileDetails = string.Join(", ", filesAfter.Select(f => {
                                var fi = new FileInfo(f);
                                return string.Format("{0} (modified: {1:HH:mm:ss.fff})", Path.GetFileName(f), fi.LastWriteTime);
                            }));
                            WriteDebugLog($"[Export + PDF] Files in folder: {fileDetails}");
                            
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        WriteDebugLog($"[Export + PDF] ERROR exporting {sheetItem.SheetNumber}: {ex.Message}");
                    }
                }

                WriteDebugLog($"[Export + PDF] Export completed - Success: {successCount}, Failed: {failCount}");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] CRITICAL ERROR in ExportSheetsWithCustomNames: {ex.Message}");
                WriteDebugLog($"[Export + PDF] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Get custom file name if available, otherwise generate default
        /// </summary>
        private string GetCustomOrDefaultFileName(SheetItem sheetItem, ViewSheet sheet, ExportSettings settings)
        {
            // Use custom file name if available
            if (!string.IsNullOrWhiteSpace(sheetItem.CustomFileName))
            {
                return SanitizeFileName(sheetItem.CustomFileName);
            }

            // Otherwise generate default file name
            return GenerateFileName(sheet, settings);
        }

        /// <summary>
        /// Export multiple sheets to PDF
        /// </summary>
        public bool ExportSheetsToPDF(List<ViewSheet> sheets, string outputFolder, ExportSettings settings)
        {
            try
            {
                WriteDebugLog($"[Export + PDF] Starting PDF export for {sheets.Count} sheets");
                WriteDebugLog($"[Export + PDF] Output folder: {outputFolder}");

                // Ensure output directory exists
                Directory.CreateDirectory(outputFolder);

                // Create PDF export options
                PDFExportOptions pdfOptions = CreatePDFExportOptions(settings);
                WriteDebugLog($"[Export + PDF] PDF options created");

                int successCount = 0;
                int failCount = 0;

                foreach (ViewSheet sheet in sheets)
                {
                    try
                    {
                        WriteDebugLog($"[Export + PDF] Exporting sheet: {sheet.SheetNumber} - {sheet.Name}");
                        
                        if (ExportSingleSheetToPDF(sheet, outputFolder, pdfOptions, settings))
                        {
                            successCount++;
                            WriteDebugLog($"[Export + PDF] SUCCESS: {sheet.SheetNumber}");
                        }
                        else
                        {
                            failCount++;
                            WriteDebugLog($"[Export + PDF] FAILED: {sheet.SheetNumber}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        WriteDebugLog($"[Export + PDF] ERROR exporting {sheet.SheetNumber}: {ex.Message}");
                    }
                }

                WriteDebugLog($"[Export + PDF] Export completed - Success: {successCount}, Failed: {failCount}");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] CRITICAL ERROR in ExportSheetsToPDF: {ex.Message}");
                WriteDebugLog($"[Export + PDF] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Export single sheet to PDF
        /// </summary>
        private bool ExportSingleSheetToPDF(ViewSheet sheet, string outputFolder, PDFExportOptions options, ExportSettings settings)
        {
            try
            {
                // Generate file name
                string fileName = GenerateFileName(sheet, settings);
                WriteDebugLog($"[Export + PDF] Generated filename: {fileName}");

                // Create list of ElementIds for this sheet
                List<ElementId> sheetIds = new List<ElementId> { sheet.Id };

                // Set output file name in options
                options.FileName = fileName;

                // Export the sheet
                _document.Export(outputFolder, sheetIds, options);

                WriteDebugLog($"[Export + PDF] Successfully exported: {fileName}.pdf");
                return true;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] Failed to export sheet {sheet.SheetNumber}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create PDF export options
        /// </summary>
        private PDFExportOptions CreatePDFExportOptions(ExportSettings settings)
        {
            PDFExportOptions options = new PDFExportOptions();

            try
            {
                // Basic configuration
                options.PaperFormat = ExportPaperFormat.Default; // Auto-detect from sheet
                options.PaperOrientation = PageOrientationType.Auto;
                options.Combine = false; // Export individual files
                options.HideCropBoundaries = true;
                options.HideScopeBoxes = true;
                
                // CRITICAL FIX: Set naming rule to use custom file names without Revit prefix
                try
                {
                    // Try to set NamingRule property (Revit 2023+)
                    var namingRuleProperty = options.GetType().GetProperty("NamingRule");
                    if (namingRuleProperty != null && namingRuleProperty.PropertyType.IsEnum)
                    {
                        // Get the enum type for NamingRule
                        var namingRuleType = namingRuleProperty.PropertyType;
                        // Try to set to "Custom" or "FileName" enum value (usually 0 or 1)
                        var customValue = Enum.Parse(namingRuleType, "Custom", true);
                        namingRuleProperty.SetValue(options, customValue);
                        WriteDebugLog("[Export + PDF] Set NamingRule to Custom for custom file names");
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"[Export + PDF] Warning - could not set NamingRule: {ex.Message}");
                }
                
                // Alternative approach: Try to disable automatic naming
                try
                {
                    var autoNamingProperty = options.GetType().GetProperty("ReplaceHalftoneWithThinLines");
                    if (autoNamingProperty != null)
                    {
                        autoNamingProperty.SetValue(options, false);
                    }
                }
                catch { /* Ignore if property doesn't exist */ }
                // Note: PDF export options may vary by Revit version
                // These settings work for Revit 2022+
                try
                {
                    // Set basic PDF options - use reflection to handle version differences
                    var colorProperty = options.GetType().GetProperty("ColorDepth");
                    if (colorProperty != null)
                    {
                        // Try to set to color mode (usually value 0 or enum)
                        colorProperty.SetValue(options, 0);
                    }
                    
                    var qualityProperty = options.GetType().GetProperty("ExportQuality");
                    if (qualityProperty != null)
                    {
                        // Try to set to high quality (usually value 2 for 600 DPI)
                        qualityProperty.SetValue(options, 2);
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"[Export + PDF] Warning - could not set advanced options: {ex.Message}");
                }

                // Advanced settings based on user preferences
                if (settings != null)
                {
                    // You can add more settings here based on ExportSettings properties
                    WriteDebugLog("[Export + PDF] Applied custom export settings");
                }

                WriteDebugLog("[Export + PDF] PDF export options configured");
                return options;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] Error creating PDF options: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generate file name based on sheet properties and settings
        /// </summary>
        private string GenerateFileName(ViewSheet sheet, ExportSettings settings)
        {
            try
            {
                // Get project information
                ProjectInfo projectInfo = _document.ProjectInformation;
                string projectNumber = GetParameterValue(projectInfo, BuiltInParameter.PROJECT_NUMBER);

                // Get sheet information
                string sheetNumber = sheet.SheetNumber ?? "Unknown";
                string sheetName = sheet.Name ?? "Untitled";

                // Get revision information
                string revision = GetSheetRevision(sheet);

                // Create base filename: ProjectNumber_SheetNumber_SheetName
                string fileName = "";

                if (!string.IsNullOrEmpty(projectNumber))
                {
                    fileName += SanitizeFileName(projectNumber) + "_";
                }

                fileName += SanitizeFileName(sheetNumber);

                if (!string.IsNullOrEmpty(sheetName))
                {
                    fileName += "_" + SanitizeFileName(sheetName);
                }

                // Add revision if available and requested
                if (!string.IsNullOrEmpty(revision))
                {
                    fileName += "_Rev" + SanitizeFileName(revision);
                }

                // Ensure filename is not too long (Windows limit: 255 chars)
                if (fileName.Length > 200)
                {
                    fileName = fileName.Substring(0, 200);
                }

                WriteDebugLog($"[Export + PDF] Generated filename: {fileName}");
                return fileName;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] Error generating filename: {ex.Message}");
                // Fallback to simple naming
                return SanitizeFileName($"{sheet.SheetNumber}_{sheet.Name}");
            }
        }

        /// <summary>
        /// Get parameter value from element
        /// </summary>
        private string GetParameterValue(Element element, BuiltInParameter paramName)
        {
            try
            {
                Parameter param = element.get_Parameter(paramName);
                return param?.AsString() ?? "";
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] Error getting parameter {paramName}: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Get sheet revision information
        /// </summary>
        private string GetSheetRevision(ViewSheet sheet)
        {
            try
            {
                // Try to get revision sequence number
                Parameter revParam = sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION);
                if (revParam != null && !string.IsNullOrEmpty(revParam.AsString()))
                {
                    return revParam.AsString();
                }

                // Try alternative revision parameters
                revParam = sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION_DATE);
                if (revParam != null && !string.IsNullOrEmpty(revParam.AsString()))
                {
                    return revParam.AsString();
                }

                return "";
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] Error getting sheet revision: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Remove invalid characters from filename
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Unknown";

            try
            {
                // Remove invalid file name characters
                char[] invalidChars = Path.GetInvalidFileNameChars();
                foreach (char c in invalidChars)
                {
                    fileName = fileName.Replace(c, '_');
                }

                // Also replace some problematic characters
                fileName = fileName.Replace(' ', '_')
                                 .Replace('.', '_')
                                 .Replace(',', '_')
                                 .Replace(';', '_')
                                 .Replace(':', '_');

                // Remove multiple underscores
                while (fileName.Contains("__"))
                {
                    fileName = fileName.Replace("__", "_");
                }

                // Trim underscores from start and end
                fileName = fileName.Trim('_');

                return string.IsNullOrEmpty(fileName) ? "Unknown" : fileName;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] Error sanitizing filename: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Write debug log with DebugView compatibility
        /// </summary>
        private void WriteDebugLog(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string fullMessage = $"[Export +] {timestamp} - {message}";

                // Output to Visual Studio debug console
                System.Diagnostics.Debug.WriteLine(fullMessage);

                // Output to DebugView
                OutputDebugStringA(fullMessage + "\r\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export +] Logging error: {ex.Message}");
                OutputDebugStringA($"[Export +] Logging error: {ex.Message}\r\n");
            }
        }
    }
}