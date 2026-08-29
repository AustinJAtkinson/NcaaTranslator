import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { toast } from "sonner";
import { cleanup, renderWithProviders, screen, waitFor, within } from "@/test/render";
import { resetBridgeMock, sendMessage } from "./test/bridgeMock";
import NamesTab from "./NamesTab";
import type { ConferenceNameSnapshot, TeamNameSnapshot } from "./types";

vi.mock("./bridge", () => import("./test/bridgeMock"));

vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
  },
}));

const teams: TeamNameSnapshot[] = [
  { name6Char: "ZZZ", customName: "Zulu", seoname: "zulu-seo", nameShort: "Zu" },
  { name6Char: "AAA", customName: "Alpha", seoname: "alpha-seo", nameShort: "Al" },
];

const conferences: ConferenceNameSnapshot[] = [
  { conferenceSeo: "zzz-conf", customConferenceName: "Zeta Conference" },
  { conferenceSeo: "aaa-conf", customConferenceName: "Alpha Conference" },
];

function mockBridge(): void {
  sendMessage.mockImplementation(async (method: string, params?: unknown) => {
    switch (method) {
      case "getTeams":
        return teams.map((team) => ({ ...team }));
      case "getConferences":
        return conferences.map((conference) => ({ ...conference }));
      case "saveTeamCustomName": {
        const body = params as { name6Char: string; customName: string };
        const team = teams.find((item) => item.name6Char === body.name6Char);
        return { ...team, customName: body.customName };
      }
      case "saveConferenceCustomName": {
        const body = params as { conferenceSeo: string; customConferenceName: string };
        const conference = conferences.find((item) => item.conferenceSeo === body.conferenceSeo);
        return { ...conference, customConferenceName: body.customConferenceName };
      }
      default:
        return null;
    }
  });
}

