using Autodesk.Revit.DB;
using ProSheetsAddin.Utils;

namespace ProSheetsAddin.Models
{
    public class PaperSize
    {
        public string Name { get; set; }
        public double Width { get; set; }  // mm
        public double Height { get; set; } // mm
        public bool IsStandard { get; set; }
        public PageOrientationType Orientation { get; set; }
        
        public static PaperSize Default => new PaperSize 
        { 
            Name = "A3", 
            Width = 297, 
            Height = 420, 
            IsStandard = true,
            Orientation = PageOrientationType.Portrait
        };
        
        public override string ToString()
        {
            return $"{Name} ({Width:F0}×{Height:F0}mm)";
        }
    }
}