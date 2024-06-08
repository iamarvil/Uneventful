## Basic Usage

```
builder.Services
    .AddEventStore(
        "some-domain",
        (eventStoreBuilder) => {
            eventStoreBuilder
                .RegisterDomainEvents(Constants.DomainName, [ typeof(SomeEvent), typeof(SomeOtherEvent) ])
                .UseInMemory("some-domain");
        }
    )
    .AddAggregateRepository(); // <-- Add this line
```