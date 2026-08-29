import { afterEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import ComboBox from "./ComboBox";

afterEach(() => {
  cleanup();
});

const options = [
  { display: "Alpha", value: "a" },
  { display: "Beta", value: "b" },
  { display: "Gamma", value: "c" },
];

describe("ComboBox", () => {
  it("selects an option with ArrowDown and Enter", async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    renderWithProviders(<ComboBox value={null} options={options} onSelect={onSelect} />);

    await user.click(screen.getByRole("combobox"));
    expect(screen.getByRole("option", { name: "Alpha" })).toBeInTheDocument();

    await user.keyboard("{ArrowDown}{Enter}");

    expect(onSelect).toHaveBeenCalledWith("b");
  });

  it("closes on Escape", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ComboBox value={null} options={options} onSelect={vi.fn()} />);

    await user.click(screen.getByRole("combobox"));
    expect(screen.getByRole("listbox")).toBeInTheDocument();

    await user.keyboard("{Escape}");

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });

  it("hides non-matches and shows No matches when filterOnType is set", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <ComboBox value={null} options={options} filterOnType onSelect={vi.fn()} />,
    );

    const input = screen.getByRole("combobox");
    await user.click(input);
    await user.type(input, "zzz");

    expect(screen.queryByRole("option", { name: "Alpha" })).not.toBeInTheDocument();
    expect(screen.queryByRole("option", { name: "Beta" })).not.toBeInTheDocument();
    expect(screen.getByText("No matches")).toBeInTheDocument();
  });
});