describe("NamesTab", () => {
  beforeEach(() => {
    resetBridgeMock();
    mockBridge();
    vi.mocked(toast.error).mockClear();
  });

  afterEach(() => {
    cleanup();
  });

  it("renders list rows and not a data-grid table", async () => {
    renderWithProviders(<NamesTab />);

    const list = await screen.findByRole("list", { name: "Teams" });
    const rows = within(list).getAllByRole("listitem");
    expect(rows).toHaveLength(2);
    expect(rows[0]).toHaveTextContent("ZZZ");
    expect(within(rows[0]).getByRole("textbox")).toHaveValue("Zulu");
    expect(rows[0]).toHaveTextContent("zulu-seo");
    expect(rows[0]).toHaveTextContent("Zu");
    expect(rows[1]).toHaveTextContent("AAA");
    expect(within(rows[1]).getByRole("textbox")).toHaveValue("Alpha");

    expect(document.querySelector("table.data-grid")).toBeNull();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
    expect(screen.queryByRole("tablist")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Teams" })).toBeInTheDocument();
    expect(screen.getByText("Changes save automatically.")).toBeInTheDocument();
  });

  it("shows only the conferences list when section is conferences", async () => {
    renderWithProviders(<NamesTab section="conferences" />);

    expect(await screen.findByRole("heading", { name: "Conferences" })).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Search conferences…")).toBeInTheDocument();
    expect(screen.getByRole("list", { name: "Conferences" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Teams" })).not.toBeInTheDocument();
    expect(screen.queryByPlaceholderText("Search teams…")).not.toBeInTheDocument();
    expect(screen.queryByRole("tablist")).not.toBeInTheDocument();
  });

  it("filters teams with search and shows a result count", async () => {
    const user = userEvent.setup();
    renderWithProviders(<NamesTab />);

    await screen.findByRole("list", { name: "Teams" });
    await user.type(screen.getByPlaceholderText("Search teams…"), "Alpha");

    const rows = screen.getAllByRole("listitem");
    expect(rows).toHaveLength(1);
    expect(rows[0]).toHaveTextContent("AAA");
    expect(screen.queryByText("ZZZ")).not.toBeInTheDocument();
    expect(screen.getByText("1 of 2")).toBeInTheDocument();
  });

  it("shows an empty state when no teams match", async () => {
    const user = userEvent.setup();
    renderWithProviders(<NamesTab />);

    await screen.findByRole("list", { name: "Teams" });
    await user.type(screen.getByPlaceholderText("Search teams…"), "no-such-team");

    expect(screen.getByText("No teams match.")).toBeInTheDocument();
    expect(screen.queryByRole("listitem")).not.toBeInTheDocument();
  });

  it("commits a GhostInput display name via saveTeamCustomName", async () => {
    const user = userEvent.setup();
    renderWithProviders(<NamesTab />);

    const input = await screen.findByLabelText("Display name for AAA");
    await user.click(input);
    await user.clear(input);
    await user.type(input, "Blue Devils{Enter}");

    await waitFor(() => {
      expect(sendMessage).toHaveBeenCalledWith("saveTeamCustomName", {
        name6Char: "AAA",
        customName: "Blue Devils",
      });
    });
  });

  it("commits a conference GhostInput via saveConferenceCustomName", async () => {
    const user = userEvent.setup();
    renderWithProviders(<NamesTab section="conferences" />);

    const input = await screen.findByLabelText("Custom name for aaa-conf");
    await user.click(input);
    await user.clear(input);
    await user.type(input, "Atlantic{Enter}");

    await waitFor(() => {
      expect(sendMessage).toHaveBeenCalledWith("saveConferenceCustomName", {
        conferenceSeo: "aaa-conf",
        customConferenceName: "Atlantic",
      });
    });
  });

  it("toasts and reverts an empty display name without window.alert", async () => {
    const user = userEvent.setup();
    const alertSpy = vi.spyOn(window, "alert").mockImplementation(() => {});
    renderWithProviders(<NamesTab />);

    const input = await screen.findByLabelText("Display name for AAA");
    await user.click(input);
    await user.clear(input);
    await user.tab();

    expect(toast.error).toHaveBeenCalledWith("Display name cannot be empty.");
    expect(alertSpy).not.toHaveBeenCalled();
    expect(sendMessage).not.toHaveBeenCalledWith("saveTeamCustomName", expect.anything());
    expect(await screen.findByLabelText("Display name for AAA")).toHaveValue("Alpha");

    alertSpy.mockRestore();
  });

  it("toasts and reverts an empty conference name", async () => {
    const user = userEvent.setup();
    renderWithProviders(<NamesTab section="conferences" />);

    const input = await screen.findByLabelText("Custom name for aaa-conf");
    await user.click(input);
    await user.clear(input);
    await user.tab();

    expect(toast.error).toHaveBeenCalledWith("Custom name cannot be empty.");
    expect(sendMessage).not.toHaveBeenCalledWith("saveConferenceCustomName", expect.anything());
    expect(await screen.findByLabelText("Custom name for aaa-conf")).toHaveValue("Alpha Conference");
  });

  it("sorts by column labels using the same compare logic", async () => {
    const user = userEvent.setup();
    renderWithProviders(<NamesTab />);

    const list = await screen.findByRole("list", { name: "Teams" });
    let rows = within(list).getAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("ZZZ");
    expect(rows[1]).toHaveTextContent("AAA");

    await user.click(screen.getByRole("button", { name: "Code" }));
    rows = within(list).getAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("AAA");
    expect(rows[1]).toHaveTextContent("ZZZ");

    await user.click(screen.getByRole("button", { name: "Code" }));
    rows = within(list).getAllByRole("listitem");
    expect(rows[0]).toHaveTextContent("ZZZ");
    expect(rows[1]).toHaveTextContent("AAA");

    await user.click(screen.getByRole("button", { name: "Display" }));
    rows = within(list).getAllByRole("listitem");
    expect(within(rows[0]).getByRole("textbox")).toHaveValue("Alpha");
    expect(within(rows[1]).getByRole("textbox")).toHaveValue("Zulu");
  });

  it("does not render SEO or short names as textboxes", async () => {
    renderWithProviders(<NamesTab />);

    await screen.findByRole("list", { name: "Teams" });

    expect(screen.getByText("zulu-seo")).toBeInTheDocument();
    expect(screen.getByText("Zu")).toBeInTheDocument();
    expect(screen.getByText("alpha-seo")).toBeInTheDocument();
    expect(screen.getByText("Al")).toBeInTheDocument();

    const textboxes = screen.getAllByRole("textbox");
    expect(textboxes).toHaveLength(3);
    for (const box of textboxes) {
      expect(box).not.toHaveValue("zulu-seo");
      expect(box).not.toHaveValue("alpha-seo");
      expect(box).not.toHaveValue("Zu");
      expect(box).not.toHaveValue("Al");
      expect(box).not.toHaveValue("ZZZ");
      expect(box).not.toHaveValue("AAA");
    }
  });
});
