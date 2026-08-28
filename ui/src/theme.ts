export type ThemeName = "light" | "dark";

export function applyLaunchTheme(): ThemeName {
  const dark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  const theme: ThemeName = dark ? "dark" : "light";
  document.documentElement.dataset.theme = theme;
  return theme;
}
