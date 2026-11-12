# Pogoda (official HomeMix add-on)

The weather now and for seven days where the household is, from [Open-Meteo](https://open-meteo.com) (free, no key).
An Admin picks the place once (permission `place.write`); everyone sees the page and the widget on the household
page, the wall tablet and "Moje aplikacje". The frame asks Open-Meteo itself every 15 minutes: the only addresses it
may reach are the two in `homemix.json` `network`, which the installer accepts on the consent screen.

Written with `@homemix/vue` as the second example of docs/homemix.md, "Building one".

```bash
cd src/Web/homemix && npm install && npm run build   # the SDK and @homemix/vue, once
cd homemix/official/pogoda
npm install
npm run dev     # a fake HomeApps at http://localhost:5180
npm run pack    # addon.zip and its SHA-256
```
