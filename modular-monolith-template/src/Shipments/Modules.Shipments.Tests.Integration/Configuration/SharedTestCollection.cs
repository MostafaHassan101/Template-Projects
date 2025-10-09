namespace Modules.Shipments.Tests.Integration.Configuration;

[CollectionDefinition("ShippingTests")]
public class SharedTestCollection : ICollectionFixture<CustomWebApplicationFactory>;
