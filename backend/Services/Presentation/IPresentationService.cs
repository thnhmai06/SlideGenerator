namespace TaoSlideTotNghiep.Services.Presentation;

public interface IPresentationService
{
    bool AddPresentation(string filepath, string? sourcePath);
    bool RemovePresentation(string filepath);
}
