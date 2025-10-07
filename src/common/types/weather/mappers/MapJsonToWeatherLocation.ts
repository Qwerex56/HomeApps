import type WeatherLocation from '../WeatherLocation'

export function MapJsonToWeatherLocation(json: any): WeatherLocation {
  return {
    name: json.name,
    region: json.region,
    country: json.country,
    lat: json.lat,
    lon: json.lon,
    tz_id: json.tz_id,
    localtime_epoch: json.localtime_epoch,
    localtime: json.localtime,
  }
}
