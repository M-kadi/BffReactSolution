using JwtApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Infrastructure: EF + Identity
// -----------------------------

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// -----------------------------
// JWT (API protection)
// -----------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = key,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// -----------------------------
// Swagger
// -----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MvcJwtStudents API", Version = "v1" });
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Put ONLY your JWT token here (without 'Bearer ' prefix).",
        Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
    };
    c.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtSecurityScheme, Array.Empty<string>() } });
});

//builder.Services.AddAuthorization();
//builder.Services.AddControllers();
// Allow React dev server (Vite default at 5173) to call the BFF with credentials.
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:5173", "https://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});

var app = builder.Build();

// -----------------------------
// Seed DB: roles, users, test data
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
    async Task EnsureRole(string r) { if (!await roleMgr.RoleExistsAsync(r)) await roleMgr.CreateAsync(new IdentityRole(r)); }
    await EnsureRole("AdminRole"); await EnsureRole("StudentRole");

    var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
    async Task EnsureUser(string u, string p, string role, string? email = null)
    {
        var user = await userMgr.FindByNameAsync(u);
        if (user is null)
        {
            user = new ApplicationUser { UserName = u, Email = email ?? $"{u}@local" };
            await userMgr.CreateAsync(user, p);
            await userMgr.AddToRoleAsync(user, role);
        }
    }
    await EnsureUser("admin", "Admin123!", "AdminRole");
    await EnsureUser("student", "Student123!", "StudentRole");

    if (!db.Students.Any())
        db.Students.AddRange(new Student { stName = "Alice", stAddress = "Street 1" }, new Student { stName = "Bob", stAddress = "Street 2" });
    if (!db.Teachers.Any())
        db.Teachers.AddRange(new Teacher { Name = "Dr Smith", Address = "A" }, new Teacher { Name = "Prof Jones", Address = "B" });
    db.SaveChanges();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

//app.UseSwagger();
//app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MvcJwtStudents API v1"));

// -----------------------------
// Middleware order (important)
// -----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MvcJwtStudents API v1"));
}

app.MapControllers();

app.MapGet("/", () => "JWT API running");
app.Run();


/***
API
https://localhost:54451/swagger/index.html

UI React
https://localhost:54461/


{
  "username": "admin",
  "password": "Admin123!"
}

**/