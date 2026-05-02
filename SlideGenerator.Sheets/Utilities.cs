using Syncfusion.XlsIO;

namespace SlideGenerator.Sheets;

public static class Utilities
{
    extension(IWorksheet ws)
    {
        public IReadOnlyList<string> GetHeaders()
        {
            var used = ws.UsedRange;
            if (used == null) return [];

            var headers = new List<string>();
            for (var col = used.Column; col <= used.LastColumn; col++)
                headers.Add(ws.Range[used.Row, col]?.DisplayText ?? string.Empty);
            return headers;
        }

        public int CountRows()
        {
            var used = ws.UsedRange;
            return used != null ? Math.Max(0, used.LastRow - used.Row) : 0;
        }

        public IReadOnlyDictionary<string, string> GetRow(int rowIndex, IReadOnlyList<string>? headers = null)
        {
            headers ??= ws.GetHeaders();
            var used = ws.UsedRange;
            if (used == null || rowIndex <= 0) return new Dictionary<string, string>();

            var dataRowAbsolute = used.Row + rowIndex;
            var result = new Dictionary<string, string>(headers.Count, StringComparer.OrdinalIgnoreCase);
        
            for (var col = 0; col < headers.Count; col++)
            {
                var header = headers[col];
                if (string.IsNullOrEmpty(header)) continue;

                var absCol = used.Column + col;
                result[header] = ws.Range[dataRowAbsolute, absCol]?.DisplayText ?? string.Empty;
            }

            return result;
        }
    }
}