// Both hosts share one RabbitMQ: two fixtures at once would take each other's messages.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
