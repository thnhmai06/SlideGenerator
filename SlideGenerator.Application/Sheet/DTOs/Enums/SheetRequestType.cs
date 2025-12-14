using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Sheet.DTOs.Enums;

/// <summary>
///     Types of sheet requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SheetRequestType
{
    BookOpen,
    BookClose,
    BookSheets,
    BookInfo,
    SheetHeaders,
    SheetRow
}