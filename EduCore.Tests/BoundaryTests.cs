using System.Text.RegularExpressions;
using Xunit;

namespace EduCore.Tests;

// The escape hatches out of school isolation, held to an allow-list so their number cannot grow
// without someone noticing. A new entry is a review question -- "why does this read cross the
// school boundary?" -- answered by adding the file here, not a reason to delete the test.
public class BoundaryTests
{
    // Reads that legitimately span schools: national statistics, a guardian's children across
    // schools, the digests, a transcript that follows a student between schools.
    private static readonly HashSet<string> MayBypassSchoolFilter =
    [
        "Repositories/AcademicYearRepository.cs",
        "Repositories/DirectorateRepository.cs",
        "Repositories/GradingPeriodRepository.cs",
        "Repositories/GuardianRepository.cs",
        "Repositories/LocationRepository.cs",
        "Repositories/PlatformStatsRepository.cs",
        "Repositories/Statistics/DemographicsStatisticsRepository.cs",
        "Repositories/Statistics/StatisticsRepositoryBase.cs",
        "Repositories/Statistics/StatisticsSnapshotRepository.cs",
        "Repositories/Statistics/TeacherStatisticsRepository.cs",
        "Repositories/StudentAcademicRecordRepository.cs",
        "Repositories/StudentRepository.cs",
        "Repositories/TeacherRepository.cs",
        "Services/Email/GuardianDigestService.cs",
        "Services/Email/ScheduleDigestService.cs",
        "Data/StatisticsInvalidationInterceptor.cs",
        "Data/QueryFilters.cs",
    ];

    // ExecuteUpdate/ExecuteDelete skip the save interceptors, so they skip the write-side scope
    // check and the audit log. Each of these touches only rows it has already scoped itself.
    private static readonly HashSet<string> MayWriteInBulk =
    [
        "Data/StatisticsInvalidationInterceptor.cs",
        "Repositories/MessagingRepository.cs",
        "Repositories/PasswordResetTokenRepository.cs",
        "Repositories/TwoFactorChallengeRepository.cs",
        // Retention deletes by age across schools, by design; it is the scheduled purge.
        "Services/RetentionBackgroundService.cs",
    ];

    [Fact]
    public void Only_allow_listed_files_bypass_the_school_filter() =>
        AssertOnlyIn(new Regex(@"IgnoreQueryFilters\s*\("), MayBypassSchoolFilter);

    [Fact]
    public void Only_allow_listed_files_write_around_the_interceptors() =>
        AssertOnlyIn(new Regex(@"\.Execute(Update|Delete)(Async)?\s*\("), MayWriteInBulk);

    // A plain IgnoreQueryFilters() turns off every filter there is or ever will be.
    [Fact]
    public void Bypasses_name_the_filter_they_skip() =>
        AssertOnlyIn(new Regex(@"IgnoreQueryFilters\s*\(\s*\)"), []);

    // Statistics is a module: only its own services and endpoints read its repositories, so a
    // change in how statistics are stored cannot ripple into messaging or marks.
    [Fact]
    public void Only_the_statistics_module_uses_statistics_repositories()
    {
        var root = RepositoryRoot();
        var offenders = SourceFiles(root)
            .Where(f => !f.Contains("/Statistics/") && f != "Program.cs")
            .Where(f => Regex.IsMatch(File.ReadAllText(Path.Combine(root, f)), @"using EduCore\.Repositories\.Statistics"))
            .ToList();

        Assert.True(offenders.Count == 0, "Outside the statistics module:\n  " + string.Join("\n  ", offenders));
    }

    private static void AssertOnlyIn(Regex pattern, HashSet<string> allowed)
    {
        var root = RepositoryRoot();
        var files = SourceFiles(root).ToList();
        Assert.True(files.Count > 100, $"Only {files.Count} source files found; the scan is not reading the project.");

        var offenders = files
            .Where(f => !allowed.Contains(f) && pattern.IsMatch(File.ReadAllText(Path.Combine(root, f))))
            .ToList();

        Assert.True(offenders.Count == 0, "Not on the allow-list:\n  " + string.Join("\n  ", offenders));
    }

    private static IEnumerable<string> SourceFiles(string root) =>
        new[] { "Repositories", "Services", "Data", "Endpoints", "Security" }
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(file => !file.StartsWith("Data/Migrations"));

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EduCore.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("EduCore.csproj not found above the test binary.");
    }
}
