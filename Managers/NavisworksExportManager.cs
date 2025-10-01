using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using ProSheetsAddin.Models;

namespace ProSheetsAddin.Managers
{
    /// <summary>
    /// Manager for exporting to Navisworks format (NWC)
    /// </summary>
    public class NavisworksExportManager
    {
        private readonly Document _document;

        public NavisworksExportManager(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>
        /// Export 3D views to Navisworks Cache (NWC) format
        /// </summary>
        public bool ExportToNavisworks(List<ViewItem> selectedViews, string outputFolder, string fileNamePrefix = "")
        {
            try
            {
                if (selectedViews == null || !selectedViews.Any())
                {
                    throw new ArgumentException("No views selected for Navisworks export");
                }

                if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
                {
                    throw new ArgumentException("Invalid output folder");
                }

                // Filter only 3D views for Navisworks export
                var threeDViews = selectedViews.Where(v => v.ViewType.Contains("ThreeD") || v.ViewType.Contains("3D")).ToList();
                
                if (!threeDViews.Any())
                {
                    // If no 3D views, try to create a simple model export
                    return ExportModelToNavisworks(outputFolder, fileNamePrefix);
                }

                int exportedCount = 0;
                foreach (var viewItem in threeDViews)
                {
                    try
                    {
                        var view = _document.GetElement(viewItem.RevitViewId) as View3D;
                        if (view != null)
                        {
                            string fileName = !string.IsNullOrEmpty(viewItem.CustomFileName) 
                                ? viewItem.CustomFileName 
                                : $"{fileNamePrefix}{view.Name}";
                            
                            // Clean filename
                            fileName = CleanFileName(fileName);
                            string fullPath = Path.Combine(outputFolder, $"{fileName}.nwc");

                            // Use Navisworks export options
                            var options = new NavisworksExportOptions
                            {
                                ExportScope = NavisworksExportScope.View,
                                ViewId = view.Id
                            };

                            // Export to Navisworks
                            _document.Export(outputFolder, fileName, options);
                            exportedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other views
                        System.Diagnostics.Debug.WriteLine($"Error exporting view {viewItem.ViewName}: {ex.Message}");
                    }
                }

                return exportedCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navisworks export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export entire model to Navisworks
        /// </summary>
        private bool ExportModelToNavisworks(string outputFolder, string fileNamePrefix)
        {
            try
            {
                string fileName = !string.IsNullOrEmpty(fileNamePrefix) 
                    ? $"{fileNamePrefix}_Model" 
                    : $"{_document.Title}_Model";
                
                fileName = CleanFileName(fileName);
                
                var options = new NavisworksExportOptions
                {
                    ExportScope = NavisworksExportScope.Model
                };

                _document.Export(outputFolder, fileName, options);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Model export to Navisworks error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export sheets as reference for coordination (limited functionality)
        /// </summary>
        public bool ExportSheetsReference(List<SheetItem> selectedSheets, string outputFolder, string fileNamePrefix = "")
        {
            try
            {
                // Navisworks typically works with 3D models, not 2D sheets
                // This creates a reference file with sheet information
                
                string fileName = !string.IsNullOrEmpty(fileNamePrefix) 
                    ? $"{fileNamePrefix}_Sheets_Reference" 
                    : "Sheets_Reference";
                
                fileName = CleanFileName(fileName);
                string fullPath = Path.Combine(outputFolder, $"{fileName}.txt");

                var content = new List<string>
                {
                    "Revit Sheets Reference for Navisworks",
                    $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"Document: {_document.Title}",
                    "",
                    "Selected Sheets:"
                };

                foreach (var sheet in selectedSheets)
                {
                    content.Add($"- {sheet.SheetNumber}: {sheet.SheetName}");
                }

                File.WriteAllLines(fullPath, content);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sheets reference export error: {ex.Message}");
                return false;
            }
        }

        public bool ExportSheetsReference(List<SheetItem> sheets, string outputFolder)
        {
            try
            {
                if (sheets?.Any() != true)
                    return false;

                string fileName = "Sheets_Reference";
                string filePath = Path.Combine(outputFolder, $"{fileName}.nwc");

                // Create 3D view for reference
                var collector = new FilteredElementCollector(_document);
                var view3D = collector.OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .FirstOrDefault(v => !v.IsTemplate);

                if (view3D == null)
                {
                    return false;
                }

                var exportOptions = new NavisworksExportOptions();
                exportOptions.ExportScope = NavisworksExportScope.View;
                exportOptions.ViewId = view3D.Id;

                try
                {
                    _document.Export(outputFolder, fileName, exportOptions);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Untitled";
            
            // Remove invalid characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            
            // Replace spaces and special characters
            fileName = fileName.Replace(' ', '_')
                             .Replace('/', '_')
                             .Replace('\\', '_')
                             .Replace(':', '_')
                             .Replace('*', '_')
                             .Replace('?', '_')
                             .Replace('"', '_')
                             .Replace('<', '_')
                             .Replace('>', '_')
                             .Replace('|', '_');
            
            return fileName;
        }
    }
}