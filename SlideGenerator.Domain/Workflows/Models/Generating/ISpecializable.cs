namespace SlideGenerator.Domain.Workflows.Models.Generating;

public interface ISpecializable<in TGeneral, out TSpecialized>
     where TGeneral : class
     where TSpecialized : class
{
    IEnumerable<TSpecialized> Flatten(TGeneral general);
}