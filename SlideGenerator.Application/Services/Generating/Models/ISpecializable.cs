namespace SlideGenerator.Application.Services.Generating.Models;

/// <summary>
///     Defines a contract for flattening a general instruction into specialized instructions using row data.
/// </summary>
/// <typeparam name="TGeneral">The general instruction type.</typeparam>
/// <typeparam name="TSpecialized">The specialized instruction type containing the resolved value.</typeparam>
public interface ISpecializable<in TGeneral, out TSpecialized>
    where TGeneral : class
    where TSpecialized : class
{
    /// <summary>
    ///     Flattens a general instruction into a sequence of potential specialized instructions by resolving values from row
    ///     data.
    /// </summary>
    /// <param name="general">The general instruction to flatten.</param>
    /// <param name="rowContent">The data row containing values mapped by column names.</param>
    /// <returns>An enumeration of specialized instructions, each representing a candidate source resolved to its actual value.</returns>
    IEnumerable<TSpecialized> Flatten(TGeneral general, IReadOnlyDictionary<string, string> rowContent);
}