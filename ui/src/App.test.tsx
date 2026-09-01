import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen, waitFor } from "@/test/render";
import { resetBridgeMock, sendMessage } from "./test/bridgeMock";
import App from "./App";

vi.mock("./bridge", () => import("./test/bridgeMock"));

function mockMatchMedia(): void {
  Object.defineProperty(window, "matchMedia", {
    writable: true,
    configurable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      matches: false,
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

function mockBridge(): void {
  sendMessage.mockImplementation(async (method: string) => {
    switch (method) {
      case "start":
      case "stop":
      case "status":
        return { running: true, lastUpdate: null };
      case "getScoreboard":
        return { sports: [] };
      case "getSettings":
        return {
          timer: 20,
          homeTeam: null,
          sports: [],
          displayTeams: [],
          xmlToJson: { enabled: false, filePaths: [] },
          clockFormats: {
            preGame: {
              includeWeekday: true,
              fullWeekday: false,
              separator: ". ",
              pattern: "{dayofweek}{separator}{text}",
            },
            final: {
              includeWeekday: true,
              fullWeekday: false,
              separator: " - ",
              pattern: "{text}{separator}{dayofweek}",
            },
          },
        };
      case "getTeams":
      case "getConferences":
        return [];
      default:
        return null;
    }
  });
}

describe("App", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    delete document.documentElement.dataset.theme;
    mockMatchMedia();
    resetBridgeMock();
    mockBridge();
  });

  afterEach(() => {
    cleanup();
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    delete document.documentElement.dataset.theme;
  });

  it("auto-starts polling on mount", async () => {
    renderWithProviders(<App />);
    await waitFor(() => {
      expect(sendMessage).toHaveBeenCalledWith("start");
    });
  });

  it("defaults to the Scoreboard view", async () => {
    const user = userEvent.setup();
    renderWithProviders(<App />);

    expect(screen.getByRole("button", { name: "Scoreboard" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("button", { name: "Start" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Stop" })).toBeInTheDocument();
    expect(screen.queryByRole("tablist", { name: "Settings sections" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Scoreboard" }));
    expect(screen.getByRole("button", { name: "Start" })).toBeVisible();
  });

  it("keeps Settings mounted after visiting it and returning to Scoreboard", async () => {
    const user = userEvent.setup();
    renderWithProviders(<App />);

    await user.click(screen.getByRole("button", { name: "Settings" }));

    expect(await screen.findByText("Timer (seconds)")).toBeInTheDocument();
    expect(screen.queryByRole("tablist", { name: "Settings sections" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Scoreboard" }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Start" })).toBeVisible();
    });

    const hiddenTimer = screen.getByText("Timer (seconds)");
    expect(hiddenTimer).toBeInTheDocument();
    expect(hiddenTimer.closest(".hidden")).not.toBeNull();
  });

  it("lazy-mounts Names on first visit and keeps it mounted", async () => {
    const user = userEvent.setup();
    renderWithProviders(<App />);

    expect(screen.queryByRole("heading", { name: "Teams" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Names" }));
    expect(await screen.findByRole("heading", { name: "Teams" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Scoreboard" }));
    const heading = screen.getByRole("heading", { name: "Teams" });
    expect(heading.closest(".hidden")).not.toBeNull();
  });

  it("toggles collapsed sidebar state from the collapse button", async () => {
    const user = userEvent.setup();
    renderWithProviders(<App />);

    const sidebar = screen.getByRole("complementary", { name: "Sidebar" });
    expect(sidebar).toHaveAttribute("data-collapsed", "false");
    expect(screen.getByRole("button", { name: "General" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));

    expect(sidebar).toHaveAttribute("data-collapsed", "true");
    expect(localStorage.getItem("ncaa-sidebar-collapsed")).toBe("true");
    expect(screen.queryByRole("button", { name: "General" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Scoreboard" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Expand sidebar" })).toBeInTheDocument();
  });
});
