using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Amazon.S3;
using AutoMapper;
using EduCore.Data;
using EduCore.Endpoints;
using EduCore.Observability;
using EduCore.Endpoints.Statistics;
using EduCore.Exceptions;
using EduCore.Helpers;
using EduCore.Localization;
using EduCore.Models;
using EduCore.Repositories;
using EduCore.Repositories.Interfaces;
using EduCore.Repositories.Statistics;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Security;
using EduCore.Security.Encryption;
using EduCore.Services;
using EduCore.Services.Account;
using EduCore.Services.Assignment;
using EduCore.Services.Assignments;
using EduCore.Services.Email;
using EduCore.Services.Interfaces;
using EduCore.Services.Statistics;
using EduCore.Services.Statistics.Interfaces;
using EduCore.Services.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

DotEnvFile.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
DotEnvFile.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

var builder = WebApplication.CreateBuilder(args);

// Kestrel hardening. Both of these matter even behind nginx: nginx is the edge in the compose
// stack, but a local `dotnet run`, a future direct deployment, or anything that reaches the
// container on port 8080 has no proxy in front of it at all.
builder.WebHost.ConfigureKestrel(kestrel =>
{
    // Student documents are capped at 10 MB by Storage:MaxFileSizeBytes, which is enforced
    // after the body has been read. This refuses an oversized body at the transport instead,
    // so a client cannot make the server buffer 30 MB (the framework default) to be told no.
    // Kept above the application limit so the application stays the component that decides.
    kestrel.Limits.MaxRequestBodySize = 12 * 1024 * 1024;

    // Do not name the server software in every response.
    kestrel.AddServerHeader = false;
});

// Metrics and traces. Returns the resolved options so the metrics endpoint can be mapped below.
var observability = builder.AddEduCoreObservability();

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Backing store for the account-status cache below. Nothing else uses IMemoryCache today; if
// something does later, note that entries here are bounded by their 60-second lifetime rather
// than by a configured SizeLimit -- setting one would require every other caller to declare a
// Size, which is a trap worth avoiding.
builder.Services.AddMemoryCache();

// Removes the per-request Users SELECT that every authenticated /api call used to make, without
// making revocation eventually-consistent: see AccountStatusCache and AccountStatusInvalidationInterceptor.
builder.Services.AddScoped<IAccountStatusCache, AccountStatusCache>();

builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<SchoolScopeInterceptor>();
builder.Services.AddScoped<StatisticsInvalidationInterceptor>();
builder.Services.AddScoped<AccountStatusInvalidationInterceptor>();
builder.Services.AddScoped<AccountMailInterceptor>();

var defaultConnectionString = builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration["DATABASE_URL"]
    ?? builder.Configuration["ConnectionStrings:Default"];

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(defaultConnectionString, npgsql =>
        {
            // A transient failure here is not hypothetical: a managed database that scales down
            // when idle drops its connections, so the first request after a quiet spell fails
            // for no reason the user can act on. Retry those, and only those -- EF classifies
            // which errors are transient, and never retries one that might have committed.
            //
            // This works because the application opens no explicit transactions anywhere (a
            // property worth keeping): the execution strategy can safely replay a whole
            // operation. If a BeginTransaction is ever added, it has to be wrapped in
            // strategy.ExecuteAsync or EF will refuse it at run time.
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);

            // A query that runs longer than this is not going to finish usefully -- it is going
            // to hold a connection while a user stares at a spinner. Fail it and free the
            // connection instead. Above the slowest legitimate screen (the statistics overview
            // on a cold snapshot), well below any human's patience.
            npgsql.CommandTimeout(30);
        })
        .AddInterceptors(
            sp.GetRequiredService<AuditSaveChangesInterceptor>(),
            sp.GetRequiredService<SchoolScopeInterceptor>(),
            sp.GetRequiredService<StatisticsInvalidationInterceptor>(),
            sp.GetRequiredService<AccountStatusInvalidationInterceptor>(),
            sp.GetRequiredService<AccountMailInterceptor>()));

builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentAcademicRecordRepository, StudentAcademicRecordRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IDirectorateRepository, DirectorateRepository>();
builder.Services.AddScoped<IRegionalAdminRepository, RegionalAdminRepository>();
builder.Services.AddScoped<IStateAdminRepository, StateAdminRepository>();
builder.Services.AddScoped<IGuardianRepository, GuardianRepository>();
builder.Services.AddScoped<ISchoolRepository, SchoolRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
builder.Services.AddScoped<IGradingPeriodRepository, GradingPeriodRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ITeacherSubjectRepository, TeacherSubjectRepository>();
builder.Services.AddScoped<IClassSubjectTeacherRepository, ClassSubjectTeacherRepository>();
builder.Services.AddScoped<IGradeCurriculumRepository, GradeCurriculumRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IDiaryEntryRepository, DiaryEntryRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IMarkRepository, MarkRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<ISchoolingHistoryRepository, SchoolingHistoryRepository>();
builder.Services.AddScoped<IStudentDocumentRepository, StudentDocumentRepository>();
builder.Services.AddScoped<IStudentMedicalRecordRepository, StudentMedicalRecordRepository>();
builder.Services.AddScoped<INoticeRepository, NoticeRepository>();
builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Field-level encryption for national identifiers at rest. Requires a 32-byte key, supplied
// out of band like Jwt:Key (never committed). Generate one with: openssl rand -base64 32
var encryptionKey = builder.Configuration["Encryption:Key"];
byte[] encryptionKeyBytes;
try { encryptionKeyBytes = string.IsNullOrWhiteSpace(encryptionKey) ? [] : Convert.FromBase64String(encryptionKey); }
catch (FormatException) { encryptionKeyBytes = []; }

if (encryptionKeyBytes.Length != 32)
    throw new InvalidOperationException(
        "Encryption:Key must be a base64-encoded 32-byte key. Set Encryption__Key in .env " +
        "(see .env.example) or as an environment variable. Generate one with: openssl rand -base64 32");

builder.Services.AddSingleton<IFieldEncryptor>(new AesFieldEncryptor(encryptionKeyBytes));

