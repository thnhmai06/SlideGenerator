import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import commonEn from "@/features/recipes/i18n/en.json";
// Dynamic imports for lazy loading
import commonVi from "@/features/recipes/i18n/vi.json";

i18n.use(initReactI18next).init({
  resources: {
    vi: { common: commonVi },
    en: { common: commonEn },
  },
  lng: "vi",
  fallbackLng: "vi",
  defaultNS: "common",
  interpolation: { escapeValue: false },
});

export default i18n;
