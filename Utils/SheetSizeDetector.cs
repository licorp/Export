using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace ProSheetsAddin.Utils
{
    public class SheetSizeDetector
    {
        public static string GetSheetSize(ViewSheet sheet)
        {
            if (sheet == null) return "A1";

            // Thử theo thứ tự ưu tiên
            string size = GetSizeFromTitleBlock(sheet);
            if (!string.IsNullOrEmpty(size) && size != "Unknown")
                return size;

            size = GetSizeFromSheetParameters(sheet);
            if (!string.IsNullOrEmpty(size) && size != "Unknown")
                return size;

            size = GetSizeFromDimensions(sheet);
            if (!string.IsNullOrEmpty(size) && size != "Unknown")
                return size;

            // Fallback: Dự đoán từ sheet number
            return GuessFromSheetNumber(sheet.SheetNumber);
        }

        private static string GetSizeFromTitleBlock(ViewSheet sheet)
        {
            try
            {
                var titleBlocks = new FilteredElementCollector(sheet.Document, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>();

                foreach (var tb in titleBlocks)
                {
                    // Thử instance parameters trước
                    string size = TryGetParameterValue(tb, new[] {
                        "Sheet Size", "Paper Size", "Size", "Format", "Drawing Size", "Page Size"
                    });
                    
                    if (!string.IsNullOrEmpty(size))
                        return size;

                    // Thử type parameters
                    if (tb.Symbol != null)
                    {
                        size = TryGetParameterValue(tb.Symbol, new[] {
                            "Sheet Size", "Paper Size", "Size", "Format", "Drawing Size", "Page Size"
                        });
                        
                        if (!string.IsNullOrEmpty(size))
                            return size;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting size from title block: {ex.Message}");
            }
            
            return "Unknown";
        }

        private static string GetSizeFromSheetParameters(ViewSheet sheet)
        {
            try
            {
                return TryGetParameterValue(sheet, new[] {
                    "Sheet Size", "Paper Size", "Size", "Format"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting size from sheet parameters: {ex.Message}");
            }
            
            return "Unknown";
        }

        private static string TryGetParameterValue(Element element, string[] parameterNames)
        {
            foreach (string paramName in parameterNames)
            {
                var param = element.LookupParameter(paramName);
                if (param != null && param.HasValue)
                {
                    string value = param.AsString();
                    if (!string.IsNullOrEmpty(value) && value.Trim() != "")
                        return value.Trim();
                }
            }
            return null;
        }

        private static string GetSizeFromDimensions(ViewSheet sheet)
        {
            try
            {
                var outline = sheet.Outline;
                if (outline != null)
                {
                    double widthFeet = outline.Max.U - outline.Min.U;
                    double heightFeet = outline.Max.V - outline.Min.V;
                    
                    // Revit 2021+ uses ForgeTypeId for units
                    // UnitTypeId is available from Revit 2021 onwards
                    double widthMM = UnitUtils.ConvertFromInternalUnits(widthFeet, UnitTypeId.Millimeters);
                    double heightMM = UnitUtils.ConvertFromInternalUnits(heightFeet, UnitTypeId.Millimeters);
                    
                    return DeterminePaperSize(widthMM, heightMM);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating paper size: {ex.Message}");
            }
            
            return "Unknown";
        }

        private static string GuessFromSheetNumber(string sheetNumber)
        {
            if (string.IsNullOrEmpty(sheetNumber))
                return "A1";

            // Dự đoán dựa trên prefix của sheet number
            if (sheetNumber.StartsWith("A0")) return "A0";
            if (sheetNumber.StartsWith("A1")) return "A1";
            if (sheetNumber.StartsWith("A2")) return "A2";
            if (sheetNumber.StartsWith("A3")) return "A3";
            if (sheetNumber.StartsWith("A4")) return "A4";
            
            // Default cho architectural sheets
            if (sheetNumber.StartsWith("A")) return "A1";
            
            return "A1"; // Default fallback
        }

        private static string DeterminePaperSize(double widthMM, double heightMM)
        {
            // Đảm bảo width luôn là chiều dài (lớn hơn)
            if (widthMM < heightMM)
            {
                double temp = widthMM;
                widthMM = heightMM;
                heightMM = temp;
            }

            // Định nghĩa các kích thước chuẩn (tolerance ±15mm)
            var standardSizes = new Dictionary<string, (double width, double height)>
            {
                ["A0"] = (1189, 841),
                ["A1"] = (841, 594),
                ["A2"] = (594, 420),
                ["A3"] = (420, 297),
                ["A4"] = (297, 210),
                ["B0"] = (1414, 1000),
                ["B1"] = (1000, 707),
                ["B2"] = (707, 500),
                ["B3"] = (500, 353),
                ["B4"] = (353, 250),
                ["ANSI C"] = (610, 457),
                ["ANSI D"] = (914, 610),
                ["ANSI E"] = (1118, 864),
                ["Tabloid"] = (432, 279),
                ["Letter"] = (279, 216)
            };

            const double tolerance = 15.0; // 15mm tolerance

            foreach (var size in standardSizes)
            {
                var (w, h) = size.Value;
                
                if (Math.Abs(widthMM - w) <= tolerance && Math.Abs(heightMM - h) <= tolerance)
                {
                    return size.Key;
                }
            }

            // Nếu không khớp với size chuẩn, return custom size
            return $"{widthMM:F0}x{heightMM:F0}mm";
        }
    }
}
