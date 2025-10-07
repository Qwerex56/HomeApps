export default interface IUriString {
  GetUri(): string
  GetBaseUri(): string

  AddParameter(key: string, value: string): Error
  RemoveParameter(key: string): Error
}
