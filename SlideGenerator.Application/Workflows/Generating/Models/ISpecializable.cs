namespace SlideGenerator.Application.Workflows.Generating.Models;

public interface ISpecializable<in TGeneral, out TSpecialized>
    where TGeneral : class
    where TSpecialized : class
{
    IEnumerable<TSpecialized> Flatten(TGeneral general);
}