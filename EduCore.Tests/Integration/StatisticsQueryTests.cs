using System.Net;
using System.Net.Http.Headers;
using EduCore.Enums;
using EduCore.Models;
using Xunit;

namespace EduCore.Tests.Integration;

// Statistics queries run against real PostgreSQL: a LINQ expression EF cannot translate, or SQL
// that fails on some data, only shows up there. Each list is asked for whole and paged.
[Collection(ApiCollection.Name)]
public sealed class StatisticsQueryTests(ApiFixture api)
{
    [Theory]
    [InlineData("/api/statistics/teachers/coverage")]
    [InlineData("/api/statistics/attendance/excessive-absences")]
    [InlineData("/api/statistics/attendance/excessive-absences?page=1&pageSize=20")]
    [InlineData("/api/statistics/students/by-age")]
    [InlineData("/api/statistics/students/by-age?page=1&pageSize=20")]
    [InlineData("/api/statistics/enrollment/capacity")]
    [InlineData("/api/statistics/enrollment/capacity?page=1&pageSize=20")]
    public async Task The_list_answers_for_a_school_with_a_teacher_who_has_no_marks(string url)
    {
        var ct = TestContext.Current.CancellationToken;
        var director = await api.UserAsync(UserRole.Directorate, api.SchoolA);

        // A teacher with no marks at all: the coverage percentage once divided by their count.
        var teacherUser = await api.UserAsync(UserRole.Teacher);
        var teacherId = Guid.CreateVersion7();
        await using (var db = api.Database())
        {
            db.Teachers.Add(new Teacher { Id = teacherId, UserId = teacherUser.Id, SchoolId = api.SchoolA });
            await db.SaveChangesAsync(ct);
        }

        var client = api.App.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFixture.Token(director, api.SchoolA));

        // Coverage lists assigned teachers only; naming this one puts them in scope directly.
        if (url.Contains("coverage"))
            url += $"?teacherId={teacherId}";

        var response = await client.GetAsync(url, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        if (url.Contains("coverage"))
            Assert.Contains(teacherId.ToString(), body);
    }
}
