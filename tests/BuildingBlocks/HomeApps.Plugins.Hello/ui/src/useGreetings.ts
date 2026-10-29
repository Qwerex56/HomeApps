import { ref } from 'vue'
import { useHost } from '@homeapps/plugin-sdk'

export interface Greeting {
  id: string
  text: string
  by: string | null
}

export function useGreetings(householdId: () => string) {
  const host = useHost()
  const greetings = ref<Greeting[]>([])
  const url = () => `${host.apiBase}/households/${householdId()}/greetings`

  async function load() {
    greetings.value = (await host.http.get<Greeting[]>(url())).data
  }

  async function say(text: string) {
    try {
      await host.http.post(url(), { text })
      await load()
    } catch (error) {
      host.notify('error', host.errorMessage(error))
    }
  }

  return { host, greetings, load, say }
}
