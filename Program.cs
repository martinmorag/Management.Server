using Management.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Plataforma.Models;
using DotNetEnv;
using System;
using Newtonsoft.Json.Converters; // For StringEnumConverter
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc; // Still expect UTC
        options.SerializerSettings.DateParseHandling = DateParseHandling.DateTime;

        // This one should be found because of 'using Newtonsoft.Json.Converters;'
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ManagementContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnectionString") 
    )
);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins("https://localhost:54121")
                                 .AllowAnyHeader()    // Allow all headers (e.g., Authorization, Content-Type)
                                 .AllowAnyMethod()    // Allow all HTTP methods (GET, POST, PUT, DELETE, OPTIONS etc.)
                                 .AllowCredentials(); // Allow cookies and authentication headers (like JWT Bearer tokens)
                      });
});


// AUTHENTICATION
builder.Services.AddIdentity<UsuarioIdentidad, IdentityRole<Guid>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 9;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedEmail = false; // Or true, if you implement email confirmation
})
.AddEntityFrameworkStores<ManagementContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/ingreso"; // Your custom login path
});

var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
