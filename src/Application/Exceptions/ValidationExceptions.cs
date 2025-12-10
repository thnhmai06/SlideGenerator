namespace Application.Exceptions;

/// <summary>
/// Exception thrown when not enough arguments are provided.
/// </summary>
public class NotEnoughArgumentException(params string[] args)
    : ArgumentException($"Not enough arguments provided. Need provide these argument(s): {string.Join(", ", args)}");

/// <summary>
/// Exception thrown when request does not include a valid type.
/// </summary>
public class TypeNotIncludedException(Type enumType) : ArgumentException(
    $"Type is not included in request/action. Must be one of [{string.Join(", ", Enum.GetNames(enumType))}].")
{
    public Type EnumType { get; } = enumType;
}
