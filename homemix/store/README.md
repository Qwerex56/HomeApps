# HomeMix store index

`addons.json` lists the add-ons every HomeApps instance shows in its store (docs/homemix.md, "The store"). The store
only links to GitHub: nothing is uploaded here, and each instance reads the repository's latest release, the SHA-256
of its `addon.zip` and its README straight from GitHub.

To publish an add-on, open a pull request that adds its repository:

```json
{
  "addons": [
    { "repository": "https://github.com/you/your-addon" }
  ]
}
```

The repository needs a release with `addon.zip` (the `create-homemix` template's workflow makes one on a `v*` tag).
The official add-ons (`homemix/official`) are not listed here: they come inside the HomeMix image.
