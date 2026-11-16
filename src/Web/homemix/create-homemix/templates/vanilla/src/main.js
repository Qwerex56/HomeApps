import '@home-apps/css'
import { applyLayout, applyTheme, autoResize, connect } from '@home-apps/sdk'

// The bridge without a framework: context, data, the host's theme and the @home-apps/css classes for where it stands.
const homemix = connect()
autoResize(homemix)

const ctx = await homemix.context()
applyTheme(ctx.theme)
applyLayout(ctx)
homemix.on('theme', (theme) => applyTheme(theme))

const counter = await homemix.data.get('counter', 'clicks')
let clicks = counter?.value ?? 0
const app = document.querySelector('#app')

if (ctx.widget) {
  // A widget: the number, and a click opens the page.
  app.innerHTML = `<section class="hm-card hm-stack hm-clickable" role="link" tabindex="0">
    <span class="hm-muted">Kliknięcia</span><span class="hm-big"></span></section>`
  app.querySelector('.hm-big').textContent = String(clicks)
  app.querySelector('section').onclick = () => homemix.open()
} else {
  app.className = 'hm-stack'
  app.innerHTML = `<h1 class="hm-title"></h1><div class="hm-row"><span class="hm-big"></span>
    <button class="hm-button">Kliknij</button></div>`
  app.querySelector('h1').textContent = ctx.user.name
  const render = () => (app.querySelector('.hm-big').textContent = String(clicks))
  app.querySelector('button').onclick = async () => {
    clicks += 1
    render()
    await homemix.data.put('counter', 'clicks', clicks)
  }
  render()
}
