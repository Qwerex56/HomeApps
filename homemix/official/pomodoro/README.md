# Pomodoro (official HomeMix add-on)

A shared focus and break timer: one timer for the household (or your account), visible on its page, as a widget on
the household page, the wall tablet and "Moje aplikacje". Each person's finished sessions are counted privately.
Admins set the lengths (permission `settings.write`). The end of a focus is pushed to everyone's devices, and JARVIS
can read the timer (summary) and your stats (tool `pomodoro_stats`) when the installer allowed it.

Written with `@homemix/vue` as the living example of docs/homemix.md, "Building one".

```bash
cd src/Web/homemix && npm install && npm run build   # the SDK and @homemix/vue, once
cd homemix/official/pomodoro
npm install
npm run dev     # a fake HomeApps at http://localhost:5180
npm run pack    # addon.zip and its SHA-256
```
