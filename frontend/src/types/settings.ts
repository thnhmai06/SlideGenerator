export interface Proxy {
  useProxy: boolean;
  domain: string;
  password: string;
  proxyAddress: string;
  username: string;
}
export interface RetrySetting {
  maxRetries: number; // default 3
  timeout: number; // seconds, default 30
}
export interface NetworkSetting {
  proxy: Proxy;
  retry: RetrySetting;
}
export interface PerformanceSetting {
  maxParallelDownloadImage: number; // default 5
  maxParallelEditImage: number;
  maxParallelEditPresentation: number;
  maxParallelReadWorkbook: number;
  maxParallelReadPresentation: number;
}
export interface Setting {
  network: NetworkSetting;
  performance: PerformanceSetting;
}
export interface SettingsDto {
  setting: Setting;
  requiresCredentialReentry: boolean;
}
