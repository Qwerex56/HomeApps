// The Auth hosts share one RabbitMQ: two running at once would take each other's messages.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