builder.Services.AddOptions<GradingOptions>()
    .Bind(builder.Configuration.GetSection(GradingOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<GradingOptions>, GradingOptionsValidator>();

builder.Services.AddOptions<PrivacyOptions>()
    .Bind(builder.Configuration.GetSection(PrivacyOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PrivacyOptions>, PrivacyOptionsValidator>();

builder.Services.AddOptions<AssignmentOptions>()
    .Bind(builder.Configuration.GetSection(AssignmentOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AssignmentOptions>, AssignmentOptionsValidator>();

builder.Services.AddOptions<AcademicCalendarOptions>()
    .Bind(builder.Configuration.GetSection(AcademicCalendarOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AcademicCalendarOptions>, AcademicCalendarOptionsValidator>();

// Where uploaded files are kept, how big they may be, and which content types are allowed.
// Storage:Provider picks the backend; everything above IFileStorage is unaware of which one runs.
builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();

var storageOptions = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
                     ?? new StorageOptions();

if (storageOptions.UsesS3)
{
    // Credentials come from the ambient AWS chain (environment, or the instance role in
    // production), never from configuration -- there is nothing here to leak into source control.
    builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
        Amazon.RegionEndpoint.GetBySystemName(storageOptions.S3Region)));
    builder.Services.AddSingleton<IFileStorage, S3FileStorage>();
}
else
{
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
}

// How long an emailed reset link lives, and where it points.
// How stale a statistics snapshot may get before the next read recomputes it.
builder.Services.AddOptions<StatisticsSnapshotOptions>()
    .Bind(builder.Configuration.GetSection(StatisticsSnapshotOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<StatisticsSnapshotOptions>, StatisticsSnapshotOptionsValidator>();

builder.Services.AddOptions<PasswordResetOptions>()
    .Bind(builder.Configuration.GetSection(PasswordResetOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PasswordResetOptions>, PasswordResetOptionsValidator>();

builder.Services.AddOptions<SessionCookieOptions>()
    .Bind(builder.Configuration.GetSection(SessionCookieOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<SessionCookieOptions>, SessionCookieOptionsValidator>();
builder.Services.AddSingleton<ISessionCookies, SessionCookies>();

builder.Services.AddScoped<IElementaryAssignmentSync, ElementaryAssignmentSync>();
builder.Services.AddScoped<IAcademicCalendar, AcademicCalendar>();

builder.Services.AddScoped<IGradingCalculator, GradingCalculator>();
builder.Services.AddScoped<ICellSuppressor, CellSuppressor>();
builder.Services.AddScoped<IStatisticsPeriodResolver, StatisticsPeriodResolver>();
builder.Services.AddScoped<IStatisticsScopeResolver, StatisticsScopeResolver>();
builder.Services.AddScoped<IStatisticsSnapshotRepository, StatisticsSnapshotRepository>();
builder.Services.AddScoped<IStatisticsSnapshotService, StatisticsSnapshotService>();

builder.Services.AddScoped<IStudentStatisticsRepository, StudentStatisticsRepository>();
builder.Services.AddScoped<IEnrollmentStatisticsRepository, EnrollmentStatisticsRepository>();
builder.Services.AddScoped<IAttendanceStatisticsRepository, AttendanceStatisticsRepository>();
builder.Services.AddScoped<IAcademicStatisticsRepository, AcademicStatisticsRepository>();
builder.Services.AddScoped<ITeacherStatisticsRepository, TeacherStatisticsRepository>();
builder.Services.AddScoped<IDemographicsStatisticsRepository, DemographicsStatisticsRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAssignmentSubmissionRepository, AssignmentSubmissionRepository>();

builder.Services.AddScoped<IStudentStatisticsService, StudentStatisticsService>();
builder.Services.AddScoped<IEnrollmentStatisticsService, EnrollmentStatisticsService>();
builder.Services.AddScoped<IAttendanceStatisticsService, AttendanceStatisticsService>();
builder.Services.AddScoped<IAcademicStatisticsService, AcademicStatisticsService>();
builder.Services.AddScoped<ITeacherStatisticsService, TeacherStatisticsService>();
builder.Services.AddScoped<IClassStatisticsService, ClassStatisticsService>();
builder.Services.AddScoped<IDemographicsStatisticsService, DemographicsStatisticsService>();
builder.Services.AddScoped<IStatisticsOverviewService, StatisticsOverviewService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IAssignmentSubmissionService, AssignmentSubmissionService>();

builder.Services.AddScoped<IAccessGuard, AccessGuard>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IDirectorateService, DirectorateService>();
builder.Services.AddScoped<IRegionalAdminService, RegionalAdminService>();
builder.Services.AddScoped<IStateAdminService, StateAdminService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAcademicYearService, AcademicYearService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IClassSubjectTeacherService, ClassSubjectTeacherService>();
builder.Services.AddScoped<ICurriculumService, CurriculumService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// Mail transport. "Log" delivers nothing and writes what would have been sent -- fine for
// development, and a silent failure in production, so the choice is explicit configuration
// rather than an environment check. Password reset and the schedule digests are the only
// callers; both go through IEmailSender and neither knows which transport is registered.
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
    ?? new EmailOptions();

if (emailOptions.IsSmtp)
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
builder.Services.AddScoped<IScheduleDigestService, ScheduleDigestService>();
builder.Services.AddScoped<IGuardianDigestService, GuardianDigestService>();
builder.Services.AddHostedService<ScheduledEmailBackgroundService>();

// Account emails -- welcome, password changed -- are queued by AccountMailInterceptor from the
// write that causes them and sent here, off the request path. A singleton queue, because the
// interceptor writing to it is scoped and the sender draining it is a background service.
builder.Services.AddSingleton<IAccountMailQueue, AccountMailQueue>();
builder.Services.AddHostedService<AccountMailBackgroundService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IStudentAcademicHistoryService, StudentAcademicHistoryService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGuardianService, GuardianService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IDiaryEntryService, DiaryEntryService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IMarkService, MarkService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISchoolingHistoryService, SchoolingHistoryService>();
builder.Services.AddScoped<IStudentDocumentService, StudentDocumentService>();
builder.Services.AddScoped<IStudentMedicalRecordService, StudentMedicalRecordService>();
builder.Services.AddScoped<INoticeService, NoticeService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Which language the API answers in. The catalogue keys on the English message, so a language
// it does not carry simply falls through to English rather than failing.
builder.Services.AddSingleton<IMessageLocalizer, MessageLocalizer>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(Language.English);
    options.AddSupportedUICultures(Language.Supported);
    options.AddSupportedCultures(Language.Supported);

    // A signed-in user's choice travels as Accept-Language from the SPA. ?lang= is kept for
    // links and for testing a language without changing browser settings.
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider { QueryStringKey = "lang", UIQueryStringKey = "lang" },
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});
builder.Services.AddProblemDetails();

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 bytes. Set Jwt__Key in .env (see .env.example) " +
        "or as an environment variable. Generate one with: openssl rand -base64 32");

var sessionCookieName =
    builder.Configuration.GetSection(SessionCookieOptions.SectionName)["CookieName"] ?? "educore.session";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                    context.Token = context.Request.Cookies[sessionCookieName];

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Set a default policy that requires authentication but no specific role.
    // This ensures `.RequireAuthorization()` calls without a policy name
    // work correctly and don't trigger 403 responses.
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
    
    options.AddEduCorePolicies();
});

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// Rate limiting: per-user (by user id claim) otherwise per-IP token-bucket limiter.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Prefer authenticated user id partitioning, fallback to remote IP
        var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var partitionKey = !string.IsNullOrEmpty(userId)
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Login-specific stricter policy to prevent brute-force attempts. Partitioned by client
    // IP: a single fixed partition would make every failed login anywhere on the instance
    // share one 5/minute bucket, so one attacker -- or one class starting at the same time --
    // locks everybody out. The address is the one UseForwardedHeaders derived from a *trusted*
    // proxy, never a raw client-supplied X-Forwarded-For; see the forwarded-headers setup below.
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5,
            TokensPerPeriod = 5,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    // Password reset is a mail-sending endpoint reachable without any credential, so it is
    // rate-limited harder than login: the abuse to stop is flooding somebody else's inbox,
    // not guessing a secret. Same per-IP partition and trust story as LoginPolicy.
    options.AddPolicy("PasswordResetPolicy", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 3,
                TokensPerPeriod = 3,
                ReplenishmentPeriod = TimeSpan.FromMinutes(15),
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Add a Retry-After header when rejected
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        await Task.CompletedTask;
    };
});

var app = builder.Build();

// Schema migration is an explicit, separate step: `dotnet EduCore.dll --migrate` applies
// pending migrations and exits, non-zero if any fail. Doing it on every boot instead meant
// each replica raced the others to migrate on start, and a broken migration took the API
// down with it rather than failing a job someone can see and rerun. compose runs this once,
// to completion, before the API starts.
if (args.Contains("--migrate"))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    await migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    return;
}

// ...and the API refuses to serve traffic against a schema it does not match, rather than
// discovering the mismatch one failing query at a time.
await using (var startupScope = app.Services.CreateAsyncScope())
{
    var db = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();

    // MigrateOnStart exists for hosts that offer no separate migration step -- Render's
    // pre-deploy command is a paid feature, and without it there is nowhere else to put this.
    // Off by default, because the separate one-shot job in compose.yaml is the better shape and
    // remains the default one.
    //
    // ONLY SAFE ON A SINGLE INSTANCE. Two replicas starting together both migrate, and they race:
    // that is the specific defect the compose `migrate` service was introduced to remove. Before
    // scaling past one instance, turn this off and run migrations as their own step again.
    //
    // A failure here stops start-up, which is the intent: a deploy that cannot migrate should be
    // visibly dead rather than quietly serving against a schema it does not match.
    if (app.Configuration.GetValue<bool>("MigrateOnStart"))
    {
        app.Logger.LogInformation(
            "MigrateOnStart is set; applying any pending migrations before serving traffic.");

        await db.Database.MigrateAsync();
    }

    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

    if (pending.Count > 0)
        throw new InvalidOperationException(
            $"The database is missing {pending.Count} migration(s), starting with {pending[0]}. " +
            "Run `dotnet EduCore.dll --migrate` before starting the API -- compose does this in "
            + "the `migrate` service. On a single-instance host with no separate migration step, "
            + "set MigrateOnStart=true instead.");
}

if (app.Environment.IsDevelopment())
{
    app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    app.MapOpenApi();
}

await PlatformOwnerSeeder.SeedAsync(app.Services, app.Configuration);

// A stub mail transport in production is invisible from the outside: password resets appear to
// work, and nobody receives one. Say so at start-up rather than letting a user discover it.
if (!emailOptions.IsSmtp && !app.Environment.IsDevelopment())
    app.Logger.LogWarning(
        "Email:Provider is '{Provider}', so no mail is delivered -- password reset links and "
        + "schedule digests are written to this log instead. Set Email:Provider=Smtp with a host "
        + "and credentials to deliver them.",
        emailOptions.Provider);

// Honour the reverse proxy's scheme/IP headers so the app knows the real client protocol
// and address when TLS is terminated upstream (nginx / a load balancer).
//
// Trust is explicit. Clearing KnownNetworks/KnownProxies -- as this used to -- trusts every
// peer, so any client could set X-Forwarded-For and have it believed. That matters beyond
// logging: UseForwardedHeaders rewrites Connection.RemoteIpAddress, which is what both rate
// limiters partition on, so a forged header meant an attacker could mint a fresh quota per
// request.
//
// When the configuration names any proxy the trusted set becomes exactly that: the framework's
// implicit 127.0.0.0/8 default is dropped rather than added to, so the deployed trust boundary
// is the one written down and nothing else. With nothing configured that default stands, which
// keeps a bare `dotnet run` behind a local proxy working.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

var trustedNetworks = app.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
var trustedProxies = app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];

if (trustedNetworks.Length > 0 || trustedProxies.Length > 0)
{
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
}

foreach (var network in trustedNetworks)
    forwardedOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));

foreach (var proxy in trustedProxies)
    forwardedOptions.KnownProxies.Add(IPAddress.Parse(proxy));

app.UseForwardedHeaders(forwardedOptions);

app.UseRequestLocalization();
app.UseExceptionHandler();
// Off by default: TLS is terminated at the edge (nginx / the platform). Set
// ForceHttpsRedirect=true only when the API itself is the HTTPS endpoint.
if (app.Configuration.GetValue<bool>("ForceHttpsRedirect"))
{
    app.UseHttpsRedirection();

    app.UseHsts();
}
app.UseCors(FrontendCors);
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseAuthentication();
// Rate limiter middleware — run after authentication so per-user limits can be applied
app.UseRateLimiter();
app.UseAuthorization();

// Liveness only: no database, no auth, no rate limit. It must stay answerable while the
// database is down (a health check that fails with its dependencies turns one outage into a
// restart loop), and it must not be able to consume a client's request quota.
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).DisableRateLimiting();

// Counters and timings for whatever scrapes them. Placed after authentication so the option to
// require a signed-in caller actually applies.
app.MapEduCoreMetrics(observability);

app.MapAuthEndpoints();

var api = app.MapGroup("/api")
    .RequireAuthorization()
    .AddEndpointFilter<AccountStatusEndpointFilter>()
    .AddEndpointFilter<ValidationEndpointFilter>();

api.MapDashboardEndpoints();
api.MapSchoolEndpoints();
api.MapSubjectEndpoints();
api.MapLocationEndpoints();
api.MapAcademicYearEndpoints();
api.MapClassEndpoints();
api.MapCurriculumEndpoints();
api.MapNotificationEndpoints();
api.MapStudentEndpoints();
api.MapStudentRecordsEndpoints();
api.MapNoticeEndpoints();
api.MapCalendarEndpoints();
api.MapTeacherEndpoints();
api.MapLessonEndpoints();
api.MapAssignmentEndpoints();
api.MapAssignmentSubmissionEndpoints();
api.MapScheduleEndpoints();
api.MapGuardianEndpoints();
api.MapAccountTierEndpoints();
api.MapStatisticsEndpoints();

app.Run();
