import { afterEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import GhostInput from "./GhostInput";

afterEach(() => {
  cleanup();
});

describe("GhostInput", () => {
  it("focuses on click", async () => {
    const user = userEvent.setup();
    renderWithProviders(<GhostInput value="Duke" onCommit={vi.fn()} />);

    const input = screen.getByRole("textbox");
    await user.click(input);

    expect(input).toHaveFocus();
  });

  it("commits on Enter", async () => {
    const user = userEvent.setup();
    const onCommit = vi.fn();
    renderWithProviders(<GhostInput value="Duke" onCommit={onCommit} />);

    const input = screen.getByRole("textbox");
    await user.click(input);
    await user.clear(input);
    await user.type(input, "UNC{Enter}");

    expect(onCommit).toHaveBeenCalledWith("UNC");
  });

  it("reverts on Escape and does not commit", async () => {
    const user = userEvent.setup();
    const onCommit = vi.fn();
    const onCancel = vi.fn();
    renderWithProviders(<GhostInput value="Duke" onCommit={onCommit} onCancel={onCancel} />);

    const input = screen.getByRole("textbox");
    await user.click(input);
    await user.clear(input);
    await user.type(input, "Wake{Escape}");

    expect(onCommit).not.toHaveBeenCalled();
    expect(onCancel).toHaveBeenCalled();
    expect(input).toHaveValue("Duke");
    expect(input).not.toHaveFocus();
  });

  it("commits on blur", async () => {
    const user = userEvent.setup();
    const onCommit = vi.fn();
    renderWithProviders(
      <>
        <GhostInput value="Duke" onCommit={onCommit} />
        <button type="button">outside</button>
      </>,
    );

    const input = screen.getByRole("textbox");
    await user.click(input);
    await user.clear(input);
    await user.type(input, "State");
    await user.click(screen.getByRole("button", { name: "outside" }));

    expect(onCommit).toHaveBeenCalledWith("State");
  });

  it("restores the previous value when onCommit does not change it", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <>
        <GhostInput value="1" onCommit={() => undefined} />
        <button type="button">outside</button>
      </>,
    );

    const input = screen.getByRole("textbox");
    await user.click(input);
    await user.clear(input);
    await user.type(input, "nope");
    await user.click(screen.getByRole("button", { name: "outside" }));

    expect(input).toHaveValue("1");
  });

  it("has no onDoubleClick handler", async () => {
    const user = userEvent.setup();
    const onCommit = vi.fn();
    const onCancel = vi.fn();
    renderWithProviders(<GhostInput value="Duke" onCommit={onCommit} onCancel={onCancel} />);

    const input = screen.getByRole("textbox");
    await user.dblClick(input);

    expect(input).toHaveFocus();
    expect(onCommit).not.toHaveBeenCalled();
    expect(onCancel).not.toHaveBeenCalled();
    expect(input.ondblclick).toBeNull();
  });
});
