import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import { mockEmptySport, mockLiveGame } from "@/devMock";
import type { ScoreboardSnapshot, SportScoreboardSnapshot, StatusResult } from "./types";
import MainTab from "./MainTab";

afterEach(() => {
  cleanup();
});

const volleyball: SportScoreboardSnapshot = {
  sportName: "Volleyball",
  gameDisplayMode: "Live",
  confGamesCount: 2,
  nonConfGamesCount: 1,
  displayGamesCount: 1,
  homeGamesCount: 1,
  games: [mockLiveGame],
};

const populatedBoard: ScoreboardSnapshot = {
  sports: [volleyball, mockEmptySport],
};

function renderMain(
  status: StatusResult,
  board: ScoreboardSnapshot = populatedBoard,
) {
  return renderWithProviders(
    <MainTab status={status} board={board} onStart={vi.fn()} onStop={vi.fn()} />,
  );
}

describe("MainTab", () => {
  it("disables Start when running and Stop when stopped", () => {
    const { rerender } = renderMain({ running: true, lastUpdate: "12:00:00.000" });

    expect(screen.getByRole("button", { name: "Start" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Stop" })).toBeEnabled();

    rerender(
      <MainTab
        status={{ running: false, lastUpdate: "12:00:00.000" }}
        board={populatedBoard}
        onStart={vi.fn()}
        onStop={vi.fn()}
      />,
    );

    expect(screen.getByRole("button", { name: "Start" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Stop" })).toBeDisabled();
  });

  it("shows running status and last update", () => {
    renderMain({ running: true, lastUpdate: "12:00:00.000" });

    expect(screen.getByText(/Running/)).toBeInTheDocument();
    expect(screen.getByText(/Last update 12:00:00.000/)).toBeInTheDocument();
  });

  it("shows stopped status and Never when there is no last update", () => {
    renderMain({ running: false, lastUpdate: null }, { sports: [] });

    expect(screen.getByText(/Stopped/)).toBeInTheDocument();
    expect(screen.getByText(/Last update Never/)).toBeInTheDocument();
  });

  it("shows No games for an empty sport", () => {
    renderMain({ running: true, lastUpdate: null });

    expect(screen.getByText("No games.")).toBeInTheDocument();
  });

  it("shows an empty board message when no sports are enabled", () => {
    renderMain({ running: false, lastUpdate: null }, { sports: [] });

    expect(screen.getByText("No sports enabled. Turn them on in Settings.")).toBeInTheDocument();
  });

  it("renders count chips for a sport", () => {
    renderMain({ running: true, lastUpdate: null });

    expect(screen.getByText("Conf 2")).toBeInTheDocument();
    expect(screen.getByText("Non-conf 1")).toBeInTheDocument();
    expect(screen.getByText("Display 1")).toBeInTheDocument();
    expect(screen.getByText("Home 1")).toBeInTheDocument();
  });

  it("lays out games in a two-column auto grid", () => {
    renderMain({ running: true, lastUpdate: null });

    const grid = document.querySelector("[data-game-grid]");
    expect(grid).toHaveClass("game-grid");
    expect(grid?.parentElement).toHaveClass("game-grid-host");
  });
});
