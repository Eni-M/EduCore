using EduCore.Helpers;
using EduCore.Services.Assignment;
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

DotEnvFile.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
DotEnvFile.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

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

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

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

builder.Services.AddAuthorization(options => options.AddEduCorePolicies());

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    app.MapOpenApi();

    await using (var scope = app.Services.CreateAsyncScope())
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

await SuperAdminSeeder.SeedAsync(app.Services, app.Configuration);

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();

    app.UseHsts();
}
app.UseCors(FrontendCors);
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

var api = app.MapGroup("/api")
    .RequireAuthorization()
    .AddEndpointFilter<AccountStatusEndpointFilter>()
    .AddEndpointFilter<ValidationEndpointFilter>();

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
