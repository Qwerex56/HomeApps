self.onmessage = function (e: MessageEvent<string>) {
  async function fetchWeather() {
    const url: string = e.data

    try {
      const response = await fetch(url)
      const data = await response.json()
      self.postMessage(data)
    } catch (err: unknown) {
      const errorMessage =
        typeof err === 'object' && err !== null && 'message' in err
          ? (err as { message: string }).message
          : String(err)
      self.postMessage({ error: errorMessage })
    }
  }

  function scheduleNextFetch() {
    const now = new Date()
    const msToNextHour =
      (60 - now.getMinutes()) * 60 * 1000 - now.getSeconds() * 1000 - now.getMilliseconds()
    setTimeout(() => {
      fetchWeather()
      setInterval(fetchWeather, 60 * 60 * 1000) // Every hour
    }, msToNextHour)
  }

  fetchWeather() // Initial fetch
  scheduleNextFetch()
}
