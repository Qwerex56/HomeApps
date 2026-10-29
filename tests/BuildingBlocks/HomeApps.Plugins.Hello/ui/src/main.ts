import { definePlugin } from '@homeapps/plugin-sdk'
import LatestWidget from './LatestWidget.vue'

export default definePlugin({
  id: 'hello',
  routes: { '': () => import('./GreetingsPage.vue') },
  widgets: { latest: LatestWidget },
  messages: {
    pl: {
      title: 'Powitania w domu {name}',
      say: 'Przywitaj się',
      placeholder: 'Napisz coś miłego',
      none: 'Nikt jeszcze się nie przywitał.',
      readOnly: 'Możesz tylko czytać powitania.',
    },
  },
})
