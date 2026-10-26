using System.Net;
using System.Net.Http.Json;
using EduCore.Data;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EduCore.Tests.Integration;

// The boundaries that unit tests over fakes cannot reach: they exist in the query filters, the
// save interceptors, the policies and the middleware, and only run against the real pipeline
// and a real database.
[Collection(ApiCollection.Name)]
public sealed class SecurityBoundaryTests(ApiFixture api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migrations_have_all_applied_to_a_fresh_database()
    {
        using var scope = api.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
    }

    [Fact]
    public async Task An_anonymous_caller_is_refused()
    {
        var response = await api.App.CreateClient().GetAsync("/api/students", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Read-side isolation: another school's student does not exist as far as this office knows.
    [Fact]
    public async Task An_office_cannot_read_another_schools_student()
    {
        var office = await api.UserAsync(UserRole.Directorate, api.SchoolA);
        var elsewhere = await api.StudentAsync(api.SchoolB, Unique());

        var response = await api.ClientFor(office, api.SchoolA).GetAsync($"/api/students/{elsewhere.Id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_office_reads_its_own_schools_student()
    {
        var office = await api.UserAsync(UserRole.Directorate, api.SchoolA);
        var mine = await api.StudentAsync(api.SchoolA, Unique());

        var response = await api.ClientFor(office, api.SchoolA).GetAsync($"/api/students/{mine.Id}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Role policies: the student list is the office's alone.
    [Fact]
    public async Task A_teacher_cannot_use_an_office_only_endpoint()
    {
        var teacher = await api.UserAsync(UserRole.Teacher);

        var response = await api.ClientFor(teacher, api.SchoolA).GetAsync("/api/students", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Write-side isolation: the interceptor behind every save, even when a read slipped through.
    [Fact]
    public async Task The_save_interceptor_refuses_another_schools_row_with_a_403()
    {
        var caller = new ApiFixture.Caller(UserRole.Directorate, [api.SchoolA]);
        await using var db = api.Database(caller, new SchoolScopeInterceptor(caller));

        db.Notifications.Add(new Notification
        {
            SchoolId = api.SchoolB, RecipientRole = UserRole.Directorate, Message = "x", CreatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<ForbiddenException>(() => db.SaveChangesAsync(Ct));
    }

    // A session cookie authenticates automatically, so a state change through it needs the CSRF
    // token too; a request carrying only the cookie is refused.
    [Fact]
    public async Task A_cookie_session_needs_the_csrf_token_to_change_anything()
    {
        var office = await api.UserAsync(UserRole.Directorate, api.SchoolA);
        var client = api.App.CreateClient();
        var session = ApiFixture.Token(office, api.SchoolA);

        async Task<HttpStatusCode> PostAsync(string cookies, string? header)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/read-all");
            request.Headers.Add("Cookie", cookies);
            if (header is not null)
                request.Headers.Add("X-CSRF-Token", header);
            return (await client.SendAsync(request, Ct)).StatusCode;
        }

        // No header at all.
        Assert.Equal(HttpStatusCode.Forbidden, await PostAsync($"educore.session={session}", null));

        // The attack a plain double-submit token falls to: whoever can plant a cookie sets the CSRF
        // cookie and the header to the same value of their choosing. Bound to the session, it fails.
        Assert.Equal(HttpStatusCode.Forbidden,
            await PostAsync($"educore.session={session}; educore.csrf=abc123", "abc123"));

        // The token minted for this session passes.
        var bound = api.App.Services.GetRequiredService<EduCore.Security.CsrfTokens>().For(session);
        Assert.Equal(HttpStatusCode.NoContent, await PostAsync($"educore.session={session}", bound));
    }

    // Sign-out and password changes move SessionsValidFrom; a token from before it is dead.
    [Fact]
    public async Task A_token_issued_before_the_sessions_ended_is_refused()
    {
        var office = await api.UserAsync(UserRole.Directorate, api.SchoolA);
        await using (var db = api.Database())
        {
            await db.Users.Where(u => u.Id == office.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.SessionsValidFrom, DateTime.UtcNow), Ct);
        }

        var client = api.App.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", ApiFixture.Token(office, api.SchoolA, issuedAt: DateTime.UtcNow.AddMinutes(-10)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/students", Ct)).StatusCode);
    }

    // Encrypted at rest, and still unique: the index works on the deterministic ciphertext.
    [Fact]
    public async Task Personal_numbers_are_stored_encrypted_and_stay_unique()
    {
        var number = Unique();
        var student = await api.StudentAsync(api.SchoolA, number);

        await using var db = api.Database();
        var stored = await db.Database
            .SqlQuery<string>($"SELECT \"PersonalNumber\" AS \"Value\" FROM \"Students\" WHERE \"Id\" = {student.Id}")
            .SingleAsync(Ct);

        Assert.StartsWith("enc:v1:", stored);
        Assert.DoesNotContain(number, stored);
        await Assert.ThrowsAsync<DbUpdateException>(() => api.StudentAsync(api.SchoolB, number));
    }

    // Health data about a child: every view leaves an audit row, filed under the school.
    [Fact]
    public async Task Reading_a_medical_record_is_audited()
    {
        var office = await api.UserAsync(UserRole.Directorate, api.SchoolA);
        var student = await api.StudentAsync(api.SchoolA, Unique());

        var response = await api.ClientFor(office, api.SchoolA).GetAsync($"/api/students/{student.Id}/medical", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = api.Database();
        Assert.True(await db.AuditLogs.AnyAsync(a =>
            a.UserId == office.Id && a.Action.Contains("StudentMedicalRecord") && a.Action.Contains(student.Id.ToString()), Ct));
    }

    // Errors are RFC 9457 problem details, and still carry the message the client reads.
    [Fact]
    public async Task Errors_are_problem_details()
    {
        var office = await api.UserAsync(UserRole.Directorate, api.SchoolA);

        var response = await api.ClientFor(office, api.SchoolA).GetAsync($"/api/students/{Guid.NewGuid()}", Ct);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(Ct);

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, int.Parse(body!["status"].ToString()!));
        Assert.True(body.ContainsKey("detail") && body.ContainsKey("traceId"));
    }

    // Reading is per person: one office user clearing the bell leaves a colleague's alone.
    [Fact]
    public async Task Notifications_are_read_per_user()
    {
        var alice = await api.UserAsync(UserRole.Directorate, api.SchoolA);
        var bob = await api.UserAsync(UserRole.Directorate, api.SchoolA);
        await using (var db = api.Database())
        {
            db.Notifications.Add(new Notification
            {
                SchoolId = api.SchoolA, RecipientRole = UserRole.Directorate, Message = "x", CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(Ct);
        }

        var aliceClient = api.ClientFor(alice, api.SchoolA);
        var before = await UnreadAsync(api.ClientFor(bob, api.SchoolA));

        await aliceClient.PostAsync("/api/notifications/read-all", null, Ct);

        Assert.Equal(0, await UnreadAsync(aliceClient));
        Assert.Equal(before, await UnreadAsync(api.ClientFor(bob, api.SchoolA)));
    }

    // The retention pass removes what has outlived its purpose and nothing newer.
    [Fact]
    public async Task Retention_removes_old_audit_rows_and_keeps_recent_ones()
    {
        var old = Guid.CreateVersion7();
        var recent = Guid.CreateVersion7();
        await using (var db = api.Database())
        {
            db.AuditLogs.AddRange(
                new AuditLog { Id = old, Action = "old", Timestamp = DateTime.UtcNow.AddYears(-3) },
                new AuditLog { Id = recent, Action = "recent", Timestamp = DateTime.UtcNow });
            await db.SaveChangesAsync(Ct);
        }

        var retention = api.App.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<EduCore.Services.RetentionBackgroundService>().Single();
        await retention.RunOnceAsync(Ct);

        await using var check = api.Database();
        Assert.False(await check.AuditLogs.AnyAsync(a => a.Id == old, Ct));
        Assert.True(await check.AuditLogs.AnyAsync(a => a.Id == recent, Ct));
    }

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<Dictionary<string, int>>("/api/notifications/unread-count", Ct))!["count"];

    private static string Unique() => Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
}
