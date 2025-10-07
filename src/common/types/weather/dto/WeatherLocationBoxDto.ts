export default interface WeatherLocationBoxDto {
  name: string
  country: string
  localtime_epoch: number
  localtime: string
}

export function MapWeatherLocationToBoxDto(location: WeatherLocationBoxDto): WeatherLocationBoxDto {
  return {
    name: location.name,
    country: location.country,
    localtime_epoch: location.localtime_epoch,
    localtime: location.localtime,
  }
}
