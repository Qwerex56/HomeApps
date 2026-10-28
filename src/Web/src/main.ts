import './styles/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import Tooltip from 'primevue/tooltip'
import App from './app/App.vue'
import { router } from './app/router'
import { installPrimeVue } from './app/primevue'
import { i18n } from './shared/i18n'
import { queryClient } from './api/queryClient'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(i18n)
app.use(VueQueryPlugin, { queryClient })
installPrimeVue(app)
app.directive('tooltip', Tooltip)

app.mount('#app')
