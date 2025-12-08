namespace TaoSlideTotNghiep.Exceptions.Services;

public class PresentationNotOpenedException(string filepath)
    : InvalidOperationException("The presentation at the specified filepath is not open: " + filepath);
