namespace Jarvis.Api.Tests;

/// <summary>One gateway, database and set of sources for all the tests: they run one after another.</summary>
[CollectionDefinition(nameof(JarvisCollection))]
public sealed class JarvisCollection : ICollectionFixture<JarvisFactory>;
