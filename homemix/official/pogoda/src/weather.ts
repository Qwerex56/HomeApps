// Open-Meteo (https://open-meteo.com): free, no key. Both hosts are in homemix.json "network", so the sandbox lets
// only them through (CSP connect-src).

/** Where the household is: chosen once by an Admin (collection "place", permission place.write). */
export interface Place {
	name: string;
	country: string;
	latitude: number;
	longitude: number;
}

export interface Forecast {
	now: { temperature: number; code: number; wind: number };
	days: { date: string; code: number; max: number; min: number; rain: number | null }[];
}

export async function search(name: string): Promise<Place[]> {
	const url = new URL("https://geocoding-api.open-meteo.com/v1/search");
	url.search = new URLSearchParams({ name, count: "5", language: "pl", format: "json" }).toString();
	const response = await fetch(url);
	if (!response.ok) throw new Error(`Open-Meteo: ${response.status}`);
	const body = (await response.json()) as {
		results?: { name: string; country?: string; admin1?: string; latitude: number; longitude: number }[];
	};
	return (body.results ?? []).map((r) => ({
		name: r.admin1 && r.admin1 !== r.name ? `${r.name} (${r.admin1})` : r.name,
		country: r.country ?? "",
		latitude: r.latitude,
		longitude: r.longitude,
	}));
}

export async function forecast(place: Place): Promise<Forecast> {
	const url = new URL("https://api.open-meteo.com/v1/forecast");
	url.search = new URLSearchParams({
		latitude: String(place.latitude),
		longitude: String(place.longitude),
		current: "temperature_2m,weather_code,wind_speed_10m",
		daily: "weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max",
		timezone: "auto",
		forecast_days: "7",
	}).toString();
	const response = await fetch(url);
	if (!response.ok) throw new Error(`Open-Meteo: ${response.status}`);
	const body = await response.json();
	return {
		now: {
			temperature: body.current.temperature_2m,
			code: body.current.weather_code,
			wind: body.current.wind_speed_10m,
		},
		days: (body.daily.time as string[]).map((date, i) => ({
			date,
			code: body.daily.weather_code[i],
			max: body.daily.temperature_2m_max[i],
			min: body.daily.temperature_2m_min[i],
			rain: body.daily.precipitation_probability_max?.[i] ?? null,
		})),
	};
}

/** WMO weather codes as Open-Meteo gives them: an emoji and a short Polish description. */
export function describe(code: number): { icon: string; text: string } {
	if (code === 0) return { icon: "☀️", text: "Bezchmurnie" };
	if (code <= 2) return { icon: "🌤️", text: "Małe zachmurzenie" };
	if (code === 3) return { icon: "☁️", text: "Pochmurno" };
	if (code <= 48) return { icon: "🌫️", text: "Mgła" };
	if (code <= 57) return { icon: "🌦️", text: "Mżawka" };
	if (code <= 67) return { icon: "🌧️", text: "Deszcz" };
	if (code <= 77) return { icon: "🌨️", text: "Śnieg" };
	if (code <= 82) return { icon: "🌧️", text: "Przelotny deszcz" };
	if (code <= 86) return { icon: "🌨️", text: "Przelotny śnieg" };
	return { icon: "⛈️", text: "Burza" };
}

export const round = (value: number) => Math.round(value);
