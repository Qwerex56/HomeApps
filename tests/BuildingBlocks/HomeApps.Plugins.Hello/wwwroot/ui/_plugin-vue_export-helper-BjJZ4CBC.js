// \0homeapps-shared:vue
var e = globalThis.__HOMEAPPS_SHARED__?.vue;
if (!e) throw Error("HomeApps does not share vue; is this plugin loaded by the HomeApps web app?");
e.default;
var { BaseTransition: t, BaseTransitionPropsValidators: n, Comment: r, DeprecationTypes: i, EffectScope: a, ErrorCodes: o, ErrorTypeStrings: s, Fragment: c, KeepAlive: l, ReactiveEffect: u, Static: d, Suspense: ee, Teleport: te, Text: ne, TrackOpTypes: re, Transition: ie, TransitionGroup: ae, TriggerOpTypes: oe, VueElement: se, assertNumber: ce, callWithAsyncErrorHandling: le, callWithErrorHandling: ue, camelize: de, capitalize: fe, cloneVNode: pe, compatUtils: me, compile: he, computed: f, createApp: p, createBlock: m, createCommentVNode: h, createElementBlock: g, createElementVNode: _, createHydrationRenderer: v, createPropsRestProxy: y, createRenderer: b, createSSRApp: x, createSlots: S, createStaticVNode: C, createTextVNode: w, createVNode: T, customRef: E, defineAsyncComponent: D, defineComponent: O, defineCustomElement: k, defineEmits: A, defineExpose: j, defineModel: M, defineOptions: N, defineProps: P, defineSSRCustomElement: F, defineSlots: I, devtools: L, effect: R, effectScope: ge, getCurrentInstance: _e, getCurrentScope: ve, getCurrentWatcher: ye, getTransitionRawChildren: be, guardReactiveProps: xe, h: Se, handleError: Ce, hasInjectionContext: we, hydrate: Te, hydrateOnIdle: Ee, hydrateOnInteraction: De, hydrateOnMediaQuery: Oe, hydrateOnVisible: ke, initCustomFormatter: Ae, initDirectivesForSSR: je, inject: z, isMemoSame: Me, isProxy: Ne, isReactive: Pe, isReadonly: Fe, isRef: Ie, isRuntimeOnly: Le, isShallow: Re, isVNode: ze, markRaw: Be, mergeDefaults: Ve, mergeModels: He, mergeProps: Ue, nextTick: We, nodeOps: Ge, normalizeClass: Ke, normalizeProps: qe, normalizeStyle: Je, onActivated: Ye, onBeforeMount: Xe, onBeforeUnmount: Ze, onBeforeUpdate: Qe, onDeactivated: $e, onErrorCaptured: et, onMounted: B, onRenderTracked: tt, onRenderTriggered: nt, onScopeDispose: rt, onServerPrefetch: it, onUnmounted: at, onUpdated: ot, onWatcherCleanup: st, openBlock: V, patchProp: ct, popScopeId: lt, provide: ut, proxyRefs: dt, pushScopeId: ft, queuePostFlushCb: pt, reactive: mt, readonly: ht, ref: H, registerRuntimeCompiler: gt, render: _t, renderList: U, renderSlot: vt, resolveComponent: W, resolveDirective: yt, resolveDynamicComponent: bt, resolveFilter: xt, resolveTransitionHooks: St, setBlockTracking: Ct, setDevtoolsHook: wt, setTransitionHooks: Tt, shallowReactive: Et, shallowReadonly: Dt, shallowRef: Ot, ssrContextKey: kt, ssrUtils: At, stop: jt, toDisplayString: G, toHandlerKey: Mt, toHandlers: Nt, toRaw: Pt, toRef: Ft, toRefs: It, toValue: Lt, transformVNodeArgs: Rt, triggerRef: zt, unref: K, useAttrs: Bt, useCssModule: Vt, useCssVars: Ht, useHost: Ut, useId: Wt, useModel: Gt, useSSRContext: Kt, useShadowRoot: qt, useSlots: Jt, useTemplateRef: Yt, useTransitionState: Xt, vModelCheckbox: Zt, vModelDynamic: Qt, vModelRadio: $t, vModelSelect: en, vModelText: tn, vShow: nn, version: rn, warn: an, watch: on, watchEffect: sn, watchPostEffect: q, watchSyncEffect: cn, withAsyncContext: ln, withCtx: un, withDefaults: dn, withDirectives: fn, withKeys: pn, withMemo: mn, withModifiers: J, withScopeId: hn } = e;
// ../../../../src/Web/plugin-sdk/dist/index.js
function Y(e) {
	return e;
}
var X = "homeapps:plugin-host";
function Z() {
	let e = z(X);
	if (!e) throw Error("useHost() works only inside a component the HomeApps web app renders for a plugin.");
	return e;
}
// src/useGreetings.ts
function Q(e) {
	let t = Z(), n = H([]), r = () => `${t.apiBase}/households/${e()}/greetings`;
	async function i() {
		n.value = (await t.http.get(r())).data;
	}
	async function a(e) {
		try {
			await t.http.post(r(), { text: e }), await i();
		} catch (e) {
			t.notify("error", t.errorMessage(e));
		}
	}
	return {
		host: t,
		greetings: n,
		load: i,
		say: a
	};
}
// \0plugin-vue:export-helper
var $ = (e, t) => {
	let n = e.__vccOpts || e;
	for (let [e, r] of t) n[e] = r;
	return n;
};
export { J as _, f as a, T as c, V as d, H as f, K as g, G as h, c as i, O as l, W as m, Q as n, g as o, U as p, Y as r, _ as s, $ as t, B as u };
