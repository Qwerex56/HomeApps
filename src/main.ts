import './assests/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { createI18n } from 'vue-i18n'

const i18n = createI18n({
  locale: 'pl',
  fallbackLocale: 'en',
  messages: {
    pl: {
      message: {
        wind: 'Wiatr',
        feels_like: 'Odczuwalna',
        humidity: 'Wilgotność',
      },
    },
    en: {
      message: {
        wind: 'Wind',
        feels_like: 'Feels like',
        humidity: 'Humidity',
      },
    },
  },
})

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.use(i18n)

app.mount('#app')
