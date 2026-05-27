import type { SettingsDto } from "@/types/settings";

export const mockSettingsDto: SettingsDto = {
  setting: {
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
  },
  requiresCredentialReentry: false,
};
