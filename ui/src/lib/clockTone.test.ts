import { describe, expect, it } from "vitest";
import { clockTone, leadingSide } from "./clockTone";

describe("clockTone", () => {
  it("returns unknown for null", () => {
    expect(clockTone(null)).toBe("unknown");
  });

  it("returns unknown for undefined", () => {
    expect(clockTone(undefined)).toBe("unknown");
  });

  it("returns unknown for empty string", () => {
    expect(clockTone("")).toBe("unknown");
  });

  it("returns final when the clock contains Final", () => {
    expect(clockTone("Final")).toBe("final");
    expect(clockTone("final")).toBe("final");
    expect(clockTone("FINAL/OT")).toBe("final");
  });

  it("returns upcoming when the clock contains AM or PM", () => {
    expect(clockTone("7:00 PM")).toBe("upcoming");
    expect(clockTone("Fri. 5:00 PM")).toBe("upcoming");
    expect(clockTone("11:00 AM")).toBe("upcoming");
  });

  it("returns live for an in-progress clock", () => {
    expect(clockTone("12:34")).toBe("live");
    expect(clockTone("Q2 4:21")).toBe("live");
    expect(clockTone("1st 8:00")).toBe("live");
  });
});

describe("leadingSide", () => {
  it("returns none when either score is nullish", () => {
    expect(leadingSide(null, 3)).toBe("none");
    expect(leadingSide(7, undefined)).toBe("none");
    expect(leadingSide(null, null)).toBe("none");
  });

  it("returns tie when scores are equal", () => {
    expect(leadingSide(0, 0)).toBe("tie");
    expect(leadingSide(14, 14)).toBe("tie");
  });

  it("returns home when the home score is higher", () => {
    expect(leadingSide(21, 14)).toBe("home");
  });

  it("returns away when the away score is higher", () => {
    expect(leadingSide(10, 17)).toBe("away");
  });
});
