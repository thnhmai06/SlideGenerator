using SlideGenerator.Document.Sheet.Models;
using Syncfusion.XlsIO;

namespace SlideGenerator.Document.Sheet.Entities;

/// <summary>
///     Wraps a Syncfusion IWorkbook and its FileStream for proper disposal and saving.
///     Utilizes lazy initialization to defer file access until the <see cref="Value" /> is accessed.
/// </summary>
public sealed class SfWorkbook : IDisposable
{
    private readonly ExcelEngine _excelEngine;
    private readonly BookIdentifier _identifier;
    private readonly bool _isWritable;
    private readonly Lazy<IWorkbook> _lazyValue;
    private FileStream? _fileStream;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SfWorkbook" /> class.
    /// </summary>
    /// <param name="excelEngine">The Excel engine used to open the workbook.</param>
    /// <param name="identifier">The identifier containing path and connection info.</param>
    /// <param name="isWritable">Whether to open the workbook in read-write mode.</param>
    public SfWorkbook(ExcelEngine excelEngine, BookIdentifier identifier, bool isWritable = false)
    {
        _identifier = identifier;
        _excelEngine = excelEngine;
        _isWritable = isWritable;
        _lazyValue = new Lazy<IWorkbook>(InitializeWorkbook);
    }

    /// <summary>
    ///     Gets the underlying Syncfusion workbook handle.
    ///     Accessing this property triggers the lazy initialization and opens the file.
    /// </summary>
    public IWorkbook Value => _lazyValue.Value;

    /// <summary>
    ///     Closes the workbook and disposes of any underlying file streams.
    ///     Only closes the workbook if it was actually opened.
    /// </summary>
    public void Dispose()
    {
        if (_lazyValue.IsValueCreated)
            Value.Close();

        _fileStream?.Dispose();
    }

    /// <summary>
    ///     Performs the actual opening of the workbook file based on its type and access mode.
    /// </summary>
    /// <returns>The opened <see cref="IWorkbook" />.</returns>
    private IWorkbook InitializeWorkbook()
    {
        switch (_identifier.GetBookType())
        {
            case BookType.Csv:
                if (_isWritable) return _excelEngine.Excel.Workbooks.Open(_identifier.BookPath, _identifier.Separator);

                _fileStream = new FileStream(
                    _identifier.BookPath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);
                return _excelEngine.Excel.Workbooks.Open(_fileStream, _identifier.Separator);

            case BookType.Xls:
            case BookType.Xlsx:
            case BookType.Xlsm:
            default:
                return _excelEngine.Excel.Workbooks.Open(_identifier.BookPath, ExcelParseOptions.Default,
                    !_isWritable, _identifier.BookPassword);
        }
    }

    /// <summary>
    ///     Saves the workbook to its original location if it has been initialized.
    ///     If the workbook was never accessed via <see cref="Value" />, this method does nothing.
    /// </summary>
    public void Save()
    {
        if (!_lazyValue.IsValueCreated) return;

        switch (_identifier.GetBookType())
        {
            case BookType.Csv:
                if (_fileStream == null)
                    Value.SaveAs(_fileStream, _identifier.Separator);
                else
                    Value.SaveAs(_identifier.BookPath, _identifier.Separator);
                break;

            case BookType.Xls:
            case BookType.Xlsx:
            case BookType.Xlsm:
            default:
                if (_fileStream == null)
                    Value.SaveAs(_identifier.BookPath);
                else
                    Value.SaveAs(_fileStream);
                break;
        }
    }
}