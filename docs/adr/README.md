# Architecture decision records

Short records of decisions that shape every service. Add a new numbered file when a decision changes; don't rewrite old ones.

| # | Decision |
|---|---|
| [0001](0001-clean-architecture-per-service.md) | Clean architecture with a four-project split per service |
| [0002](0002-typed-results-instead-of-exceptions.md) | Typed results instead of exceptions for business rules |
| [0003](0003-domain-events-and-the-outbox.md) | Domain events dispatched before commit, integration messages through the outbox |
| [0004](0004-repositories-and-unit-of-work.md) | One repository per aggregate, the DbContext as unit of work, saving in a pipeline behavior |
| [0005](0005-authentication-by-default.md) | Every endpoint authenticated by default, tokens validated with the Auth service's public keys |
| [0006](0006-shared-kit-and-templates.md) | A shared kit plus `dotnet new` templates instead of copied code |
