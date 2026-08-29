import { useState } from "react";
import { afterEach, describe, expect, it } from "vitest";
import userEvent from "@testing-library/user-event";
import { cleanup, renderWithProviders, screen } from "@/test/render";
import SearchField from "./SearchField";

afterEach(() => {
  cleanup();
});

function SearchHarness() {
  const [value, setValue] = useState("");
  return <SearchField value={value} onChange={setValue} placeholder="Search teams" />;
}

describe("SearchField", () => {
  it("calls onChange while typing and uses the placeholder", async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchHarness />);

    const input = screen.getByPlaceholderText("Search teams");
    await user.type(input, "Duke");

    expect(input).toHaveValue("Duke");
  });
});
