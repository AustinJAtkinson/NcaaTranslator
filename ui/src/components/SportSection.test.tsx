import { afterEach, describe, expect, it } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import { mockLiveGame } from "@/devMock";
import type { GameSnapshot, PeriodSnapshot, SportScoreboardSnapshot } from "@/types";
import SportSection from "./SportSection";

afterEach(() => {
  cleanup();
});

function game(home: string): GameSnapshot {
  return { ...mockLiveGame, home };
}

function period(
  partial: Partial<PeriodSnapshot> & { games: GameSnapshot[]; dateRange?: string | null },
): PeriodSnapshot {
  return {
    confGamesCount: 0,
    nonConfGamesCount: 0,
    displayGamesCount: 0,
    homeGamesCount: 0,
    dateRange: partial.dateRange ?? null,
    ...partial,
  };
}

function sport(partial: Partial<SportScoreboardSnapshot> = {}): SportScoreboardSnapshot {
  const current = partial.current ?? period({ games: [game("Current Home")], dateRange: "Sep 1", confGamesCount: 2, nonConfGamesCount: 1, displayGamesCount: 1, homeGamesCount: 1 });
  return {
    sportName: "Volleyball",
    gameDisplayMode: "Live",
    confGamesCount: current.confGamesCount,
    nonConfGamesCount: current.nonConfGamesCount,
    displayGamesCount: current.displayGamesCount,
    homeGamesCount: current.homeGamesCount,
    games: current.games,
    week: null,
    lookBack: 0,
    lookForward: 0,
    current,
    prev: null,
    post: null,
    ...partial,
  };
}

