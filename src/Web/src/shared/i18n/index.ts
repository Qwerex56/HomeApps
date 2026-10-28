import { createI18n } from 'vue-i18n'
import pl from './pl.json'

// Polish only in 1.0 (Q5); a second language is one more JSON file here.
export type MessageSchema = typeof pl

export const i18n = createI18n<[MessageSchema], 'pl'>({
  legacy: false,
  locale: 'pl',
  fallbackLocale: 'pl',
  messages: { pl },
})
