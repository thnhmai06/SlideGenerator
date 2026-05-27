# Image Module

The **SlideGenerator.Image** module handles all intelligent image processing using **Magick.NET** and **OpenCV**.

## Responsibility
- Intelligent Region of Interest (ROI) calculation.
- Face detection.
- High-performance cropping and resizing.

## ROI Algorithms
1. **Center**: Simple crop centered on the image.
2. **Rule of Thirds**: Places the subject (or center) on rule-of-thirds intersections.
3. **Face Detection**: Uses **YuNet** (OpenCV) to find faces and ensure they are preserved within the cropped area.

## Face Detection Pipeline
- **Adapter**: `YuNet.cs` wraps the OpenCV DNN module.
- **Performance**: MagickImage is converted to an OpenCV `Mat` internally for detection, then the resulting coordinates are used to crop the original MagickImage.

## MagickImage Factory
Provides a unified way to open images from `byte[]` or file paths, ensuring consistent decoding settings across the application.
