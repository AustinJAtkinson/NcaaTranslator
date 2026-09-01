import { readFileSync } from "node:fs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { toast } from "sonner";
import { cleanup, fireEvent, renderWithProviders, screen, waitFor, within } from "@/test/render";
import { resetBridgeMock, sendMessage } from "./test/bridgeMock";
import SettingsTab, { FINAL_CLOCK_DEFAULTS, PRE_GAME_CLOCK_DEFAULTS } from "./SettingsTab";
import type { SettingsSnapshot, SportSnapshot, TeamNameSnapshot } from "./types";

vi.mock("./bridge", () => import("./test/bridgeMock"));

const football: SportSnapshot = {
  name: "Football",
  short: "FB",
  code: "MFB",
  enabled: true,
  conferenceName: "SEC",
  division: 1,
  week: 1,
  seasonYear: 2025,
  gameDisplayMode: "Live",
  listsNeeded: { conferenceGames: true, nonConferenceGames: true, top25Games: true },
  oosUpdater: {
    enabled: false,
    oosFilePath: null,
    oosFileName: null,
    numberOfOutScores: 0,
    numberOfTeamsPer: 0,
  },
};

const basketball: SportSnapshot = {
  name: "Basketball",
  short: "BB",
  code: "MBB",
  enabled: false,
  conferenceName: "ACC",
  division: 1,
  week: 2,
  seasonYear: null,
  gameDisplayMode: "All",
  listsNeeded: { conferenceGames: true, nonConferenceGames: false, top25Games: true },
  oosUpdater: {
    enabled: false,
    oosFilePath: null,
    oosFileName: null,
    numberOfOutScores: 0,
    numberOfTeamsPer: 0,
  },
};

const teams: TeamNameSnapshot[] = [
  { name6Char: "DUKE", customName: "Duke", seoname: "duke", nameShort: "Duke" },
  { name6Char: "UNC", customName: "North Carolina", seoname: "unc", nameShort: "UNC" },
];

const baseSettings: SettingsSnapshot = {
  timer: 20,
  homeTeam: null,
  sports: [football, basketball],
  displayTeams: [{ ncaaTeamName: "Duke" }],
  xmlToJson: { enabled: false, filePaths: ["/tmp/games.xml"] },
  clockFormats: {
    preGame: { ...PRE_GAME_CLOCK_DEFAULTS },
    final: { ...FINAL_CLOCK_DEFAULTS },
  },
};

function mockBridge(settings: SettingsSnapshot = baseSettings): void {
  sendMessage.mockImplementation(async (method: string, params?: unknown) => {
    switch (method) {
      case "getSettings":
        return settings;
      case "saveSettings":
        return params;
      case "getTeams":
        return teams;
      case "getConferences":
        return [
          { conferenceSeo: "sec", customConferenceName: "SEC" },
          { conferenceSeo: "acc", customConferenceName: "ACC" },
        ];
      case "pickFolder":
      case "pickFile":
        return { path: null };
      default:
        return null;
    }
  });
}

function savedSettings(): SettingsSnapshot[] {
  return sendMessage.mock.calls
    .filter((call) => call[0] === "saveSettings")
    .map((call) => call[1] as SettingsSnapshot);
}

