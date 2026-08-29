import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { applyLaunchTheme, getTheme, setTheme } from "./theme";

function mockMatchMedia(matches: boolean): void {
  Object.defineProperty(window, "matchMedia", {
    writable: true,
    configurable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      matches,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  });
}

function resetDomTheme(): void {
  document.documentElement.classList.remove("dark");
  delete document.documentElement.dataset.theme;
  localStorage.clear();
}

describe("theme", () => {
  beforeEach(() => {
    resetDomTheme();
    mockMatchMedia(false);
  });

  afterEach(() => {
    resetDomTheme();
    vi.restoreAllMocks();
  });

  it("defaults to light when OS prefers light and nothing is stored", () => {
    mockMatchMedia(false);
    const theme = applyLaunchTheme();

    expect(theme).toBe("light");
    expect(getTheme()).toBe("light");
    expect(document.documentElement.dataset.theme).toBe("light");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("defaults to dark when OS prefers dark and nothing is stored", () => {
    mockMatchMedia(true);
    const theme = applyLaunchTheme();

    expect(theme).toBe("dark");
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("persists the chosen theme in localStorage", () => {
    setTheme("dark");

    expect(localStorage.getItem("ncaa-theme")).toBe("dark");
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);

    setTheme("light");

    expect(localStorage.getItem("ncaa-theme")).toBe("light");
    expect(document.documentElement.dataset.theme).toBe("light");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("prefers the stored theme over OS preference", () => {
    localStorage.setItem("ncaa-theme", "dark");
    mockMatchMedia(false);

    expect(applyLaunchTheme()).toBe("dark");
    expect(getTheme()).toBe("dark");
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });
});
