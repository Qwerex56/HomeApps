import { describe, expect, it } from 'vitest'
import { readEvents } from '../api'

function streamOf(...chunks: string[]) {
  const encoder = new TextEncoder()
  return new ReadableStream<Uint8Array>({
    start(controller) {
      for (const chunk of chunks) controller.enqueue(encoder.encode(chunk))
      controller.close()
    },
  })
}

describe('readEvents', () => {
  it('reads events split across chunks, JSON and plain', async () => {
    const events = []
    for await (const event of readEvents(
      streamOf(
        'event: text\ndata: "Dodałem ',
        'mleko."\n\nevent: action\ndata: {"id":"a1","status":"Done"}\n\n',
        'event: text\ndata: plain\n\n',
      ),
    )) {
      events.push(event)
    }

    expect(events).toEqual([
      { event: 'text', data: 'Dodałem mleko.' },
      { event: 'action', data: { id: 'a1', status: 'Done' } },
      { event: 'text', data: 'plain' },
    ])
  })
})
