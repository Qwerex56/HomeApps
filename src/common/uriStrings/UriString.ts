import type IUriString from './IUriString'

export default class UriString implements IUriString {
  private baseUri: string | null = null;
  private queryParams: Map<string, string> = new Map<string, string>();

  /**
   *
   */
  constructor(baseUri: string) {
    this.baseUri = baseUri;
  }

  public GetUri(): string {
    if (this.baseUri === null) {
      throw new Error('Base URI is not set.');
    }

    return this.baseUri + this.buildQueryString();
  }

  public GetBaseUri(): string {
    throw new Error('Method not implemented.');
  }

  public AddParameter(key: string, value: string): Error {
    if (this.queryParams.has(key)) {
      return new Error(`Parameter with key ${key} already exists.`);
    }

    this.queryParams.set(key, value);
    return new Error('OK');
  }

  public RemoveParameter(key: string): Error {
    if (!this.queryParams.has(key)) {
      return new Error(`Parameter with key ${key} does not exist.`);
    }

    this.queryParams.delete(key);
    return new Error('OK');
  }

  private buildQueryString(): string {
    const params = Array.from(this.queryParams.entries()).map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`);
    return params.length > 0 ? `?${params.join('&')}` : '';
  }
}
