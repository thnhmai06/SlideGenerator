from pathlib import Path

import cv2
import numpy as np
import imageio.v3 as iio
from pillow_heif import register_heif_opener

from src.core.exceptions import ComputeSaliencyFailed, ReadImageFailed

register_heif_opener()


class Image:
    """Represents an image and provides methods for saliency computation and cropping."""
    SALIENCY = cv2.saliency.StaticSaliencyFineGrained.create()

    def __init__(self, file_path: Path):
        """Initializes the Image object by loading the image from the specified file path.
        Args:
            file_path (Path): The path to the image file.
        Raises:
            FileNotFoundError: If the file does not exist.
            ReadImageFailed: If the image cannot be read.
        """
        self.file_path = file_path
        if not file_path.is_file():
            raise FileNotFoundError(file_path)

        self._data = iio.imread(str(file_path), index=0)
        if self._data is None:
            raise ReadImageFailed(self.file_path)

    @property
    def data(self):
        """Returns the loaded image."""
        return self._data

    def compute_saliency(self) -> tuple[np.ndarray, np.ndarray]:
        """Computes the saliency map of the image.
        Returns:
            tuple[np.ndarray, np.ndarray]: (saliency_map, thresh_map) (0-1)
        Raises:
            ComputeSaliencyFailed: If the saliency computation fails.
        """
        success, saliency_map = self.SALIENCY.computeSaliency(self._data)

        if not success:
            raise ComputeSaliencyFailed(self.file_path)

        thresh_map = cv2.threshold(saliency_map.astype("uint8"), 0, 255, cv2.THRESH_BINARY | cv2.THRESH_OTSU)[1]
        return saliency_map, thresh_map

    def get_prominent_crop(self, target_w: int, target_h: int):
        """
        Finds the best crop of the image based on saliency sum/mean.
        Args:
            target_w (int): The target width of the crop.
            target_h (int): The target height of the crop.
        Returns:
            tuple[tuple[int, int], float]: ((top_left_x, top_left_y), best_saliency_value)
        """
        saliency_map, _ = self.compute_saliency()
        img = self._data.copy()
        h, w = img.shape[:2]

        # Resize if image is smaller than target size
        ratio = max(target_h / h, target_w / w, 1.0)
        if ratio > 1:
            w = int(w * ratio)
            h = int(h * ratio)
            img = cv2.resize(img, (w, h), interpolation=cv2.INTER_AREA)
            saliency_map = cv2.resize(saliency_map, (w, h), interpolation=cv2.INTER_NEAREST)
        t_w = min(w, target_w)
        t_h = min(h, target_h)

        # Compute the score map using box filter
        score_map = cv2.boxFilter(saliency_map, ddepth=-1, ksize=(t_w, t_h),
                                  normalize=True, borderType=cv2.BORDER_CONSTANT)
        _, best_saliency_val, _, max_loc = cv2.minMaxLoc(score_map)

        center_x, center_y = max_loc
        top_left_x = int(center_x - t_w / 2)
        top_left_y = int(center_y - t_h / 2)

        top_left_x = max(0, min(top_left_x, w - t_w))
        top_left_y = max(0, min(top_left_y, h - t_h))
        return (top_left_x, top_left_y), best_saliency_val
    
    def get_center_crop(self, target_w: int, target_h: int) -> tuple[int, int]:
        """Calculates the center crop coordinates for the image.
        Args:
            target_w (int): The target width of the crop.
            target_h (int): The target height of the crop.
        Returns:
            tuple[int, int]: (top_left_x, top_left_y)
        """
        h, w = self._data.shape[:2]
        top_left_x = max(0, (w - target_w) // 2)
        top_left_y = max(0, (h - target_h) // 2)
        return top_left_x, top_left_y

    def crop(self, x: int, y: int, w: int, h: int) -> np.ndarray:
        """Crops image to specified position and size.
        Args:
            x (int): The x-coordinate of the top-left corner of the crop.
            y (int): The y-coordinate of the top-left corner of the crop.
            w (int): The width of the crop.
            h (int): The height of the crop.
        Returns:
            np.ndarray: The cropped image.
        """
        self._data = self._data[y:y + h, x:x + w]
        return self._data

    def save(self, file_path: Path = None):
        """Saves the current image to the specified file path.
        Args:
            file_path (Path, optional): The path to save the image. If None, saves to the original file path.
        """
        if file_path is None:
            file_path = self.file_path
        iio.imwrite(str(file_path), self._data)
