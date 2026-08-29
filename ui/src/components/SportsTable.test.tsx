import { afterEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import SportsTable from "./SportsTable";
import type { SportSnapshot } from "../types";

const sport: SportSnapshot = {
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

const displayModes = [
  { display: "Live", value: "Live" },
  { display: "All", value: "All" },
];

afterEach(() => {
  cleanup();
});

describe("SportsTable", () => {
  it("toggles Enabled and requests remove without using EditableCell or ▲", async () => {
    const user = userEvent.setup();
    const onPatchSport = vi.fn();
    const onRemove = vi.fn();
    const onSort = vi.fn();
    renderWithProviders(
      <SportsTable
        rows={[{ sport, index: 0 }]}
        sort={null}
        onSort={onSort}
        focusedIndex={null}
        onFocus={vi.fn()}
        conferenceOptions={[{ display: "SEC", value: "SEC" }]}
        displayModes={displayModes}
        onPatchSport={onPatchSport}
        onPatchLists={vi.fn()}
        onPatchOos={vi.fn()}
        onRemove={onRemove}
      />,
    );

    expect(screen.getByDisplayValue("Football")).toBeInTheDocument();
    expect(screen.queryByText("▲")).not.toBeInTheDocument();

    await user.click(screen.getByRole("checkbox", { name: "Enabled" }));
    expect(onPatchSport).toHaveBeenCalledWith(0, { enabled: false });

    await user.click(screen.getByRole("button", { name: "Name" }));
    expect(onSort).toHaveBeenCalledWith("name");

    await user.click(screen.getByRole("button", { name: "Remove Football" }));
    expect(onRemove).toHaveBeenCalledWith(0, "Football");
  });

  it("shows a lucide chevron on the active sort column", () => {
    const { container } = renderWithProviders(
      <SportsTable
        rows={[{ sport, index: 0 }]}
        sort={{ key: "name", dir: 1 }}
        onSort={vi.fn()}
        focusedIndex={null}
        onFocus={vi.fn()}
        conferenceOptions={[]}
        displayModes={displayModes}
        onPatchSport={vi.fn()}
        onPatchLists={vi.fn()}
        onPatchOos={vi.fn()}
        onRemove={vi.fn()}
      />,
    );

    expect(screen.queryByText("▲")).not.toBeInTheDocument();
    expect(container.querySelector("svg")).not.toBeNull();
  });
});
