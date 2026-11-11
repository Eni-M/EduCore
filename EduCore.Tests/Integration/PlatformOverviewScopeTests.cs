using System.Net;
using System.Text.Json;
using EduCore.Enums;
using EduCore.Models;
using Xunit;

namespace EduCore.Tests.Integration;

// A regional administrator's dashboard and statistics read the platform overview. Its student
// figures were narrowed to their city but its school count and per-school rows were not, so a
// region of four schools reported every school in the country.
[Collection(ApiCollection.Name)]
public sealed class PlatformOverviewScopeTests(ApiFixture api)
{
    [Fact]
    public async Task A_regional_admin_sees_only_the_schools_in_their_city()
    {
        var ct = TestContext.Current.CancellationToken;
        var cityId = Guid.CreateVersion7();
        var otherCityId = Guid.CreateVersion7();
        var inCity = Guid.CreateVersion7();
        var elsewhere = Guid.CreateVersion7();

        await using (var db = api.Database())
        {
            var state = new State { Id = Guid.CreateVersion7(), Name = $"State {Guid.NewGuid():N}" };
            db.States.Add(state);
            db.Cities.AddRange(
                new City { Id = cityId, Name = $"Here {Guid.NewGuid():N}", StateId = state.Id },
                new City { Id = otherCityId, Name = $"There {Guid.NewGuid():N}", StateId = state.Id });
            db.Schools.AddRange(
                new School { Id = inCity, Name = "Regional In City", Type = SchoolType.HighSchool, CityId = cityId },
                new School { Id = elsewhere, Name = "Regional Elsewhere", Type = SchoolType.HighSchool, CityId = otherCityId });
            await db.SaveChangesAsync(ct);
        }

        var admin = await api.UserAsync(UserRole.RegionalAdmin);
        var client = api.App.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiFixture.Token(admin, cityId: cityId));

        // The platform-overview endpoint is the ministry's; a regional admin reaches the same
        // service through their dashboard.
        var response = await client.GetAsync("/api/dashboard", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{(int)response.StatusCode} {body}");

        using var json = JsonDocument.Parse(body);
        var schools = json.RootElement.GetProperty("stats").EnumerateArray()
            .First(s => s.GetProperty("label").GetString() == "Schools")
            .GetProperty("value").GetString();
        var recent = json.RootElement.GetProperty("recent").EnumerateArray()
            .Select(r => r.GetProperty("title").GetString())
            .ToList();

        Assert.Equal("1", schools);
        Assert.Equal(["Regional In City"], recent);
    }
}
