using HomeApps.BuildingBlocks.Domain;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class DomainTests
{
    private sealed record Renamed(Guid Id) : IDomainEvent;

    private sealed class Thing : AggregateRoot<Guid>
    {
        public Thing(Guid id) : base(id) { }

        public void Rename() => Raise(new Renamed(Id));
    }

    private sealed class OtherThing(Guid id) : Entity<Guid>(id);

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return amount;
            yield return currency;
        }
    }

    [Fact]
    public void Entities_with_the_same_type_and_id_are_equal()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new Thing(id), new Thing(id));
        Assert.True(new Thing(id) == new Thing(id));
        Assert.NotEqual<Entity<Guid>>(new Thing(id), new OtherThing(id));
    }

    [Fact]
    public void Value_objects_compare_by_value()
    {
        Assert.Equal(new Money(5, "PLN"), new Money(5, "PLN"));
        Assert.NotEqual(new Money(5, "PLN"), new Money(5, "EUR"));
        Assert.Equal(new Money(5, "PLN").GetHashCode(), new Money(5, "PLN").GetHashCode());
    }

    [Fact]
    public void Aggregate_collects_and_clears_domain_events()
    {
        var thing = new Thing(Guid.NewGuid());

        thing.Rename();
        thing.Rename();

        Assert.Equal(2, thing.DomainEvents.Count);
        thing.ClearDomainEvents();
        Assert.Empty(thing.DomainEvents);
    }

    [Fact]
    public void NotFound_error_has_a_snake_case_code()
    {
        var error = NotFoundError.For<OtherThing>(42);

        Assert.Equal("other_thing.not_found", error.Code);
        Assert.Equal("OtherThing '42' was not found.", error.Message);
    }
}
