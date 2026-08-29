export type ClockTone = "unknown" | "final" | "upcoming" | "live";

export function clockTone(displayClock: string | null | undefined): ClockTone {
  if (displayClock == null || displayClock === "") return "unknown";
  if (/final/i.test(displayClock)) return "final";
  if (displayClock.includes("AM") || displayClock.includes("PM")) return "upcoming";
  return "live";
}

export type LeadingSide = "home" | "away" | "tie" | "none";

export function leadingSide(
  homeScore: number | null | undefined,
  awayScore: number | null | undefined,
): LeadingSide {
  if (homeScore == null || awayScore == null) return "none";
  if (homeScore === awayScore) return "tie";
  return homeScore > awayScore ? "home" : "away";
}
