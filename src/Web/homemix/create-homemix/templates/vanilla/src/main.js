import { autoResize, connect } from '@homemix/sdk'

// The bridge without a framework: context, data and the host's theme.
const homemix = connect()
autoResize(homemix)

const ctx = await homemix.context()
for (const [name, value] of Object.entries(ctx.theme.vars)) document.documentElement.style.setProperty(name, value)
document.body.style.color = 'var(--p-text-color)'

const counter = await homemix.data.get('counter', 'clicks')
let clicks = counter?.value ?? 0
const button = document.createElement('button')
const render = () => (button.textContent = `${ctx.user.name}: ${clicks}`)
button.onclick = async () => {
  clicks += 1
  render()
  await homemix.data.put('counter', 'clicks', clicks)
}
render()
document.querySelector('#app').append(button)
