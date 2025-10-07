<script setup lang="ts">
import type WeatherCurrentBoxDto from '@/common/types/weather/dto/WeatherCurrentBoxDto'
import { MapWeatherCurrentToBoxDto } from '@/common/types/weather/dto/WeatherCurrentBoxDto'
import type WeatherLocationBoxDto from '@/common/types/weather/dto/WeatherLocationBoxDto'
import { MapWeatherLocationToBoxDto } from '@/common/types/weather/dto/WeatherLocationBoxDto'
import { MapJsonToWeatherCurrent } from '@/common/types/weather/mappers/MapJsonToWeatherCurrent'
import { MapJsonToWeatherLocation } from '@/common/types/weather/mappers/MapJsonToWeatherLocation'
import UriString from '@/common/uriStrings/UriString'
import CurrentWeatherBox from '@/components/weatherApp/CurrentWeatherBox.vue'
import { ref } from 'vue'

const apiKey: string = import.meta.env.VITE_WEATHER_API_KEY
const apiUrl: string = import.meta.env.VITE_WEATHER_API_URL

const apiUri = new UriString(apiUrl + '/current.json')
apiUri.AddParameter('key', apiKey)
apiUri.AddParameter('q', 'Wroclaw')
apiUri.AddParameter('aqi', 'no')
apiUri.AddParameter('lang', 'pl')

const todayWeather = ref<WeatherCurrentBoxDto>()
const location = ref<WeatherLocationBoxDto>()

if (window.Worker) {
  const weatherWorker = new Worker(new URL('@/common/workers/weatherWorker.ts', import.meta.url), {
    type: 'module',
  })

  weatherWorker.postMessage(apiUri.GetUri())
  weatherWorker.onmessage = (event) => {
    const respTodayWeather = MapJsonToWeatherCurrent(event.data.current)
    const respLocation = MapJsonToWeatherLocation(event.data.location)

    todayWeather.value = MapWeatherCurrentToBoxDto(respTodayWeather)
    location.value = MapWeatherLocationToBoxDto(respLocation)
  }
}
</script>

<template>
  <CurrentWeatherBox :weatherCondition="todayWeather" :weatherLocation="location" />
</template>
