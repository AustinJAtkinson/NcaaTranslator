import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import Sidebar from "./Sidebar";

describe("Sidebar", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    delete document.documentElement.dataset.theme;
  });

  afterEach(() => {
    cleanup();
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    delete document.documentElement.dataset.theme;
  });

  it("selects Scoreboard, Settings, and Names via buttons", async () => {
    const user = userEvent.setup();
    const onNavigate = vi.fn();
    renderWithProviders(<Sidebar activeSection="main" onNavigate={onNavigate} />);

    await user.click(screen.getByRole("button", { name: "Scoreboard" }));
    expect(onNavigate).toHaveBeenCalledWith("main");

    await user.click(screen.getByRole("button", { name: "Settings" }));
    expect(onNavigate).toHaveBeenCalledWith("settings");

    await user.click(screen.getByRole("button", { name: "Names" }));
    expect(onNavigate).toHaveBeenCalledWith("names");
  });

  it("calls onNavigate to settings when a nested Settings item is clicked", async () => {
    const user = userEvent.setup();
    const onNavigate = vi.fn();
    renderWithProviders(<Sidebar activeSection="main" onNavigate={onNavigate} />);

    await user.click(screen.getByRole("button", { name: "General" }));
    expect(onNavigate).toHaveBeenCalledWith("settings-general");

    await user.click(screen.getByRole("button", { name: "Sports" }));
    expect(onNavigate).toHaveBeenCalledWith("settings-sports");

    await user.click(screen.getByRole("button", { name: "Display" }));
    expect(onNavigate).toHaveBeenCalledWith("settings-display");

    await user.click(screen.getByRole("button", { name: "XML" }));
    expect(onNavigate).toHaveBeenCalledWith("settings-xml");
  });

  it("toggles collapsed state from the collapse control", async () => {
    const user = userEvent.setup();
    renderWithProviders(<Sidebar activeSection="main" onNavigate={vi.fn()} />);

    const sidebar = screen.getByRole("complementary", { name: "Sidebar" });
    expect(sidebar).toHaveAttribute("data-collapsed", "false");
    expect(screen.getByText("NCAA Translator")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));

    expect(sidebar).toHaveAttribute("data-collapsed", "true");
    expect(localStorage.getItem("ncaa-sidebar-collapsed")).toBe("true");
    expect(screen.queryByText("NCAA Translator")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "General" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Expand sidebar" }));

    expect(sidebar).toHaveAttribute("data-collapsed", "false");
    expect(localStorage.getItem("ncaa-sidebar-collapsed")).toBe("false");
    expect(screen.getByRole("button", { name: "General" })).toBeInTheDocument();
  });
});
