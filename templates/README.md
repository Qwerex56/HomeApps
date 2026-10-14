# Templates

```bash
dotnet new install ./templates     # from the repository root
```

| Template | Run from | Example |
|---|---|---|
| `homeapps-service` | repository root | `dotnet new homeapps-service -n Accounts [--sample]` |
| `homeapps-command` | `src/<Service>` | `dotnet new homeapps-command -n DeactivateUser --service Accounts --feature Users [--returns Guid] [--method Post\|Put\|Patch\|Delete]` |
| `homeapps-query` | `src/<Service>` | `dotnet new homeapps-query -n GetUser --service Accounts --feature Users` |

`./verify.sh` proves they still work (CI runs it). The full guide is [docs/how-a-service-is-built.md](../docs/how-a-service-is-built.md).