describe("SettingsTab", () => {
  beforeEach(() => {
    resetBridgeMock();
    mockBridge();
    vi.spyOn(toast, "error").mockImplementation(() => "id");
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it("does not use window.alert in the module", () => {
    const source = readFileSync("src/SettingsTab.tsx", "utf8");
    expect(source).not.toMatch(/window\.alert/);
    expect(source).not.toMatch(/window\.confirm/);
  });

  it("does not save before settings have loaded", async () => {
    let resolveSettings: ((value: SettingsSnapshot) => void) | undefined;
    sendMessage.mockImplementation(async (method: string, params?: unknown) => {
      if (method === "getSettings") {
        return await new Promise<SettingsSnapshot>((resolve) => {
          resolveSettings = resolve;
        });
      }
      if (method === "saveSettings") return params;
      if (method === "getTeams") return teams;
      if (method === "getConferences") return [];
      return null;
    });

    renderWithProviders(<SettingsTab section="general" />);
    expect(screen.queryByText("Timer (seconds)")).not.toBeInTheDocument();
    expect(savedSettings()).toHaveLength(0);

    resolveSettings?.(baseSettings);
    expect(await screen.findByText("Timer (seconds)")).toBeInTheDocument();
    expect(savedSettings()).toHaveLength(0);
  });

  describe("general", () => {
    it("saves a new timer from the ComboBox", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="general" />);

      const timer = await screen.findByRole("combobox", { name: "Timer (seconds)" });
      await user.click(timer);
      await user.click(screen.getByRole("option", { name: "30" }));

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].timer).toBe(30);
    });

    it("renders pre-game and final clock groups with token hints", async () => {
      renderWithProviders(<SettingsTab section="general" />);

      const preGame = await screen.findByRole("group", { name: "Pre-game clock" });
      expect(preGame.closest(".overflow-auto")).toHaveClass("min-h-0", "flex-1");
      expect(preGame).toBeInTheDocument();
      expect(screen.getByRole("group", { name: "Final clock" })).toBeInTheDocument();
      expect(screen.getAllByText(/Tokens: \{text\}, \{separator\}, \{dayofweek\}/)).toHaveLength(2);
      expect(screen.getByRole("textbox", { name: "Pre-game clock pattern" })).toHaveAttribute(
        "placeholder",
        PRE_GAME_CLOCK_DEFAULTS.pattern
      );
      expect(screen.getByRole("textbox", { name: "Final clock pattern" })).toHaveAttribute(
        "placeholder",
        FINAL_CLOCK_DEFAULTS.pattern
      );
    });

    it("saves final include weekday toggle", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="general" />);

      const group = await screen.findByRole("group", { name: "Final clock" });
      await user.click(within(group).getByRole("switch", { name: "Include weekday" }));

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].clockFormats.final.includeWeekday).toBe(false);
    });

    it("saves a custom final pattern", async () => {
      renderWithProviders(<SettingsTab section="general" />);

      const pattern = await screen.findByRole("textbox", { name: "Final clock pattern" });
      fireEvent.change(pattern, { target: { value: "{text} / {dayofweek}" } });

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].clockFormats.final.pattern).toBe("{text} / {dayofweek}");
    });
  });

  describe("sports", () => {
    it("appends a sport when Add Sport is clicked", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="sports" />);

      await screen.findByDisplayValue("Football");
      await user.click(screen.getByRole("button", { name: "Add Sport" }));

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].sports).toHaveLength(3);
      expect(savedSettings()[0].sports[2]?.name).toBe("New Sport");
    });

    it("saves when the Enabled checkbox is toggled", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="sports" />);

      const enabled = await screen.findAllByRole("checkbox", { name: "Enabled" });
      await user.click(enabled[0]);

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].sports[0].enabled).toBe(false);
    });

    it("shows the OOS inspector when OOS is enabled on the focused sport", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="sports" />);

      const oos = await screen.findAllByRole("checkbox", { name: "OOS" });
      await user.click(oos[0]);

      expect(await screen.findByRole("complementary", { name: "OOS inspector" })).toBeInTheDocument();
      expect(screen.getByLabelText("Path")).toBeInTheDocument();
      expect(screen.getByLabelText("File")).toBeInTheDocument();
      expect(screen.getByLabelText("Scores")).toBeInTheDocument();
      expect(screen.getByLabelText("Teams")).toBeInTheDocument();
    });

    it("filters sports from the search field", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="sports" />);

      await screen.findByDisplayValue("Football");
      await user.type(screen.getByPlaceholderText("Search sports…"), "Basket");

      expect(screen.getByDisplayValue("Basketball")).toBeInTheDocument();
      expect(screen.queryByDisplayValue("Football")).not.toBeInTheDocument();
      expect(screen.getByText("Showing 1 of 2 sports")).toBeInTheDocument();
    });

    it("removes a sport after ConfirmDialog confirm", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="sports" />);

      await screen.findByDisplayValue("Football");
      await user.click(screen.getByRole("button", { name: "Remove Football" }));

      const dialog = screen.getByRole("dialog");
      expect(within(dialog).getByRole("heading", { name: "Remove sport 'Football'?" })).toBeInTheDocument();
      expect(savedSettings()).toHaveLength(0);

      await user.click(within(dialog).getByRole("button", { name: "Remove" }));

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].sports.map((sport) => sport.name)).toEqual(["Basketball"]);
    });

    it("does not save when GhostInput is cancelled with Escape", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="sports" />);

      const name = await screen.findByDisplayValue("Football");
      await user.click(name);
      await user.clear(name);
      await user.type(name, "Soccer{Escape}");

      expect(name).toHaveValue("Football");
      expect(savedSettings()).toHaveLength(0);
    });

    it("does not mention double-click and shows the auto-save caption", async () => {
      renderWithProviders(<SettingsTab section="sports" />);

      expect(await screen.findByText("Changes save automatically.")).toBeInTheDocument();
      expect(screen.queryByText(/double-click/i)).not.toBeInTheDocument();
      expect(screen.queryByText("▲")).not.toBeInTheDocument();
    });
  });

  describe("display teams", () => {
    it("does not add a duplicate display team", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="display-teams" />);

      await screen.findByText("Duke");
      await user.click(screen.getByRole("combobox"));
      await user.click(screen.getByRole("option", { name: "Duke" }));
      await user.click(screen.getByRole("button", { name: "Add" }));

      expect(savedSettings()).toHaveLength(0);
      expect(screen.getAllByText("Duke")).toHaveLength(1);
    });

    it("saves when a display team is removed", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="display-teams" />);

      await screen.findByText("Duke");
      await user.click(screen.getByRole("button", { name: "Remove Duke" }));

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].displayTeams).toEqual([]);
    });
  });

  describe("xml", () => {
    it("toggles xmlToJson.enabled with the switch", async () => {
      const user = userEvent.setup();
      renderWithProviders(<SettingsTab section="xml" />);

      const enabled = await screen.findByRole("switch", { name: "Enabled" });
      await user.click(enabled);

      await waitFor(() => {
        expect(savedSettings()).toHaveLength(1);
      });
      expect(savedSettings()[0].xmlToJson.enabled).toBe(true);
    });

    it("debounces path typing for 300ms and saves once", async () => {
      renderWithProviders(<SettingsTab section="xml" />);
      const input = await screen.findByLabelText("Path");

      vi.useFakeTimers();
      fireEvent.change(input, { target: { value: "/tmp/games.xmla" } });
      fireEvent.change(input, { target: { value: "/tmp/games.xmlab" } });
      fireEvent.change(input, { target: { value: "/tmp/games.xmlabc" } });

      expect(savedSettings()).toHaveLength(0);

      await act(async () => {
        vi.advanceTimersByTime(299);
      });
      expect(savedSettings()).toHaveLength(0);

      await act(async () => {
        vi.advanceTimersByTime(1);
      });

      expect(savedSettings()).toHaveLength(1);
      expect(savedSettings()[0].xmlToJson.filePaths).toEqual(["/tmp/games.xmlabc"]);
    });
  });

  it("toasts save errors instead of alerting", async () => {
    sendMessage.mockImplementation(async (method: string, params?: unknown) => {
      if (method === "getSettings") return baseSettings;
      if (method === "saveSettings") throw new Error("disk full");
      if (method === "getTeams") return teams;
      if (method === "getConferences") return [];
      return params ?? null;
    });
    const alertSpy = vi.spyOn(window, "alert").mockImplementation(() => {});
    const user = userEvent.setup();
    renderWithProviders(<SettingsTab section="sports" />);

    const enabled = await screen.findAllByRole("checkbox", { name: "Enabled" });
    await user.click(enabled[0]);

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Error saving settings: disk full");
    });
    expect(alertSpy).not.toHaveBeenCalled();
  });
});
