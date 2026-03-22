using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Domain.Slide.Interfaces;

public interface ICopyable<out T> where T : IObject
{
    T Copy(int location);
}