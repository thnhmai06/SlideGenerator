using System.Text;
using SlideGenerator.Application.Settings.Abstractions;

namespace SlideGenerator.Infrastructure.Settings.Adapters;

public sealed class StreamTextFile : ITextFile
{
    private readonly Lock _syncRoot = new();
    private readonly FileStream _stream;
    private bool _disposed;

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

    public string FilePath { get; }

    public bool IsEditable { get; }

    public string Read()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();

            _stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(
                _stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true);
            return reader.ReadToEnd();
        }
    }

    public void Write(string? content)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            if (!IsEditable)
                throw new InvalidOperationException("Text file is opened in read-only mode.");

            _stream.Seek(0, SeekOrigin.Begin);
            _stream.SetLength(0);
            using var writer = new StreamWriter(
                _stream,
                new UTF8Encoding(false),
                bufferSize: 1024,
                leaveOpen: true);
            writer.Write(content ?? string.Empty);
            writer.Flush();
            _stream.Flush();
            _stream.Seek(0, SeekOrigin.Begin);
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
                return;

            _stream.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(StreamTextFile));
    }
}