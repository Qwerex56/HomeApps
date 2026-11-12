// The shared timer as one document: who started what and when. Every frame computes the rest from the clock.
export type Mode = "focus" | "break";

export interface Timer {
	mode: Mode;
	/** ISO time it started, or null while stopped. */
	startedAt: string | null;
	/** Seconds left when it was paused or stopped. */
	remaining: number;
	by: string;
}

export interface Settings {
	focus: number;
	break: number;
}

export const defaults: Settings = { focus: 25, break: 5 };

export function idle(settings: Settings, mode: Mode = "focus", by = ""): Timer {
	return { mode, startedAt: null, remaining: settings[mode] * 60, by };
}

/** Seconds left at `now`. */
export function left(timer: Timer, now: number): number {
	if (!timer.startedAt) return timer.remaining;
	return Math.max(
		0,
		timer.remaining -
			Math.floor((now - Date.parse(timer.startedAt)) / 1000),
	);
}

export function clock(seconds: number) {
	const m = Math.floor(seconds / 60);
	const s = seconds % 60;
	return `${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`;
}

export function today(now = new Date()) {
	return now.toISOString().slice(0, 10);
}
