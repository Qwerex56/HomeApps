import { a as e, d as t, g as n, h as r, l as i, n as a, o, r as s, t as c, u as l } from "./_plugin-vue_export-helper-BjJZ4CBC.js";
// src/LatestWidget.vue?vue&type=script&setup=true&lang.ts
var u = { class: "hello-latest" }, d = s({
	id: "hello",
	routes: { "": () => import("./GreetingsPage-DAPLWe_V.js") },
	widgets: { latest: /* @__PURE__ */ c(/* @__PURE__ */ i({
		__name: "LatestWidget",
		props: {
			householdId: {},
			place: {}
		},
		setup(i) {
			let s = i, { host: c, greetings: d, load: f } = a(() => s.householdId), p = e(() => d.value.at(-1)?.text);
			return l(f), (e, i) => (t(), o("p", u, r(p.value ?? n(c).t("none")), 1));
		}
	}), [["__scopeId", "data-v-ea12c9ac"]]) },
	messages: { pl: {
		title: "Powitania w domu {name}",
		say: "Przywitaj się",
		placeholder: "Napisz coś miłego",
		none: "Nikt jeszcze się nie przywitał.",
		readOnly: "Możesz tylko czytać powitania."
	} }
});
export { d as default };
