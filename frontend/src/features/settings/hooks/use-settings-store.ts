import { defaultSetting } from "@/config/defaults";
import type { Setting } from "@/types/settings";
import { create } from "zustand";

interface SettingsDialogStore {
  isOpen: boolean;
  activeTab: string;
  open: (tab?: string) => void;
  close: () => void;
  setActiveTab: (tab: string) => void;
}

export const useSettingsDialogStore = create<SettingsDialogStore>((set) => ({
  isOpen: false,
  activeTab: "network",
  open: (tab) => set({ isOpen: true, activeTab: tab ?? "network" }),
  close: () => set({ isOpen: false }),
  setActiveTab: (tab) => set({ activeTab: tab }),
}));

interface SettingsStore {
  setting: Setting;
  updateSetting: (setting: Setting) => void;
  resetToDefaults: () => void;
}

export const useSettingsStore = create<SettingsStore>((set) => ({
  setting: defaultSetting,
  updateSetting: (setting) => set({ setting }),
  resetToDefaults: () => set({ setting: defaultSetting }),
}));
