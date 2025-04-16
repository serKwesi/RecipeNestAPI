using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecipeNestAPI.Data;
using RecipeNestAPI.Models;
using System.Text;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure CORS to allow the Netlify frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://your-netlify-app.netlify.app") // Replace with your Netlify URL
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure SQLite with BusyTimeout
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=recipenest.db;Pooling=True;BusyTimeout=5000", sqliteOptions =>
    {
        sqliteOptions.CommandTimeout(60);
    }));

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "RecipeNestAPI",
            ValidAudience = "RecipeNestFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                Environment.GetEnvironmentVariable("JWT_SECRET") ?? "YourSuperSecretKey1234567890!@#$%"))
        };
    });

var app = builder.Build();

// Apply CORS policy
app.UseCors("AllowFrontend");

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        dbContext.Database.EnsureCreated();
        if (!dbContext.Recipes.Any())
        {
            logger.LogInformation("Seeding database with initial recipes...");
            dbContext.Recipes.AddRange(
                new Recipe { Id = 11, Title = "Recipe 1", Description = "Delicious dish", Image = "", Likes = 0, Dislikes = 0, ChefId = 1 },
                new Recipe { Id = 12, Title = "Recipe 2", Description = "Tasty treat", Image = "", Likes = 0, Dislikes = 0, ChefId = 1 }
            );
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Database seeded successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed database.");
        throw;
    }
}

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
