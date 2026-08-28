import { mockSend } from "./devMock";
import type { BridgeResponse, PhotinoExternal } from "./types";

type Pending = {
  resolve: (value: unknown) => void;
  reject: (error: Error) => void;
};

const pending = new Map<string, Pending>();
let nextId = 1;
let listening = false;

function getPhotino(): PhotinoExternal | undefined {
  const external = window.external as unknown as PhotinoExternal;
  if (typeof external?.sendMessage !== "function") return undefined;
  return external;
}

function ensureListener(): void {
  if (listening) return;
  const external = getPhotino();
  if (!external?.receiveMessage) return;
  listening = true;
  external.receiveMessage((raw: string) => {
    let response: BridgeResponse;
    try {
      response = JSON.parse(raw) as BridgeResponse;
    } catch {
      return;
    }
    if (!response.id) return;
    const waiter = pending.get(response.id);
    if (!waiter) return;
    pending.delete(response.id);
    if (response.error) waiter.reject(new Error(response.error));
    else waiter.resolve(response.result);
  });
}

export function sendMessage<T>(method: string, params?: unknown): Promise<T> {
  ensureListener();
  const external = getPhotino();
  if (!external) {
    if (import.meta.env.DEV) return mockSend<T>(method, params);
    return Promise.reject(new Error("Photino bridge is not available"));
  }

  const id = String(nextId++);
  return new Promise<T>((resolve, reject) => {
    pending.set(id, {
      resolve: (value) => resolve(value as T),
      reject,
    });
    external.sendMessage(JSON.stringify({ id, method, params }));
  });
}
