using System.Text.Json.Serialization;

namespace TaoSlideTotNghiep.Application.DTOs.Requests;

#region Enums

/// <summary>
/// Types of requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestType
{
    Presentation,
    Download,
    Image,
    Sheet
}

#endregion

#region Classes

/// <summary>
/// Base request class.
/// </summary>
public abstract record Request(RequestType RequestType);

#endregion