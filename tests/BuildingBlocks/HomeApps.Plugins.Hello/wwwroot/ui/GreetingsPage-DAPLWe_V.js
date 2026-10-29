import { _ as e, c as t, d as n, f as r, g as i, h as a, i as o, l as s, m as c, n as l, o as u, p as d, s as f, t as p, u as m } from "./_plugin-vue_export-helper-BjJZ4CBC.js";
// src/GreetingsPage.vue?vue&type=script&setup=true&lang.ts
var h = { class: "hello-page" }, g = { key: 1 }, _ = { key: 2 }, v = { key: 3 }, y = /*#__PURE__*/ p(/* @__PURE__ */ s({
	__name: "GreetingsPage",
	setup(s) {
		let { host: p, greetings: y, load: b, say: x } = l(() => p.household.value.id), S = r("");
		m(b);
		async function C() {
			await x(S.value), S.value = "";
		}
		return (r, s) => {
			let l = c("InputText"), m = c("Button");
			return n(), u("div", h, [
				f("h2", null, a(i(p).t("title", { name: i(p).household.value?.name })), 1),
				i(p).can("greetings.write") ? (n(), u("form", {
					key: 0,
					class: "hello-form",
					onSubmit: e(C, ["prevent"])
				}, [t(l, {
					modelValue: S.value,
					"onUpdate:modelValue": s[0] ||= (e) => S.value = e,
					placeholder: i(p).t("placeholder")
				}, null, 8, ["modelValue", "placeholder"]), t(m, {
					type: "submit",
					label: i(p).t("say"),
					icon: "pi pi-send"
				}, null, 8, ["label"])], 32)) : (n(), u("p", g, a(i(p).t("readOnly")), 1)),
				i(y).length ? (n(), u("ul", _, [(n(!0), u(o, null, d(i(y), (e) => (n(), u("li", { key: e.id }, a(e.text), 1))), 128))])) : (n(), u("p", v, a(i(p).t("none")), 1))
			]);
		};
	}
}), [["__scopeId", "data-v-a2a574d2"]]);
export { y as default };
