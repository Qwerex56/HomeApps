<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { useCan, useCollection, useHomeMix } from "@homemix/vue";
import {
	clock,
	defaults,
	idle,
	left,
	today,
	type Mode,
	type Settings,
	type Timer,
} from "./timer";

// The official HomeMix Pomodoro: one timer shared by the household (storage.household), each person's finished
// sessions counted privately (storage.user), the lengths set by Admins (permission settings.write), and the end of a
// focus sent to everyone else's devices (notify.push).
const { homemix, context, ready } = useHomeMix();
const timers = useCollection<Timer>("timer");
const settingsDocs = useCollection<Settings>("settings");
const stats = useCollection<{ count: number }>("stats", "user");
const canSet = useCan("settings.write");

const now = ref(Date.now());
const tick = setInterval(() => (now.value = Date.now()), 500);
onBeforeUnmount(() => clearInterval(tick));

const settings = computed(
	() =>
		settingsDocs.items.value.find((d) => d.key === "lengths")?.value ??
		defaults,
);
const timer = computed(
	() =>
		timers.items.value.find((d) => d.key === "current")?.value ??
		idle(settings.value),
);
const seconds = computed(() => left(timer.value, now.value));
const running = computed(() => !!timer.value.startedAt && seconds.value > 0);
const widget = computed(() => !!context.value?.widget);
const doneToday = computed(
	() => stats.items.value.find((d) => d.key === today())?.value.count ?? 0,
);
const busy = ref(false);
const error = ref("");

async function save(next: Timer) {
	busy.value = true;
	error.value = "";
	try {
		await timers.put("current", next);
	} catch (e) {
		error.value = (e as Error).message;
		await timers.refresh();
	} finally {
		busy.value = false;
	}
}

const me = () => context.value?.user.name ?? "";
const start = () =>
	save({ ...timer.value, startedAt: new Date().toISOString(), by: me() });
const pause = () =>
	save({
		...timer.value,
		startedAt: null,
		remaining: seconds.value,
		by: me(),
	});
const reset = (mode: Mode = timer.value.mode) =>
	save(idle(settings.value, mode, me()));

// The frame that sees the end (any open one) moves the timer on; the others get it from data.changed.
let finishing = false;
watch(seconds, async (value) => {
	if (value > 0 || !timer.value.startedAt || finishing) return;
	finishing = true;
	const finished = timer.value.mode;
	try {
		await timers.put(
			"current",
			idle(
				settings.value,
				finished === "focus" ? "break" : "focus",
				me(),
			),
		);
		if (finished === "focus") {
			await stats.put(today(), { count: doneToday.value + 1 });
			await homemix.toast(
				"Koniec skupienia, czas na przerwę.",
				"success",
			);
			// Everyone else on their devices too (notify.push); the same tag replaces the previous one.
			await homemix
				.push({ title: "Pomodoro", body: "Koniec skupienia, czas na przerwę.", tag: "end" })
				.catch(() => undefined);
		}
	} catch {
		await timers.refresh();
	} finally {
		finishing = false;
	}
});

async function setLength(mode: Mode, minutes: number) {
	const value = Math.min(Math.max(Math.round(minutes) || 1, 1), 180);
	await settingsDocs.put("lengths", { ...settings.value, [mode]: value });
}
</script>

<template>
	<main v-if="ready" :class="['pomodoro', { widget }]">
		<div class="mode hm-muted">
			{{ timer.mode === "focus" ? "Skupienie" : "Przerwa" }}
		</div>
		<div class="clock" :aria-live="running ? 'off' : 'polite'">
			{{ clock(seconds) }}
		</div>
		<div v-if="timer.by" class="hm-muted by">
			{{ running ? "uruchomił(a)" : "ostatnio" }}: {{ timer.by }}
		</div>
		<div class="actions">
			<button
				v-if="!running"
				class="hm-button"
				:disabled="busy"
				@click="start"
			>
				Start
			</button>
			<button v-else class="hm-button" :disabled="busy" @click="pause">
				Pauza
			</button>
			<button
				class="hm-button secondary"
				:disabled="busy"
				@click="reset()"
			>
				Od nowa
			</button>
			<button
				v-if="!widget"
				class="hm-button secondary"
				:disabled="busy"
				@click="reset(timer.mode === 'focus' ? 'break' : 'focus')"
			>
				{{ timer.mode === "focus" ? "Przerwa" : "Skupienie" }}
			</button>
		</div>
		<p v-if="error" class="error">{{ error }}</p>
		<template v-if="!widget">
			<p class="hm-muted">Twoje sesje dziś: {{ doneToday }}</p>
			<section class="hm-card settings">
				<label
					>Skupienie (min)
					<input
						type="number"
						min="1"
						max="180"
						:value="settings.focus"
						:disabled="!canSet"
						@change="
							setLength(
								'focus',
								Number(
									($event.target as HTMLInputElement).value,
								),
							)
						"
				/></label>
				<label
					>Przerwa (min)
					<input
						type="number"
						min="1"
						max="180"
						:value="settings.break"
						:disabled="!canSet"
						@change="
							setLength(
								'break',
								Number(
									($event.target as HTMLInputElement).value,
								),
							)
						"
				/></label>
				<p v-if="!canSet" class="hm-muted">
					Długości ustawia administrator domu.
				</p>
			</section>
		</template>
	</main>
</template>

<style scoped>
.pomodoro {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 0.75rem;
	padding: 1rem;
}
.pomodoro.widget {
	padding: 0;
	gap: 0.5rem;
}
.clock {
	font-size: 4rem;
	font-variant-numeric: tabular-nums;
	font-weight: 600;
	line-height: 1;
}
.widget .clock {
	font-size: 2.5rem;
}
.actions {
	display: flex;
	gap: 0.5rem;
	flex-wrap: wrap;
	justify-content: center;
}
.by {
	font-size: 0.85rem;
}
.error {
	color: #dc2626;
}
.settings {
	display: flex;
	gap: 1rem;
	flex-wrap: wrap;
	align-items: center;
}
.settings input {
	width: 4rem;
	margin-left: 0.25rem;
}
</style>
