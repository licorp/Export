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
        /// <summary>
        /// Export multiple sheets to PDF with custom file names
        /// Uses Document.Export() with view options applied beforehand
        /// </summary>
        /// <param name="progressCallback">Progress callback: (current, total, sheetNumber, isFileCompleted)</param>
        public bool ExportSheetsWithCustomNames(List<SheetItem> sheetItems, string outputFolder, ExportSettings settings, Action<int, int, string, bool> progressCallback = null)
        {
            try
            {
                WriteDebugLog($"[Export + PDF] Starting PDF export with custom names for {sheetItems.Count} sheets");
                WriteDebugLog($"[Export + PDF] Output folder: {outputFolder}");

                // Ensure output directory exists
                Directory.CreateDirectory(outputFolder);

                // ===================================================================
                // STEP 1: Apply view options to ALL sheets BEFORE export
                // This ensures hidden categories, crop boxes, etc. are set correctly
                // ===================================================================
                WriteDebugLog("[Export + PDF] STEP 1: Applying view options to all sheets...");
                try
                {
                    using (Transaction trans = new Transaction(_document, "Apply PDF View Options"))
                    {
                        trans.Start();
                        
                        foreach (var sheetItem in sheetItems)
                        {
                            ViewSheet sheet = _document.GetElement(sheetItem.Id) as ViewSheet;
                            if (sheet != null)
                            {
                                // Use NoTransaction version since we're already in a Transaction
                                Services.PDFOptionsApplier.ApplyViewOptionsToSheetNoTransaction(_document, sheet, settings);
                            }
                        }
                        
                        trans.Commit();
                    }
                    WriteDebugLog("[Export + PDF] ✓ View options applied to all sheets");
                }
                catch (Exception viewEx)
                {
                    WriteDebugLog($"[Export + PDF] WARNING: Could not apply view options: {viewEx.Message}");
                    // Continue export even if view options fail
                }

                // ===================================================================
                // STEP 2: Create PDF export options
                // ===================================================================
                WriteDebugLog("[Export + PDF] STEP 2: Creating PDF export options...");
                PDFExportOptions pdfOptions = CreatePDFExportOptions(settings);
                WriteDebugLog($"[Export + PDF] PDF options created");

                // ===================================================================
                // STEP 3: Export each sheet
                // ===================================================================
                WriteDebugLog("[Export + PDF] STEP 3: Exporting sheets...");
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
                        
                        // Report progress - START of export
                        progressCallback?.Invoke(i + 1, total, sheet.SheetNumber, false);

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
                            
                            // ===================================================================
                            // QUALITY VERIFICATION - Check exported PDF file
                            // ===================================================================
                            try
                            {
                                FileInfo pdfFile = new FileInfo(targetFile);
                                long fileSizeKB = pdfFile.Length / 1024;
                                WriteDebugLog($"[Export + PDF] === QUALITY CHECK ===");
                                WriteDebugLog($"[Export + PDF] File: {customFileName}.pdf");
                                WriteDebugLog($"[Export + PDF] Size: {fileSizeKB} KB");
                                
                                // Expected: Vector PDF for A3 sheet = ~100 KB - 1 MB
                                // Too small (<50 KB) = likely low quality raster
                                // Too large (>5 MB) = may contain unnecessary raster data
                                if (fileSizeKB < 50)
                                {
                                    WriteDebugLog($"[Export + PDF] ⚠ WARNING: File size very small ({fileSizeKB} KB)");
                                    WriteDebugLog($"[Export + PDF] ⚠ May indicate low quality raster export");
                                }
                                else if (fileSizeKB > 5000)
                                {
                                    WriteDebugLog($"[Export + PDF] ⚠ WARNING: File size large ({fileSizeKB} KB)");
                                    WriteDebugLog($"[Export + PDF] ⚠ May contain excessive raster data");
                                }
                                else
                                {
                                    WriteDebugLog($"[Export + PDF] ✅ File size looks good for vector PDF");
                                }
                                
                                WriteDebugLog($"[Export + PDF] TIP: Open in Adobe Acrobat:");
                                WriteDebugLog($"[Export + PDF]   - Object Inspector: Check for 'Path' and 'Text' objects (vector)");
                                WriteDebugLog($"[Export + PDF]   - Try selecting text: Should be selectable if vector");
                                WriteDebugLog($"[Export + PDF]   - Preflight → Images: Check DPI ≥ 300 for raster elements");
                            }
                            catch (Exception qcEx)
                            {
                                WriteDebugLog($"[Export + PDF] Info - Quality check failed: {qcEx.Message}");
                            }
                            
                            // Report completion - file exists on disk
                            progressCallback?.Invoke(i + 1, total, sheet.SheetNumber, true);
                            
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
                WriteDebugLog("[Export + PDF] Creating PDF export options...");
                
                // Basic configuration
                options.PaperFormat = ExportPaperFormat.Default; // Auto-detect from sheet
                options.PaperOrientation = PageOrientationType.Auto;
                
                // Combine Files setting
                options.Combine = settings.CombineFiles;
                WriteDebugLog($"[Export + PDF] ✓ Combine Files: {options.Combine}");
                
                // ===================================================================
                // CRITICAL: Apply Color & Raster Quality settings
                // These are the ONLY advanced settings PDFExportOptions supports
                // ===================================================================
                
                // 1. COLOR SETTINGS - THIS IS THE KEY FIX
                WriteDebugLog($"[Export + PDF] Setting ColorDepth: {settings.Colors}");
                if (settings.Colors == PSColors.Color)
                {
                    options.ColorDepth = ColorDepthType.Color;
                    WriteDebugLog("[Export + PDF] ✓ ColorDepth set to COLOR");
                }
                else if (settings.Colors == PSColors.BlackAndWhite)
                {
                    options.ColorDepth = ColorDepthType.BlackLine;
                    WriteDebugLog("[Export + PDF] ✓ ColorDepth set to BLACK AND WHITE");
                }
                else if (settings.Colors == PSColors.Grayscale)
                {
                    options.ColorDepth = ColorDepthType.GrayScale;
                    WriteDebugLog("[Export + PDF] ✓ ColorDepth set to GRAYSCALE");
                }
                
                // 2. RASTER QUALITY SETTINGS
                WriteDebugLog($"[Export + PDF] Setting RasterQuality: {settings.RasterQuality}");
                if (settings.RasterQuality == PSRasterQuality.High)
                {
                    options.RasterQuality = RasterQualityType.High;
                    WriteDebugLog("[Export + PDF] ✓ RasterQuality set to HIGH (300 DPI)");
                }
                else if (settings.RasterQuality == PSRasterQuality.Medium)
                {
                    options.RasterQuality = RasterQualityType.Medium;
                    WriteDebugLog("[Export + PDF] ✓ RasterQuality set to MEDIUM (150 DPI)");
                }
                else if (settings.RasterQuality == PSRasterQuality.Low)
                {
                    options.RasterQuality = RasterQualityType.Low;
                    WriteDebugLog("[Export + PDF] ✓ RasterQuality set to LOW (72 DPI)");
                }
                else if (settings.RasterQuality == PSRasterQuality.Maximum)
                {
                    options.RasterQuality = RasterQualityType.Presentation;
                    WriteDebugLog("[Export + PDF] ✓ RasterQuality set to MAXIMUM/PRESENTATION (600 DPI)");
                }
                
                // 3. VIEW OPTIONS (hide scope boxes, crop boundaries, etc.)
                options.HideCropBoundaries = settings.HideCropBoundaries;
                options.HideScopeBoxes = settings.HideScopeBoxes;
                options.HideUnreferencedViewTags = settings.HideUnreferencedViewTags;
                
                WriteDebugLog($"[Export + PDF] ✓ HideCropBoundaries: {settings.HideCropBoundaries}");
                WriteDebugLog($"[Export + PDF] ✓ HideScopeBoxes: {settings.HideScopeBoxes}");
                WriteDebugLog($"[Export + PDF] ✓ HideUnreferencedViewTags: {settings.HideUnreferencedViewTags}");
                
                // 4. REPLACE HALFTONE WITH THIN LINES (if supported)
                try
                {
                    options.ReplaceHalftoneWithThinLines = settings.ReplaceHalftone;
                    WriteDebugLog($"[Export + PDF] ✓ ReplaceHalftoneWithThinLines: {settings.ReplaceHalftone}");
                }
                catch (Exception htEx)
                {
                    WriteDebugLog($"[Export + PDF] Info - ReplaceHalftone not supported in this Revit version: {htEx.Message}");
                }
                
                // ===================================================================
                // CRITICAL FOR HIGH QUALITY VECTOR PDF OUTPUT
                // These settings ensure lines, text, and fills remain as vectors
                // ===================================================================
                
                // 5. HIDDEN LINE VIEWS - VECTOR PROCESSING (CRITICAL!)
                // This is THE MOST IMPORTANT setting for quality
                // Ensures hidden lines are processed as vectors, not rasterized
                try
                {
                    // Try to access HiddenLineViews property (may not exist in all Revit versions)
                    var hiddenLineProperty = typeof(PDFExportOptions).GetProperty("HiddenLineViews");
                    if (hiddenLineProperty != null)
                    {
                        // HiddenLineViews enum: VectorProcessing = 0, RasterProcessing = 1
                        // Always use VectorProcessing (0) for best quality
                        hiddenLineProperty.SetValue(options, 0); // 0 = VectorProcessing
                        WriteDebugLog("[Export + PDF] ✅ CRITICAL: HiddenLineViews set to VECTOR PROCESSING");
                        WriteDebugLog("[Export + PDF] This ensures lines, text, and fills remain as vectors");
                    }
                    else
                    {
                        WriteDebugLog("[Export + PDF] ⚠ HiddenLineViews property not available in this Revit version");
                    }
                }
                catch (Exception hvEx)
                {
                    WriteDebugLog($"[Export + PDF] Info - HiddenLineViews not supported: {hvEx.Message}");
                }
                
                // 6. ZOOM SETTINGS - FIT TO PAGE (if supported)
                // Ensures content fills the page properly
                try
                {
                    var zoomTypeProperty = typeof(PDFExportOptions).GetProperty("ZoomType");
                    if (zoomTypeProperty != null)
                    {
                        // ZoomFitType.FitToPage = 0, ZoomFitType.Zoom = 1
                        if (settings.Zoom == PSZoomType.FitToPage)
                        {
                            zoomTypeProperty.SetValue(options, 0); // 0 = FitToPage
                            WriteDebugLog("[Export + PDF] ✓ ZoomType set to FIT TO PAGE");
                        }
                        else
                        {
                            zoomTypeProperty.SetValue(options, 1); // 1 = Zoom (100%)
                            WriteDebugLog("[Export + PDF] ✓ ZoomType set to ZOOM 100%");
                        }
                    }
                }
                catch (Exception zoomEx)
                {
                    WriteDebugLog($"[Export + PDF] Info - ZoomType not supported: {zoomEx.Message}");
                }
                
                // 7. ALWAYS VECTOR TEXT (CRITICAL FOR TEXT QUALITY)
                // Ensures text is exported as selectable vector fonts, not rasterized
                try
                {
                    var vectorTextProperty = typeof(PDFExportOptions).GetProperty("AlwaysUseVectorText");
                    if (vectorTextProperty != null)
                    {
                        vectorTextProperty.SetValue(options, true);
                        WriteDebugLog("[Export + PDF] ✅ CRITICAL: AlwaysUseVectorText = TRUE");
                        WriteDebugLog("[Export + PDF] Text will be selectable and embedded as fonts");
                    }
                }
                catch (Exception vtEx)
                {
                    WriteDebugLog($"[Export + PDF] Info - AlwaysUseVectorText not supported: {vtEx.Message}");
                }
                
                // ===================================================================
                // NOTE: The following settings are NOT supported by PDFExportOptions:
                // - HiddenLineViews (Vector/Raster Processing) - PrintManager only
                // - Zoom Type & Zoom Percentage - PrintManager only
                // - Paper Placement (Center/Offset) - PrintManager only
                // 
                // If you need these settings, must use PrintManager API instead
                // See: PDFExportManager_PrintManager.cs
                // ===================================================================

                WriteDebugLog("[Export + PDF] ===== PDF EXPORT OPTIONS SUMMARY =====");
                WriteDebugLog("[Export + PDF] === QUALITY SETTINGS (CRITICAL FOR VECTOR OUTPUT) ===");
                WriteDebugLog($"[Export + PDF] ColorDepth: {options.ColorDepth} (Color = DeviceRGB, maintains original colors)");
                WriteDebugLog($"[Export + PDF] RasterQuality: {options.RasterQuality} (High/Presentation = 300-600 DPI for raster elements)");
                WriteDebugLog("[Export + PDF] HiddenLineViews: VectorProcessing (attempted via reflection)");
                WriteDebugLog("[Export + PDF] AlwaysUseVectorText: TRUE (attempted via reflection)");
                WriteDebugLog("[Export + PDF] → Lines, text, fills remain as VECTORS (selectable, scalable)");
                WriteDebugLog("[Export + PDF] === FILE OPTIONS ===");
                WriteDebugLog($"[Export + PDF] Combine Files: {options.Combine}");
                WriteDebugLog($"[Export + PDF] PaperFormat: {options.PaperFormat} (auto-detect from sheet)");
                WriteDebugLog("[Export + PDF] === VIEW OPTIONS ===");
                WriteDebugLog($"[Export + PDF] HideCropBoundaries: {options.HideCropBoundaries}");
                WriteDebugLog($"[Export + PDF] HideScopeBoxes: {options.HideScopeBoxes}");
                WriteDebugLog($"[Export + PDF] HideUnreferencedViewTags: {options.HideUnreferencedViewTags}");
                WriteDebugLog("[Export + PDF] ===== EXPECTED RESULT =====");
                WriteDebugLog("[Export + PDF] ✅ Vector PDF: Lines, text, and fills as paths/fonts (not images)");
                WriteDebugLog("[Export + PDF] ✅ File size: ~100 KB - 1 MB per A3 sheet (vector is compact)");
                WriteDebugLog("[Export + PDF] ✅ Text: Selectable and searchable with embedded fonts");
                WriteDebugLog("[Export + PDF] ✅ Quality: Infinite zoom without pixelation");
                WriteDebugLog("[Export + PDF] ===== SETTINGS FROM UI (NOT applied - PDFExportOptions limitations) =====");
                WriteDebugLog($"[Export + PDF] ⚠ Paper Placement: {settings.PaperPlacement} (requires PrintManager)");
                WriteDebugLog($"[Export + PDF] ⚠ Paper Margin: {settings.PaperMargin} (requires PrintManager)");
                WriteDebugLog($"[Export + PDF] ⚠ Offset X: {settings.OffsetX}, Y: {settings.OffsetY} (requires PrintManager)");
                WriteDebugLog($"[Export + PDF] ⚠ Keep Paper Size: {settings.KeepPaperSize} (requires PrintManager)");
                WriteDebugLog("[Export + PDF] ==========================================");
                
                return options;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[Export + PDF] ERROR creating PDF options: {ex.Message}");
                WriteDebugLog($"[Export + PDF] Stack trace: {ex.StackTrace}");
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