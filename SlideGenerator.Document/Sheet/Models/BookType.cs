namespace SlideGenerator.Document.Sheet.Models;

public enum BookType
{
    Xls,
    Xlsx,
    Xlsm,
    Csv
}

public static class BookTypeExtensions
{
    public static string GetExtension(this BookType type)
    {
        return type switch
        {
            BookType.Xls => ".xls",
            BookType.Xlsx => ".xlsx",
            BookType.Xlsm => ".xlsm",
            BookType.Csv => ".csv",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static BookType FromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".xls" => BookType.Xls,
            ".xlsx" => BookType.Xlsx,
            ".xlsm" => BookType.Xlsm,
            ".csv" => BookType.Csv,
            _ => throw new ArgumentException($"Unsupported file extension: {extension}", nameof(extension))
        };
    }
}