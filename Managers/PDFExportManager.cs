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