import { describe, expect, it } from 'vitest'
import { authorizationRequestFrom } from '../appsApi'

describe('authorizationRequestFrom', () => {
  it('takes the OAuth parameters from the consent page query', () => {
    expect(
      authorizationRequestFrom({
        response_type: 'code',
        client_id: 'app_1',
        redirect_uri: 'http://localhost:33418/callback',
        code_challenge: 'abc',
        code_challenge_method: 'S256',
        resource: 'https://home.example/jarvis/mcp',
        state: 'xyz',
      }),
    ).toEqual({
      responseType: 'code',
      clientId: 'app_1',
      redirectUri: 'http://localhost:33418/callback',
      codeChallenge: 'abc',
      codeChallengeMethod: 'S256',
      resource: 'https://home.example/jarvis/mcp',
      state: 'xyz',
    })
  })

  it('leaves out what is missing or repeated', () => {
    expect(authorizationRequestFrom({ client_id: ['a', 'b'] })).toMatchObject({ clientId: null, state: null })
  })
})
