import type { Setting } from "@/types/settings";
import type { GeneratingRequest } from "@/types/workflow";

export const defaultGeneratingRequest: GeneratingRequest = {
  recipeId: 1,
  name: "Lần xuất mới",
  outputType: "Pptx",
  saveFolder: "C:\\Users\\User\\Documents\\SlideGenerator\\Output",
  downloadAssetsPath: "C:\\Users\\User\\Documents\\SlideGenerator\\Assets\\Download",
  editAssetsPath: "C:\\Users\\User\\Documents\\SlideGenerator\\Assets\\Edit",
  allowLocalImagePaths: false,
};

export const defaultSetting: Setting = {
  network: {
    proxy: {
      useProxy: false,
      domain: "",
      password: "",
      proxyAddress: "",
      username: "",
    },
    retry: {
      maxRetries: 3,
      timeout: 30,
    },
  },
  performance: {
    maxParallelDownloadImage: 5,
    maxParallelEditImage: 5,
    maxParallelEditPresentation: 5,
    maxParallelReadWorkbook: 5,
    maxParallelReadPresentation: 5,
  },
};
