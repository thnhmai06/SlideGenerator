using Syncfusion.XlsIO;

namespace SlideGenerator.Documents.Sheets;

public static class Utilities
{
    extension(IWorksheet ws)
    {
        public IReadOnlyList<string> GetHeaders() => ws.GetRow(0);

        public int CountRows()
        {
            var used = ws.UsedRange;
            return used != null ? Math.Max(0, used.LastRow - used.Row) : 0; // exclude header row
        }

        public IReadOnlyList<string> GetRow(int rowIndex)
        {
            var used = ws.UsedRange;
            if (used == null || rowIndex < 0) return [];

            var cols = used.LastColumn - used.Column + 1;
            var dataRowAbsolute = used.Row + rowIndex;
            var result = new List<string>(cols);
        
            for (var col = 0; col < cols; col++)
            {
                var absCol = used.Column + col;
                result.Add(ws.Range[dataRowAbsolute, absCol]?.DisplayText ?? string.Empty);
            }

            return result;
        }
    }
}
