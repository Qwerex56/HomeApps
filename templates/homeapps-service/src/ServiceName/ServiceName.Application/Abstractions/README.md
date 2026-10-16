# Ports

Interfaces the use cases need and Infrastructure implements: one repository per aggregate
(`IOrderRepository`, never a generic `IRepository<T>`), external services, and so on.
`IUnitOfWork`, `ICurrentUser`, `IDateTimeProvider` and `IEventBus` come from the shared kit.
