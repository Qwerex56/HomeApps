import { createI18n } from 'vue-i18n'
import pl from './pl.json'

// Polish only in 1.0 (Q5); a second language is one more JSON file here.
export type MessageSchema = typeof pl

/** Polish plurals as "one | few | many": 1 osoba, 2–4 osoby (but 12–14 osób), 0 and 5+ osób. */
export function polishPlural(choice: number) {
  const n = Math.abs(choice)
  if (n === 1) return 0
  const tens = n % 100
  return n % 10 >= 2 && n % 10 <= 4 && (tens < 12 || tens > 14) ? 1 : 2
}

export const i18n = createI18n<[MessageSchema], 'pl'>({
  legacy: false,
  locale: 'pl',
  fallbackLocale: 'pl',
  messages: { pl },
  pluralRules: { pl: polishPlural },
  datetimeFormats: {
    pl: {
      short: { year: 'numeric', month: 'long', day: 'numeric' },
      long: { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' },
    },
  },
})
