import type WeatherCurrent from '../WeatherCurrent'

export default interface WeatherCurrentBoxDto {
  last_updated_epoch: number
  last_updated: string
  temp_c: number
  is_day: number
  condition: WeatherConditionBoxDto
  wind_kph: number
  wind_dir: string
  feelslike_c: number
  humidity: number
  uv: number
}

export interface WeatherConditionBoxDto {
  text: string
  icon: string
}

export function MapWeatherCurrentToBoxDto(current: WeatherCurrent): WeatherCurrentBoxDto {
  return {
    last_updated_epoch: current.last_updated_epoch,
    last_updated: current.last_updated,
    temp_c: current.temp_c,
    is_day: current.is_day,
    condition: {
      text: current.condition.text,
      icon: current.condition.icon,
    } as WeatherConditionBoxDto,
    wind_kph: current.wind_kph,
    wind_dir: current.wind_dir,
    feelslike_c: current.feelslike_c,
    humidity: current.humidity,
    uv: current.uv,
  }
}
