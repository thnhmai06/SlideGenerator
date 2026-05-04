namespace SlideGenerator.Logging.Models;

/// <summary>
///     A lightweight, serializable representation of an exception.
///     Avoids storing full exception objects which can cause serialization cycles or memory leaks.
/// </summary>
/// <param name="Name">The type name of the exception (e.g., System.IO.FileNotFoundException).</param>
/// <param name="Message">The error message describing the failure.</param>
public record ExceptionIdentifier(string Name, string Message);
