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
            
            // Thêm push button cho Test Command
            PushButtonData testButtonData = new PushButtonData(
                "TestCommand", 
                "Test\nExport +", 
                Assembly.GetExecutingAssembly().Location, 
                "ProSheetsAddin.Commands.TestCommand");
            
            PushButton testButton = panel.AddItem(testButtonData) as PushButton;
            testButton.ToolTip = "Test Export + functionality with debug output";
            
            // Thêm push button cho Simple Export  
            PushButtonData buttonData = new PushButtonData(
                "SimpleExport", 
                "Export +\nTool", 
                Assembly.GetExecutingAssembly().Location, 
                "ProSheetsAddin.Commands.SimpleExportCommand");
            
            PushButton pushButton = panel.AddItem(buttonData) as PushButton;
            pushButton.ToolTip = "Export sheets to multiple formats";
            
            Debug.WriteLine("[Export +] Ribbon setup completed successfully");
            return Result.Succeeded;
        }
        
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}