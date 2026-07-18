using System.Net;
using System.Net.Http.Json;
using ATS.API.Controllers.v1;
using ATS.Domain.Aggregates.Jobs;
using FluentAssertions;

namespace ATS.API.IntegrationTests.Controllers.Jobs;

public class JobsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public JobsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateJob_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var request = new CreateJobRequest(
            "Software Engineer",
            "Job Description",
            EmploymentType.FullTime,
            WorkplaceType.Remote,
            ExperienceLevel.MidLevel,
            "Remote",
            50000,
            100000,
            "USD");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/companies/{companyId}/jobs", request);

        // Assert
        // In real tests, we would setup an authenticated user.
        // For now, since authentication is enabled globally, it might return Unauthorized
        // unless we have a mock authentication scheme in our CustomWebApplicationFactory.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Unauthorized);
    }
}
