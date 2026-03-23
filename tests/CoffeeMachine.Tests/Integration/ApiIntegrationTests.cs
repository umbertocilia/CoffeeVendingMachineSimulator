using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CoffeeMachine.Tests.Integration;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_Should_Return_Ok_And_SeededProducts()
    {
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        payload.Should().NotBeNull();
        payload!.Should().Contain(x => x.Id == "espresso");
    }

    [Fact]
    public async Task AddCredit_Should_Return_Ok()
    {
        var response = await _client.PostAsJsonAsync("/api/credit/add", new { amount = 1.5, description = "integration-test" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("currentCredit");
    }

    private sealed record ProductResponse(string Id, string Name, decimal Price);
}
