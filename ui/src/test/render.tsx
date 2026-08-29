import { type ReactElement, type ReactNode } from "react";
import { render, type RenderOptions } from "@testing-library/react";

function Providers({ children }: { children: ReactNode }) {
  return children;
}

export function renderWithProviders(
  ui: ReactElement,
  options?: Omit<RenderOptions, "wrapper">,
) {
  return render(ui, { wrapper: Providers, ...options });
}

export * from "@testing-library/react";
