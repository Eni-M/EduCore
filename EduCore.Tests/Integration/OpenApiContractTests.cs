using Xunit;

namespace EduCore.Tests.Integration;

// openapi.json at the repository root is the API's contract, and the front end generates its
// TypeScript types from it (npm run gen:api). This test regenerates the document from the running
// app and fails when the committed copy no longer matches, so a change to an endpoint or a DTO
// cannot reach the front end unnoticed. To accept a deliberate change:
//
//   UPDATE_OPENAPI=1 dotnet run --project EduCore.Tests/EduCore.Tests.csproj
[Collection(ApiCollection.Name)]
public sealed class OpenApiContractTests(ApiFixture api)
{
    [Fact]
    public async Task The_committed_openapi_document_matches_the_api()
    {
        var current = Normalize(await api.App.CreateClient()
            .GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));
        var path = Path.Combine(Root(), "openapi.json");

        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI") == "1")
            await File.WriteAllTextAsync(path, current, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(path), "openapi.json is missing; run the tests once with UPDATE_OPENAPI=1.");
        Assert.True(Normalize(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)) == current,
            "openapi.json is out of date with the API. Rerun with UPDATE_OPENAPI=1, commit it, "
            + "then regenerate the front end's types (npm run gen:api).");
    }

    private static string Normalize(string json) => json.Replace("\r\n", "\n").TrimEnd() + "\n";

    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EduCore.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("EduCore.csproj not found.");
    }
}
