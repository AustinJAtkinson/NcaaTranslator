import { afterEach, describe, expect, it, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import DisplayTeamList from "./DisplayTeamList";

afterEach(() => {
  cleanup();
});

describe("DisplayTeamList", () => {
  it("shows an empty state when there are no teams", () => {
    renderWithProviders(<DisplayTeamList teams={[]} onRemove={vi.fn()} />);

    expect(screen.getByRole("status")).toHaveTextContent(
      "No display teams. Add a team to include it in Display mode.",
    );
  });

  it("lists teams and removes one", async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    renderWithProviders(
      <DisplayTeamList teams={[{ ncaaTeamName: "Duke" }, { ncaaTeamName: "UNC" }]} onRemove={onRemove} />,
    );

    expect(screen.getByText("Duke")).toBeInTheDocument();
    expect(screen.getByText("UNC")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Remove Duke" }));
    expect(onRemove).toHaveBeenCalledWith(0);
  });
});
