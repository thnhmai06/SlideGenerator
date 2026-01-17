using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using SlideGenerator.Framework.Image.Modules.FaceDetection.Models;
using SlideGenerator.Infrastructure.Features.Images.Services;
using CoreImage = SlideGenerator.Framework.Image.Models.Image;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SlideGenerator.Tests.Infrastructure;

[TestClass]
public class ResizingFaceDetectorModelTests
{
    private FakeFaceDetectorModel _fakeInner = null!;
    private FakeLogger _fakeLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _fakeInner = new FakeFaceDetectorModel();
        _fakeLogger = new FakeLogger();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fakeInner.Dispose();
    }

    private CoreImage CreateTestImage(int width, int height)
    {
        // Create a simple image in memory
        var mat = new Mat(height, width, DepthType.Cv8U, 3);
        mat.SetTo(new MCvScalar(255, 255, 255)); // White image

        // Use reflection or a helper to create CoreImage since it doesn't have a public constructor taking Mat
        // Actually CoreImage has a constructor taking byte[]. 
        // But to avoid encoding/decoding overhead in test, let's use the reflection trick used in the main code
        // OR better: Create a valid PNG byte array from the Mat and use the public constructor.

        // Let's try the public constructor with bytes to be safe and "real".
        // To avoid dependency on ImageMagick in test setup if possible, let's just use the Mat directly 
        // if we can inject it. But CoreImage.Mat is internal set.

        // We will rely on the fact that we can construct it via file or bytes.
        // Let's use the byte[] constructor.
        using var vector = new VectorOfByte();
        CvInvoke.Imencode(".png", mat, vector);
        return new CoreImage(vector.ToArray());
    }

    [TestMethod]
    public async Task DetectAsync_WithZeroMaxDim_ShouldNotResize()
    {
        // Arrange
        var model = new ResizingFaceDetectorModel(_fakeInner, () => 0, _fakeLogger);
        using var image = CreateTestImage(2000, 2000);

        // Act
        await model.DetectAsync(image, 0.5f);

        // Assert
        Assert.AreEqual(2000, _fakeInner.LastDetectedImageSize.Width);
        Assert.AreEqual(2000, _fakeInner.LastDetectedImageSize.Height);
    }

    [TestMethod]
    public async Task DetectAsync_WithSmallImage_ShouldNotResize()
    {
        // Arrange
        var model = new ResizingFaceDetectorModel(_fakeInner, () => 1500, _fakeLogger);
        using var image = CreateTestImage(1000, 1000);

        // Act
        await model.DetectAsync(image, 0.5f);

        // Assert
        Assert.AreEqual(1000, _fakeInner.LastDetectedImageSize.Width);
        Assert.AreEqual(1000, _fakeInner.LastDetectedImageSize.Height);
    }

    [TestMethod]
    public async Task DetectAsync_WithLargeImage_ShouldResizeAndScaleResults()
    {
        // Arrange
        var model = new ResizingFaceDetectorModel(_fakeInner, () => 500, _fakeLogger);
        using var image = CreateTestImage(1000, 1000); // 1000x1000 -> Should resize to 500x500 (Scale 0.5)

        // Setup fake result on the *resized* image
        // The inner model sees a 500x500 image. 
        // Let's say it finds a face at (50, 50) with size (100, 100).
        // The original face should be at (100, 100) with size (200, 200).
        _fakeInner.FacesToReturn = new List<Face>
        {
            new(new Rectangle(50, 50, 100, 100), 0.9f)
        };

        // Act
        var results = await model.DetectAsync(image, 0.5f);

        // Assert
        Assert.AreEqual(500, _fakeInner.LastDetectedImageSize.Width);
        Assert.AreEqual(500, _fakeInner.LastDetectedImageSize.Height);

        Assert.HasCount(1, results);
        var face = results[0];

        // Check scaled coordinates
        // Expected: 50 / 0.5 = 100
        Assert.AreEqual(100, face.Rect.X);
        Assert.AreEqual(100, face.Rect.Y);
        Assert.AreEqual(200, face.Rect.Width);
        Assert.AreEqual(200, face.Rect.Height);
    }
}

// Fake classes
public class FakeFaceDetectorModel : FaceDetectorModel
{
    public Size LastDetectedImageSize { get; private set; }
    public List<Face> FacesToReturn { get; set; } = new();

    public override bool IsModelAvailable => true;

    public override void Dispose()
    {
    }

    public override Task<bool> InitAsync()
    {
        return Task.FromResult(true);
    }

    public override Task<bool> DeInitAsync()
    {
        return Task.FromResult(true);
    }

    public override Task<List<Face>> DetectAsync(CoreImage image, float minScore)
    {
        LastDetectedImageSize = image.Size;
        return Task.FromResult(FacesToReturn);
    }
}

public class FakeLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // No-op
    }
}