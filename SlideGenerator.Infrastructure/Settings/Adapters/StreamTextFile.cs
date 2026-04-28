using System.Text;
using SlideGenerator.Application.Modules.Settings.Abstractions;

namespace SlideGenerator.Infrastructure.Settings.Adapters;

/// <summary>
///     Provides a <see cref="FileStream" />-backed implementation of <see cref="ITextFile" />.
/// </summary>
public sealed class StreamTextFile : ITextFile
{
    /// <summary>
    ///     The underlying <see cref="FileStream" /> used for file operations.
    /// </summary>
    private readonly FileStream _stream;

    /// <summary>
    ///     Object used for thread-safe synchronization of stream operations.
    /// </summary>
    private readonly Lock _syncRoot = new();

    /// <summary>
    ///     Indicates whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamTextFile" /> class.
    /// </summary>
    /// <param name="filePath">The absolute path to the text file.</param>
    /// <param name="isEditable">Whether the file should be opened in read-write mode.</param>
    public StreamTextFile(string filePath, bool isEditable = true)
    {
        IsEditable = isEditable;
        FilePath = Path.GetFullPath(filePath);

        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        _stream = new FileStream(
            FilePath,
            FileMode.OpenOrCreate,
            isEditable ? FileAccess.ReadWrite : FileAccess.Read,
            FileShare.Read);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Gets a value indicating whether the file is editable.
    /// </summary>
    public bool IsEditable { get; }

    /// <inheritdoc />
    /// <summary>
    ///     Gets the full file path.
    /// </summary>
    public string FilePath { get; }

    /// <inheritdoc />
    /// <summary>
    ///     Reads the entire content of the file.
    /// </summary>
    /// <returns>The file's content as a <see cref="string" />.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the instance is disposed.</exception>
    public string Read()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();

            _stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(
                _stream,
                Encoding.UTF8,
                true,
                1024,
                true);
            return reader.ReadToEnd();
        }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Writes content to the file, overwriting any existing data.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the file is opened in read-only mode.</exception>
    public void Write(string? content)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            if (!IsEditable)
                throw new InvalidOperationException("File is opened in read-only mode.");

            _stream.Seek(0, SeekOrigin.Begin);
            _stream.SetLength(0);
            using var writer = new StreamWriter(
                _stream,
                new UTF8Encoding(false),
                1024,
                true);
            writer.Write(content ?? string.Empty);
            writer.Flush();
            _stream.Flush();
            _stream.Seek(0, SeekOrigin.Begin);
        }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Closes the file stream and releases all associated resources.
    /// </summary>
    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
                return;

            _stream.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    ///     Throws an <see cref="ObjectDisposedException" /> if the instance is disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the instance is disposed.</exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(StreamTextFile));
    }
}