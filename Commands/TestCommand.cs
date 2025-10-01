using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;

namespace ProSheetsAddin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class TestCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Debug.WriteLine("[Export +] TestCommand started");
                
                var doc = commandData.Application.ActiveUIDocument.Document;
                Debug.WriteLine($"[Export +] Document: {doc.Title}");
                
                // Test basic functionality first
                var sheets = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(sheet => !sheet.IsTemplate)
                    .ToList();
                
                Debug.WriteLine($"[Export +] Found {sheets.Count} sheets");
                
                // Show simple message
                TaskDialog.Show("Export + Test", 
                    $"Document: {doc.Title}\n" +
                    $"Sheets found: {sheets.Count}\n" +
                    $"Debug output is being written to DebugView.\n\n" +
                    $"Check DebugView for detailed logging.");
                
                // Test creating the main window
                Debug.WriteLine("[Export +] Attempting to create XAML main window");
                
                try 
                {
                    var mainWindow = new Views.ProSheetsMainWindow(doc);
                    Debug.WriteLine("[Export +] XAML main window created successfully");
                    
                    // Show as non-modal window so user can interact with both Revit and ProSheets
                    mainWindow.Show();
                    Debug.WriteLine("[Export +] XAML main window shown (non-modal)");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Export +] Error creating main window: {ex.Message}");
                    Debug.WriteLine($"[Export +] Stack trace: {ex.StackTrace}");
                    
                    TaskDialog.Show("Error", $"Cannot create main window:\n{ex.Message}");
                    return Result.Failed;
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Export +] TestCommand error: {ex.Message}");
                Debug.WriteLine($"[Export +] Stack trace: {ex.StackTrace}");
                
                TaskDialog.Show("Error", $"Command failed:\n{ex.Message}");
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}