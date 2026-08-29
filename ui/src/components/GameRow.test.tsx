import { afterEach, describe, expect, it } from "vitest";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import { mockFinalGame, mockLiveGame, mockUpcomingGame } from "@/devMock";
import type { GameSnapshot } from "@/types";
import GameRow from "./GameRow";

afterEach(() => {
  cleanup();
});

describe("GameRow", () => {
  it("styles the live clock pill and preserves clock spaces", () => {
    renderWithProviders(<GameRow game={mockLiveGame} />);

    const clock = screen.getByText((_, node) => node?.textContent === "2nd     8:00");
    expect(clock).toHaveClass("whitespace-pre");
    expect(clock).toHaveClass("text-live");
  });

  it("styles the final clock pill as muted", () => {
    renderWithProviders(<GameRow game={mockFinalGame} />);

    const clock = screen.getByText("Final");
    expect(clock).toHaveClass("whitespace-pre");
    expect(clock).toHaveClass("text-muted-foreground");
    expect(clock).not.toHaveClass("text-live");
  });

  it("styles the upcoming clock pill as muted and keeps pregame spaces", () => {
    renderWithProviders(<GameRow game={mockUpcomingGame} />);

    const clock = screen.getByText("Fri. 5:00 PM");
    expect(clock).toHaveClass("whitespace-pre");
    expect(clock).toHaveClass("text-muted-foreground");
    expect(clock).not.toHaveClass("text-live");
  });

  it("emphasizes the leading score and mutes the trailer", () => {
    renderWithProviders(<GameRow game={mockLiveGame} />);

    expect(screen.getByText("14")).toHaveClass("text-foreground");
    expect(screen.getByText("7")).toHaveClass("text-muted-foreground");
  });

  it("keeps both scores foreground on a tie", () => {
    const tie: GameSnapshot = {
      home: "UND",
      homeScore: 10,
      away: "NDSU",
      awayScore: 10,
      displayClock: "2nd     8:00",
    };
    renderWithProviders(<GameRow game={tie} />);

    const scores = screen.getAllByText("10");
    expect(scores).toHaveLength(2);
    expect(scores[0]).toHaveClass("text-foreground");
    expect(scores[1]).toHaveClass("text-foreground");
  });

  it("mutes both scores when neither side leads", () => {
    renderWithProviders(<GameRow game={mockUpcomingGame} />);

    const dash = screen.getByText("—");
    const homeScore = dash.previousElementSibling;
    const awayScore = dash.nextElementSibling;
    expect(homeScore).toHaveClass("text-muted-foreground");
    expect(awayScore).toHaveClass("text-muted-foreground");
  });
});
