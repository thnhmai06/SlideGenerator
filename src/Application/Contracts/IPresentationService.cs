using Domain.Models;

namespace Application.Contracts
{
    /// <summary>
    /// Interface for template presentation service.
    /// </summary>
    public interface ITemplateService
    {
        bool AddTemplate(string filepath);
        bool RemoveTemplate(string filepath);
        TemplatePresentation GetTemplate(string filepath);
    }

    /// <summary>
    /// Interface for generating presentation service.
    /// </summary>
    public interface IGeneratingService
    {
        bool AddDerivedPresentation(string filepath, string sourcePath);
        bool RemoveDerivedPresentation(string filepath);
        DerivedPresentation GetDerivedPresentation(string filepath);
    }
}
