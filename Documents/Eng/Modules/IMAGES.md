# Module: Images & ROI

## The Hook (Q&A)

**Q: How do we prevent faces from being cut off during cropping?**  
The `RoiResolver` utilizes **AI-powered Face Detection** (via YuNet/OpenCV). Before cropping, the system identifies facial coordinates and adjusts the Region of Interest (ROI) to ensure people remain the focus of the slide.

**Q: What happens if no faces are found?**  
The system falls back to standard geometric algorithms like **Center Crop** or **Rule of Thirds**, depending on the configuration.

---

## 1. ROI Algorithms

- **Center**: Simple and fast. Focuses on the middle of the image.
- **Rule of Thirds**: Places the focus points along the grid lines for a more "professional" look.
- **Face-Aware**: The priority mode. Centers the crop around detected faces.

---

## 2. Processing Engine

We use **ImageMagick (Magick.NET)** for high-performance image manipulation. All operations (resize, crop, format conversion) happen in memory before being persisted to the temporary directory.

---

## 3. Face Detection

Uses the `YuNet.onnx` model for lightweight and fast inference, ensuring the sidecar remains fast even on machines without a dedicated GPU.