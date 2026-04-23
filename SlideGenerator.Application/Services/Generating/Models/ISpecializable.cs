namespace SlideGenerator.Application.Services.Generating.Models;

/// <summary>
///     Defines a contract for flattening a general instruction into specialized instructions.
/// </summary>
/// <typeparam name="TGeneral">The general instruction type.</typeparam>
/// <typeparam name="TSpecialized">The specialized instruction type.</typeparam>
public interface ISpecializable<in TGeneral, out TSpecialized>
    where TGeneral : class
    where TSpecialized : class
{
    /// <summary>
    ///     Flattens a general instruction into a sequence of specialized instructions.
    /// </summary>
    /// <param name="general">The general instruction to flatten.</param>
    /// <returns>An enumeration of specialized instructions.</returns>
    IEnumerable<TSpecialized> Flatten(TGeneral general);
}
