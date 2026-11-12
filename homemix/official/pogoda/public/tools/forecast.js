// pogoda_forecast for JARVIS (docs/homemix.md, "JARVIS"): runs on the HomeMix server in Jint, with the bridge's ctx
// and nothing else (no URL, no global fetch), so it builds the Open-Meteo address by hand.
const codes = [
	[0, "bezchmurnie"],
	[2, "małe zachmurzenie"],
	[3, "pochmurno"],
	[48, "mgła"],
	[57, "mżawka"],
	[67, "deszcz"],
	[77, "śnieg"],
	[82, "przelotny deszcz"],
	[86, "przelotny śnieg"],
	[99, "burza"],
];

const describe = (code) => (codes.find(([max]) => code <= max) ?? codes[codes.length - 1])[1];

export default async function ({ days = 2 }, ctx) {
	const place = (await ctx.data.get("place", "current"))?.value;
	if (!place) return "Pogoda nie ma jeszcze miejsca: Admin domu wybiera je na stronie dodatku.";

	const count = Math.min(Math.max(Math.round(days), 1), 7);
	const url =
		"https://api.open-meteo.com/v1/forecast" +
		`?latitude=${place.latitude}&longitude=${place.longitude}` +
		"&current=temperature_2m,weather_code,wind_speed_10m" +
		"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
		`&timezone=auto&forecast_days=${count}`;
	const response = await ctx.fetch(url);
	if (!response.ok) throw new Error(`Open-Meteo: ${response.status}`);
	const body = await response.json();

	const now = body.current;
	const lines = [
		`${place.name}: teraz ${Math.round(now.temperature_2m)}°C, ${describe(now.weather_code)}, wiatr ${Math.round(now.wind_speed_10m)} km/h.`,
	];
	body.daily.time.forEach((date, i) => {
		const rain = body.daily.precipitation_probability_max?.[i];
		lines.push(
			`${date}: ${describe(body.daily.weather_code[i])}, ${Math.round(body.daily.temperature_2m_min[i])}..` +
				`${Math.round(body.daily.temperature_2m_max[i])}°C` +
				(rain == null ? "" : `, deszcz ${rain}%`),
		);
	});
	return lines.join("\n");
}
