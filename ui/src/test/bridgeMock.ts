import { vi } from "vitest";

export const sendMessage = vi.fn();

export function resetBridgeMock(): void {
  sendMessage.mockReset();
}
