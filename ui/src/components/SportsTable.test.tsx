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
  lookBack: 0,
  lookForward: 0,
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

  it("closes a conference dropdown when another cell is selected", async () => {
    const user = userEvent.setup();
    const other: SportSnapshot = { ...sport, name: "Basketball", conferenceName: "Big Ten" };
    renderWithProviders(
      <SportsTable
        rows={[
          { sport, index: 0 },
          { sport: other, index: 1 },
        ]}
        sort={null}
        onSort={vi.fn()}
        focusedIndex={null}
        onFocus={vi.fn()}
        conferenceOptions={[
          { display: "SEC", value: "SEC" },
          { display: "Big Ten", value: "Big Ten" },
          { display: "ACC", value: "ACC" },
        ]}
        displayModes={displayModes}
        onPatchSport={vi.fn()}
        onPatchLists={vi.fn()}
        onPatchOos={vi.fn()}
        onRemove={vi.fn()}
      />,
    );

    const openButtons = screen.getAllByRole("button", { name: "Open" });
    await user.click(openButtons[0]);
    expect(screen.getByRole("listbox")).toBeInTheDocument();

    await user.click(screen.getByDisplayValue("Basketball"));
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });

  it("closes a display mode dropdown when another cell is selected", async () => {
    const user = userEvent.setup();
    const other: SportSnapshot = {
      ...sport,
      name: "Basketball",
      conferenceName: "Big Ten",
      gameDisplayMode: "All",
    };
    renderWithProviders(
      <SportsTable
        rows={[
          { sport, index: 0 },
          { sport: other, index: 1 },
        ]}
        sort={null}
        onSort={vi.fn()}
        focusedIndex={null}
        onFocus={vi.fn()}
        conferenceOptions={[
          { display: "SEC", value: "SEC" },
          { display: "Big Ten", value: "Big Ten" },
        ]}
        displayModes={displayModes}
        onPatchSport={vi.fn()}
        onPatchLists={vi.fn()}
        onPatchOos={vi.fn()}
        onRemove={vi.fn()}
      />,
    );

    const combos = screen.getAllByRole("combobox");
    await user.click(screen.getAllByRole("button", { name: "Open" })[1]);
    expect(combos[1]).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("listbox")).toBeInTheDocument();

    await user.click(combos[3]);
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(combos[1]).toHaveAttribute("aria-expanded", "false");
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

  it("labels look back and look forward as weeks when week is set", () => {
    renderWithProviders(
      <SportsTable
        rows={[{ sport, index: 0 }]}
        sort={null}
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

    expect(screen.getByRole("button", { name: "Look Back" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Look Forward" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Look back (weeks)" })).toHaveValue("0");
    expect(screen.getByRole("textbox", { name: "Look forward (weeks)" })).toHaveValue("0");
  });

  it("labels look back and look forward as days when week is null", () => {
    renderWithProviders(
      <SportsTable
        rows={[{ sport: { ...sport, week: null }, index: 0 }]}
        sort={null}
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

    expect(screen.getByRole("textbox", { name: "Look back (days)" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Look forward (days)" })).toBeInTheDocument();
  });

  it("saves look back and look forward integers and treats empty as 0", async () => {
    const user = userEvent.setup();
    const onPatchSport = vi.fn();
    renderWithProviders(
      <>
        <SportsTable
          rows={[{ sport: { ...sport, lookBack: 2, lookForward: 3 }, index: 0 }]}
          sort={null}
          onSort={vi.fn()}
          focusedIndex={null}
          onFocus={vi.fn()}
          conferenceOptions={[]}
          displayModes={displayModes}
          onPatchSport={onPatchSport}
          onPatchLists={vi.fn()}
          onPatchOos={vi.fn()}
          onRemove={vi.fn()}
        />
        <button type="button">outside</button>
      </>,
    );

    const lookBack = screen.getByRole("textbox", { name: "Look back (weeks)" });
    await user.clear(lookBack);
    await user.type(lookBack, "4{Enter}");
    expect(onPatchSport).toHaveBeenCalledWith(0, { lookBack: 4 });

    onPatchSport.mockClear();
    const lookForward = screen.getByRole("textbox", { name: "Look forward (weeks)" });
    await user.clear(lookForward);
    await user.type(lookForward, "{Enter}");
    await user.click(screen.getByRole("button", { name: "outside" }));
    expect(onPatchSport).toHaveBeenCalledWith(0, { lookForward: 0 });
  });

  it("rejects negative look back and look forward without saving", async () => {
    const user = userEvent.setup();
    const onPatchSport = vi.fn();
    renderWithProviders(
      <>
        <SportsTable
          rows={[{ sport, index: 0 }]}
          sort={null}
          onSort={vi.fn()}
          focusedIndex={null}
          onFocus={vi.fn()}
          conferenceOptions={[]}
          displayModes={displayModes}
          onPatchSport={onPatchSport}
          onPatchLists={vi.fn()}
          onPatchOos={vi.fn()}
          onRemove={vi.fn()}
        />
        <button type="button">outside</button>
      </>,
    );

    const lookBack = screen.getByRole("textbox", { name: "Look back (weeks)" });
    await user.clear(lookBack);
    await user.type(lookBack, "-2");
    await user.click(screen.getByRole("button", { name: "outside" }));
    expect(onPatchSport).not.toHaveBeenCalled();
    expect(lookBack).toHaveValue("0");
  });
});
