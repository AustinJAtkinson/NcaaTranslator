export type ThemeName = "light" | "dark";

const STORAGE_KEY = "ncaa-theme";

function prefersDark(): boolean {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return false;
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

function readStoredTheme(): ThemeName | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === "light" || stored === "dark") return stored;
  } catch {
    /* localStorage can throw in private mode */
  }
  return null;
}

function applyTheme(theme: ThemeName): void {
  const root = document.documentElement;
  root.classList.toggle("dark", theme === "dark");
  root.dataset.theme = theme;
}

export function getTheme(): ThemeName {
  return readStoredTheme() ?? (prefersDark() ? "dark" : "light");
}

export function setTheme(theme: ThemeName): void {
  try {
    localStorage.setItem(STORAGE_KEY, theme);
  } catch {
    /* ignore quota / privacy errors */
  }
  applyTheme(theme);
}

export function applyLaunchTheme(): ThemeName {
  const theme = getTheme();
  applyTheme(theme);
  return theme;
}
