<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { useCan, useCollection, useHomeMix } from "@homemix/vue";
import { describe, forecast, round, search, type Forecast, type Place } from "./weather";

// The official HomeMix Pogoda: an Admin picks the household's place once (collection "place", permission
// place.write); every frame then asks Open-Meteo itself, every 15 minutes. Nothing about the weather is stored.
const { context, ready } = useHomeMix();
const places = useCollection<Place>("place");
const canSet = useCan("place.write");

const place = computed(() => places.items.value.find((d) => d.key === "current")?.value ?? null);
const widget = computed(() => !!context.value?.widget);
const weather = ref<Forecast | null>(null);
const error = ref("");

async function refresh() {
	if (!place.value) return;
	try {
		weather.value = await forecast(place.value);
		error.value = "";
	} catch (e) {
		error.value = `Nie udało się pobrać pogody (${(e as Error).message}).`;
	}
}

watch(place, refresh, { immediate: true });
const timer = setInterval(refresh, 15 * 60 * 1000);
onBeforeUnmount(() => clearInterval(timer));

const query = ref("");
const found = ref<Place[]>([]);
const searching = ref(false);
const editing = ref(false);

async function find() {
	if (!query.value.trim()) return;
	searching.value = true;
	try {
		found.value = await search(query.value.trim());
		error.value = found.value.length ? "" : "Nie znaleziono takiego miejsca.";
	} catch (e) {
		error.value = (e as Error).message;
	} finally {
		searching.value = false;
	}
}

async function choose(next: Place) {
	await places.put("current", next);
	found.value = [];
	query.value = "";
	editing.value = false;
}

const day = (date: string) =>
	new Date(`${date}T12:00:00`).toLocaleDateString(context.value?.language ?? "pl", { weekday: "short", day: "numeric" });
</script>

<template>
	<main v-if="ready" :class="['pogoda', { widget }]">
		<template v-if="place && weather">
			<div class="now">
				<span class="icon" aria-hidden="true">{{ describe(weather.now.code).icon }}</span>
				<div>
					<div class="temperature">{{ round(weather.now.temperature) }}°C</div>
					<div class="hm-muted">{{ describe(weather.now.code).text }} · {{ place.name }}</div>
				</div>
			</div>
			<ol v-if="!widget" class="days">
				<li v-for="d in weather.days" :key="d.date" class="hm-card">
					<div class="hm-muted">{{ day(d.date) }}</div>
					<div class="icon" :title="describe(d.code).text">{{ describe(d.code).icon }}</div>
					<div>{{ round(d.max) }}° / {{ round(d.min) }}°</div>
					<div v-if="d.rain !== null" class="hm-muted">💧 {{ d.rain }}%</div>
				</li>
			</ol>
			<p v-if="!widget" class="hm-muted">Wiatr {{ round(weather.now.wind) }} km/h. Dane: Open-Meteo.</p>
		</template>
		<p v-else-if="place && !error" class="hm-muted">Wczytuję pogodę…</p>
		<p v-else-if="!place" class="hm-muted">
			{{ canSet ? "Wybierz miejsce poniżej." : "Administrator domu jeszcze nie wybrał miejsca." }}
		</p>
		<p v-if="error" class="error">{{ error }}</p>

		<section v-if="!widget && canSet && (editing || !place)" class="hm-card choose">
			<form @submit.prevent="find">
				<label>
					Miejsce
					<input v-model="query" placeholder="np. Kraków" />
				</label>
				<button class="hm-button" :disabled="searching">Szukaj</button>
			</form>
			<ul v-if="found.length">
				<li v-for="p in found" :key="`${p.latitude},${p.longitude}`">
					<button class="hm-button secondary" @click="choose(p)">{{ p.name }}, {{ p.country }}</button>
				</li>
			</ul>
		</section>
		<button v-else-if="!widget && canSet" class="hm-button secondary" @click="editing = true">Zmień miejsce</button>
	</main>
</template>

<style scoped>
.pogoda {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	padding: 1rem;
}
.pogoda.widget {
	padding: 0;
}
.now {
	display: flex;
	align-items: center;
	gap: 0.75rem;
}
.now .icon {
	font-size: 3rem;
	line-height: 1;
}
.temperature {
	font-size: 2.5rem;
	font-weight: 600;
	line-height: 1;
}
.widget .temperature {
	font-size: 2rem;
}
.days {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(6rem, 1fr));
	gap: 0.5rem;
	list-style: none;
	padding: 0;
	margin: 0;
}
.days li {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 0.25rem;
}
.days .icon {
	font-size: 1.75rem;
}
.choose form {
	display: flex;
	gap: 0.5rem;
	align-items: end;
	flex-wrap: wrap;
}
.choose input {
	margin-left: 0.25rem;
}
.choose ul {
	list-style: none;
	padding: 0;
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
}
.error {
	color: #dc2626;
}
</style>
