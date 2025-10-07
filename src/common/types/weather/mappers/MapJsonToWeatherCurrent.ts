import type WeatherCurrent from '../WeatherCurrent'
import type { WeatherCondition } from '../WeatherCurrent'

export function MapJsonToWeatherCurrent(json: any): WeatherCurrent {
  return {
    last_updated_epoch: json.last_updated_epoch,
    last_updated: json.last_updated,
    temp_c: json.temp_c,
    temp_f: json.temp_f,
    is_day: json.is_day,
    condition: {
      text: json.condition.text,
      icon: json.condition.icon,
      code: json.condition.code,
    } as WeatherCondition,
    wind_mph: json.wind_mph,
    wind_kph: json.wind_kph,
    wind_degree: json.wind_degree,
    wind_dir: json.wind_dir,
    pressure_mb: json.pressure_mb,
    pressure_in: json.pressure_in,
    precip_mm: json.precip_mm,
    precip_in: json.precip_in,
    humidity: json.humidity,
    cloud: json.cloud,
    feelslike_c: json.feelslike_c,
    feelslike_f: json.feelslike_f,
    windchill_c: json.windchill_c,
    windchill_f: json.windchill_f,
    heatindex_c: json.heatindex_c,
    heatindex_f: json.heatindex_f,
    dewpoint_c: json.dewpoint_c,
    dewpoint_f: json.dewpoint_f,
    vis_km: json.vis_km,
    vis_miles: json.vis_miles,
    uv: json.uv,
    gust_mph: json.gust_mph,
    gust_kph: json.gust_kph,
    short_rad: json.short_rad,
    diff_rad: json.diff_rad,
    dni: json.dni,
    gti: json.gti,
  }
}
