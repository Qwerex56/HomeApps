# HomeMix official store listing

`addons.json` is the quick listing of **official** add-ons: repositories the HomeApps team keeps, which every
HomeApps instance shows in its store as "Oficjalne" (docs/homemix.md, "The store"). Anyone else submits their add-on
from the store of an instance; that needs no change here.

Nothing is uploaded here: each instance reads the repository's latest release, the SHA-256 of its `addon.zip` and its
README straight from GitHub, about once an hour.

```json
{
  "addons": [
    { "repository": "https://github.com/homeapps/your-addon" }
  ]
}
```

The repository needs a release with `addon.zip` (the `create-homemix` template's workflow makes one on a `v*` tag).
The add-ons built into the HomeMix image (`homemix/official`) are not listed here; one with the same id wins.
