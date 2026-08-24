using System.Text;
using AutoMapper;
using EduCore.Data;
using EduCore.Exceptions;
using EduCore.Repositories;
using EduCore.Repositories.Interfaces;
using EduCore.Services;
using EduCore.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<SchoolIsolationSaveChangesInterceptor>();
builder.Services.AddScoped<ICurrentSchoolAccessor, HttpContextCurrentSchoolAccessor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(
            sp.GetRequiredService<AuditSaveChangesInterceptor>(),
            sp.GetRequiredService<SchoolIsolationSaveChangesInterceptor>()));

builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();


if (app.Environment.IsDevelopment())
    app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();

await SuperAdminSeeder.SeedAsync(app.Services, app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

