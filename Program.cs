using EduCore.Grading;
using EduCore.Services.Assignment;
using EduCore.Services.Calendar;
using System.Text;
using AutoMapper;
using EduCore.Data;
using EduCore.Endpoints;
using EduCore.Endpoints.Statistics;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories;
using EduCore.Repositories.Interfaces;
using EduCore.Repositories.Statistics;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Interfaces;
using EduCore.Services.Statistics;
using EduCore.Services.Statistics.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// Before anything reads configuration: pull .env into the environment, so the
// secrets it holds are found by the environment-variables provider the default
// builder already installs. See DotEnvFile for why they are not in appsettings.
DotEnvFile.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<SchoolScopeInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(
            sp.GetRequiredService<AuditSaveChangesInterceptor>(),
            sp.GetRequiredService<SchoolScopeInterceptor>()));

builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IParentRepository, ParentRepository>();
builder.Services.AddScoped<ISchoolRepository, SchoolRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
builder.Services.AddScoped<IGradingPeriodRepository, GradingPeriodRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ITeacherSubjectRepository, TeacherSubjectRepository>();
builder.Services.AddScoped<IClassSubjectTeacherRepository, ClassSubjectTeacherRepository>();
builder.Services.AddScoped<IDiaryEntryRepository, DiaryEntryRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IMarkRepository, MarkRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Stateless and thread-safe, so one instance serves the whole app. Registering it here
// rather than newing it up inside UserService means hashing options are configured in one
// place and every password in the system goes through the same settings.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// The state's grading rules, and how small an aggregate may be before it is withheld. What
// counts as a valid configuration lives in the two validator classes, not here.
builder.Services.AddOptions<GradingOptions>()
    .Bind(builder.Configuration.GetSection(GradingOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<GradingOptions>, GradingOptionsValidator>();

builder.Services.AddOptions<PrivacyOptions>()
    .Bind(builder.Configuration.GetSection(PrivacyOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PrivacyOptions>, PrivacyOptionsValidator>();

// How many subjects a junior-high/high-school teacher may hold, and the default split of a
// new academic year into its two grading periods.
builder.Services.AddOptions<AssignmentOptions>()
    .Bind(builder.Configuration.GetSection(AssignmentOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AssignmentOptions>, AssignmentOptionsValidator>();

builder.Services.AddOptions<AcademicCalendarOptions>()
    .Bind(builder.Configuration.GetSection(AcademicCalendarOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AcademicCalendarOptions>, AcademicCalendarOptionsValidator>();

builder.Services.AddScoped<IElementaryAssignmentSync, ElementaryAssignmentSync>();
builder.Services.AddScoped<IAcademicCalendar, AcademicCalendar>();

builder.Services.AddScoped<IGradingCalculator, GradingCalculator>();
builder.Services.AddScoped<ICellSuppressor, CellSuppressor>();
builder.Services.AddScoped<IStatisticsPeriodResolver, StatisticsPeriodResolver>();
builder.Services.AddScoped<IStatisticsScopeResolver, StatisticsScopeResolver>();

builder.Services.AddScoped<IStudentStatisticsRepository, StudentStatisticsRepository>();
builder.Services.AddScoped<IEnrollmentStatisticsRepository, EnrollmentStatisticsRepository>();
builder.Services.AddScoped<IAttendanceStatisticsRepository, AttendanceStatisticsRepository>();
builder.Services.AddScoped<IAcademicStatisticsRepository, AcademicStatisticsRepository>();
builder.Services.AddScoped<ITeacherStatisticsRepository, TeacherStatisticsRepository>();
builder.Services.AddScoped<IDemographicsStatisticsRepository, DemographicsStatisticsRepository>();

builder.Services.AddScoped<IStudentStatisticsService, StudentStatisticsService>();
builder.Services.AddScoped<IEnrollmentStatisticsService, EnrollmentStatisticsService>();
builder.Services.AddScoped<IAttendanceStatisticsService, AttendanceStatisticsService>();
builder.Services.AddScoped<IAcademicStatisticsService, AcademicStatisticsService>();
builder.Services.AddScoped<ITeacherStatisticsService, TeacherStatisticsService>();
builder.Services.AddScoped<IClassStatisticsService, ClassStatisticsService>();
builder.Services.AddScoped<IDemographicsStatisticsService, DemographicsStatisticsService>();
builder.Services.AddScoped<IStatisticsOverviewService, StatisticsOverviewService>();

builder.Services.AddScoped<IAccessGuard, AccessGuard>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAcademicYearService, AcademicYearService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IClassSubjectTeacherService, ClassSubjectTeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IDiaryEntryService, DiaryEntryService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IMarkService, MarkService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var jwtKey = builder.Configuration["Jwt:Key"];

// HS256 needs at least 256 bits. Better to fail here than to sign tokens with a placeholder.
//
// The key is deliberately not in appsettings.json: that file is committed, and a
// signing key in the repository is a signing key everyone has. Set Jwt__Key in
// .env for local work, or in the environment for a deployment.
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 bytes. Set Jwt__Key in .env (see .env.example) " +
        "or as an environment variable. Generate one with: openssl rand -base64 32");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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

builder.Services.AddAuthorization(options => options.AddEduCorePolicies());

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    app.MapOpenApi();

    // Bring the database up to the current model on start-up.
    //
    // Development only, and deliberately so: applying migrations automatically is unsafe
    // where more than one instance can start at once, or where a migration is destructive
    // and wants a human looking at it. Deployments run "dotnet ef database update" as their
    // own step.
    //
    // The failure this prevents is a quiet one. A pulled branch that adds a column leaves
    // the model and the database disagreeing, and EF then names the new column in every
    // query it writes -- so an untouched screen starts answering 500 with "An unexpected
    // error occurred", and nothing points at the migration as the cause.
    await using (var scope = app.Services.CreateAsyncScope())
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

await SuperAdminSeeder.SeedAsync(app.Services, app.Configuration);

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();

    // Redirecting to https still leaves the first request of a session on http,
    // where it can be intercepted -- and every request after sign-in carries a
    // bearer token in a header. HSTS tells the browser never to try http again.
    app.UseHsts();
}
app.UseCors(FrontendCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

// Everything else requires a token and a password that is not pending a change.
var api = app.MapGroup("/api")
    .RequireAuthorization()
    .AddEndpointFilter<ValidationEndpointFilter>()
    .AddEndpointFilter<AccountStatusEndpointFilter>();

api.MapSchoolEndpoints();
api.MapSubjectEndpoints();
api.MapLocationEndpoints();
api.MapAcademicYearEndpoints();
api.MapClassEndpoints();
api.MapStudentEndpoints();
api.MapTeacherEndpoints();
api.MapLessonEndpoints();
api.MapStatisticsEndpoints();

app.Run();
