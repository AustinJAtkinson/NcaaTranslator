import { afterEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import OosInspector from "./OosInspector";
import type { OosUpdaterSnapshot } from "../types";

const oos: OosUpdaterSnapshot = {
  enabled: true,
  oosFilePath: "/tmp/oos",
  oosFileName: "scores.xml",
  numberOfOutScores: 4,
  numberOfTeamsPer: 2,
};

afterEach(() => {
  cleanup();
});

describe("OosInspector", () => {
  it("renders Path, File, Scores, and Teams and calls Browse", async () => {
    const user = userEvent.setup();
    const onPatch = vi.fn();
    const onBrowse = vi.fn();
    renderWithProviders(<OosInspector oos={oos} onPatch={onPatch} onBrowse={onBrowse} />);

    expect(screen.getByRole("complementary", { name: "OOS inspector" })).toBeInTheDocument();
    expect(screen.getByLabelText("Path")).toHaveValue("/tmp/oos");
    expect(screen.getByLabelText("File")).toHaveValue("scores.xml");
    expect(screen.getByLabelText("Scores")).toHaveValue("4");
    expect(screen.getByLabelText("Teams")).toHaveValue("2");

    await user.click(screen.getByRole("button", { name: "Browse" }));
    expect(onBrowse).toHaveBeenCalledTimes(1);
  });

  it("commits a path change", async () => {
    const user = userEvent.setup();
    const onPatch = vi.fn();
    renderWithProviders(<OosInspector oos={oos} onPatch={onPatch} onBrowse={vi.fn()} />);

    const path = screen.getByLabelText("Path");
    await user.click(path);
    await user.clear(path);
    await user.type(path, "/other{Enter}");

    expect(onPatch).toHaveBeenCalledWith({ oosFilePath: "/other" });
  });
});
