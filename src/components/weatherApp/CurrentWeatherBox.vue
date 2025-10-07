<script setup lang="ts">
import type WeatherCurrentBoxDto from '@/common/types/weather/dto/WeatherCurrentBoxDto'
import type WeatherLocationBoxDto from '@/common/types/weather/dto/WeatherLocationBoxDto'
import { faDroplet, faSun, faWind } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import type { PropType } from 'vue'

const props = defineProps({
  weatherCondition: {
    type: Object as PropType<WeatherCurrentBoxDto>,
    required: false,
  },
  weatherLocation: {
    type: Object as PropType<WeatherLocationBoxDto>,
    required: false,
  },
})
</script>

<template>
  <section
    class="flex flex-col text-gray-200 border-1 p-6 rounded-4xl bg-blue-800/10 backdrop-blur-sm gap-6 w-fit"
  >
    <div class="flex justify-center">
      <h1 class="text-9xl font-bold text-center">
        {{ props.weatherLocation?.name ?? '---' }}
      </h1>
    </div>

    <div class="grid grid-cols-[auto_1fr_auto] gap-x-12">
      <div class="flex flex-col">
        <div class="grid grid-cols-[auto_1fr] grid-rows-2 gap-x-2">
          <FontAwesomeIcon
            :icon="faWind"
            class="row-span-2 self-center justify-self-end text-3xl"
          />
          <p class="text-sm self-end">{{ props.weatherCondition?.wind_kph }} km/h</p>
          <p class="text-lg self-start">{{ $t('message.wind') }}</p>
        </div>

        <div class="grid grid-cols-[auto_1fr] grid-rows-2 gap-x-2 justify-self-start">
          <FontAwesomeIcon
            :icon="faDroplet"
            class="row-span-2 self-center justify-self-end text-3xl"
          />
          <p class="text-sm self-end">{{ props.weatherCondition?.humidity }}%</p>
          <p class="text-lg self-start">{{ $t('message.humidity') }}</p>
        </div>

        <div class="grid grid-cols-[auto_1fr] grid-rows-2 gap-x-2">
          <FontAwesomeIcon :icon="faSun" class="row-span-2 self-center justify-self-end text-3xl" />
          <p class="text-sm self-end">{{ props.weatherCondition?.uv }}</p>
          <p class="text-lg self-start">UV</p>
        </div>
      </div>

      <div class="flex flex-col items-center self-center">
        <img
          :src="props.weatherCondition?.condition.icon"
          :alt="props.weatherCondition?.condition.text"
          class="w-32 h-auto"
        />
        <p>{{ props.weatherCondition?.condition.text }}</p>
      </div>

      <div class="text-center self-center">
        <h2 class="text-8xl font-semibold">{{ props.weatherCondition?.temp_c ?? '---' }}&#8451;</h2>
        <h3 class="text-xl font-medium">
          {{ $t('message.feels_like') }} {{ props.weatherCondition?.feelslike_c ?? '---' }}&#8451;
        </h3>
      </div>
    </div>
  </section>
</template>
