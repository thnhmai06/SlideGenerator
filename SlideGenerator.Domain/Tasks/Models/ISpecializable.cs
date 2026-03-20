namespace SlideGenerator.Domain.Tasks.Models;

public interface ISpecializable<in TGeneral, out TSpecialized>
     where TGeneral : class
     where TSpecialized : class
{
    IEnumerable<TSpecialized> Specialize(TGeneral general);
}