describe("SportSection", () => {
  it("hides Prev and Post when those snapshots are null", () => {
    renderWithProviders(<SportSection sport={sport()} />);

    expect(screen.getByRole("button", { name: "Current" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Prev" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Post" })).not.toBeInTheDocument();
  });

  it("shows Prev and Post when those snapshots are present", () => {
    renderWithProviders(
      <SportSection
        sport={sport({
          prev: period({ games: [game("Prev Home")], dateRange: "Aug 27–29" }),
          post: period({ games: [game("Post Home")], dateRange: "Sep 4" }),
        })}
      />,
    );

    expect(screen.getByRole("button", { name: "Current" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Prev" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Post" })).toBeInTheDocument();
  });

  it("defaults to Current games, counts, and date range", () => {
    renderWithProviders(
      <SportSection
        sport={sport({
          prev: period({ games: [game("Prev Home")], dateRange: "Aug 27–29", confGamesCount: 9 }),
          post: period({ games: [game("Post Home")], dateRange: "Sep 4" }),
        })}
      />,
    );

    expect(screen.getByRole("button", { name: "Current" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: "Prev" })).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByText("Current Home")).toBeInTheDocument();
    expect(screen.queryByText("Prev Home")).not.toBeInTheDocument();
    expect(screen.getByText("Sep 1")).toBeInTheDocument();
    expect(screen.getByText("Conf 2")).toBeInTheDocument();
    expect(screen.getByText("Non-conf 1")).toBeInTheDocument();
    expect(screen.getByText("Display 1")).toBeInTheDocument();
    expect(screen.getByText("Home 1")).toBeInTheDocument();
  });

  it("switches games and count badges with Current | Prev | Post", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <SportSection
        sport={sport({
          prev: period({
            games: [game("Prev Home")],
            dateRange: "Aug 27–29",
            confGamesCount: 4,
            nonConfGamesCount: 3,
            displayGamesCount: 2,
            homeGamesCount: 0,
          }),
          post: period({
            games: [game("Post Home")],
            dateRange: "Sep 4",
            confGamesCount: 1,
            nonConfGamesCount: 0,
            displayGamesCount: 5,
            homeGamesCount: 8,
          }),
        })}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Prev" }));

    expect(screen.getByRole("button", { name: "Prev" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByText("Prev Home")).toBeInTheDocument();
    expect(screen.queryByText("Current Home")).not.toBeInTheDocument();
    expect(screen.getByText("Aug 27–29")).toBeInTheDocument();
    expect(screen.getByText("Conf 4")).toBeInTheDocument();
    expect(screen.getByText("Non-conf 3")).toBeInTheDocument();
    expect(screen.getByText("Display 2")).toBeInTheDocument();
    expect(screen.getByText("Home 0")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Post" }));

    expect(screen.getByText("Post Home")).toBeInTheDocument();
    expect(screen.queryByText("Prev Home")).not.toBeInTheDocument();
    expect(screen.getByText("Sep 4")).toBeInTheDocument();
    expect(screen.getByText("Conf 1")).toBeInTheDocument();
    expect(screen.getByText("Home 8")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Current" }));

    expect(screen.getByText("Current Home")).toBeInTheDocument();
    expect(screen.getByText("Conf 2")).toBeInTheDocument();
  });

  it("does not collapse the expander when a period is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <SportSection
        sport={sport({
          prev: period({ games: [game("Prev Home")], dateRange: "Aug 27–29" }),
        })}
      />,
    );

    const expander = screen.getByRole("button", { name: "Volleyball" });
    expect(expander).toHaveAttribute("aria-expanded", "true");

    await user.click(screen.getByRole("button", { name: "Prev" }));

    expect(expander).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText("Prev Home")).toBeInTheDocument();

    await user.click(expander);

    expect(expander).toHaveAttribute("aria-expanded", "false");
    const panel = document.getElementById(expander.getAttribute("aria-controls") ?? "");
    expect(panel).toHaveAttribute("hidden");
  });

  it("uses distinct empty copy for current, prev, and post", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <SportSection
        sport={sport({
          current: period({ games: [], dateRange: "Sep 1" }),
          games: [],
          confGamesCount: 0,
          nonConfGamesCount: 0,
          displayGamesCount: 0,
          homeGamesCount: 0,
          prev: period({ games: [], dateRange: "Aug 27–29" }),
          post: period({ games: [], dateRange: "Sep 4" }),
        })}
      />,
    );

    expect(screen.getByText("No games.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Prev" }));
    expect(screen.getByText("No previous games.")).toBeInTheDocument();
    expect(screen.queryByText("No games.")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Post" }));
    expect(screen.getByText("No upcoming games.")).toBeInTheDocument();
    expect(screen.queryByText("No previous games.")).not.toBeInTheDocument();
  });

  it("shows Week n for week sports", () => {
    renderWithProviders(<SportSection sport={sport({ week: 2 })} />);

    expect(screen.getByText("Week 2")).toBeInTheDocument();
  });

  it("keeps Prev selected when current games change on rerender", async () => {
    const user = userEvent.setup();
    const prev = period({ games: [game("Prev Home")], dateRange: "Aug 27–29" });
    const { rerender } = renderWithProviders(
      <SportSection
        sport={sport({
          prev,
          current: period({ games: [game("Current A")], dateRange: "Sep 1" }),
        })}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Prev" }));
    expect(screen.getByText("Prev Home")).toBeInTheDocument();

    rerender(
      <SportSection
        sport={sport({
          prev,
          current: period({ games: [game("Current B")], dateRange: "Sep 2" }),
          games: [game("Current B")],
        })}
      />,
    );

    expect(screen.getByText("Prev Home")).toBeInTheDocument();
    expect(screen.queryByText("Current B")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Prev" })).toHaveAttribute("aria-pressed", "true");
  });

  it("resets to Current when the selected period snapshot becomes null", async () => {
    const user = userEvent.setup();
    const prev = period({ games: [game("Prev Home")], dateRange: "Aug 27–29" });
    const { rerender } = renderWithProviders(<SportSection sport={sport({ prev })} />);

    await user.click(screen.getByRole("button", { name: "Prev" }));
    expect(screen.getByText("Prev Home")).toBeInTheDocument();

    rerender(<SportSection sport={sport({ prev: null })} />);

    expect(screen.queryByRole("button", { name: "Prev" })).not.toBeInTheDocument();
    expect(screen.getByText("Current Home")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Current" })).toHaveAttribute("aria-pressed", "true");

    rerender(<SportSection sport={sport({ prev })} />);

    expect(screen.getByRole("button", { name: "Prev" })).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByText("Current Home")).toBeInTheDocument();
    expect(screen.queryByText("Prev Home")).not.toBeInTheDocument();
  });

  it("falls back to top-level games and counts when current is missing", () => {
    renderWithProviders(
      <SportSection
        sport={{
          sportName: "Volleyball",
          gameDisplayMode: "Live",
          confGamesCount: 2,
          nonConfGamesCount: 1,
          displayGamesCount: 1,
          homeGamesCount: 1,
          games: [game("Legacy Home")],
        }}
      />,
    );

    expect(screen.getByText("Legacy Home")).toBeInTheDocument();
    expect(screen.getByText("Conf 2")).toBeInTheDocument();
    expect(screen.queryByText("No games.")).not.toBeInTheDocument();
  });
});
