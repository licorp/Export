using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Reflection;
using System.Diagnostics;

namespace ProSheetsAddin
{
    [Transaction(TransactionMode.Manual)]
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            Debug.WriteLine("[Export +] Application OnStartup started");
            
            // Tạo ribbon tab
            string tabName = "Export + Tools";
            application.CreateRibbonTab(tabName);
            Debug.WriteLine("[Export +] Created ribbon tab: " + tabName);
            
            // Tạo ribbon panel
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Export Tools");
            
            // Thêm push button cho ProSheets Export
            PushButtonData buttonData = new PushButtonData(
                "SimpleExport", 
                "ProSheets\nExport", 
                Assembly.GetExecutingAssembly().Location, 
                "ProSheetsAddin.Commands.SimpleExportCommand");
            
            PushButton pushButton = panel.AddItem(buttonData) as PushButton;
            pushButton.ToolTip = "Export sheets to PDF, DWG, IFC and other formats with advanced settings";
            
            Debug.WriteLine("[Export +] Ribbon setup completed successfully");
            return Result.Succeeded;
        }
        
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}