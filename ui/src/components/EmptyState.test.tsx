import { afterEach, describe, expect, it } from "vitest";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import EmptyState from "./EmptyState";

afterEach(() => {
  cleanup();
});

describe("EmptyState", () => {
  it("renders a muted caption and optional title", () => {
    renderWithProviders(<EmptyState title="No teams">Add a team to get started.</EmptyState>);

    expect(screen.getByText("No teams")).toBeInTheDocument();
    expect(screen.getByText("Add a team to get started.")).toBeInTheDocument();
  });
});
