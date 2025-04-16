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
    options.UseSqlite("Data Source=recipenest.db", sqliteOptions =>
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
        if (!dbContext.Chefs.Any()) // Check if there are no chefs
{
    logger.LogInformation("Seeding database with initial chef...");
    var chef = new Chef
    {
        Name = "Default Chef",
        Surname = "Smith",
        Email = "chef@example.com",
        PasswordHash = "hashedpassword", // In a real app, hash a password properly
        Rating = 0.0F
    };
    dbContext.Chefs.Add(chef);
    await dbContext.SaveChangesAsync();
    logger.LogInformation("Chef seeded successfully.");
}

if (!dbContext.Recipes.Any()) // Check if there are no recipes
{
    logger.LogInformation("Seeding database with initial recipes...");
    var chef = await dbContext.Chefs.FirstAsync(); // Get the first chef
    dbContext.Recipes.AddRange(
        new Recipe { Id = 11, Title = "Recipe 1", Description = "Delicious dish", Image = "", Likes = 0, Dislikes = 0, ChefId = chef.Id },
        new Recipe { Id = 12, Title = "Recipe 2", Description = "Tasty treat", Image = "", Likes = 0, Dislikes = 0, ChefId = chef.Id }
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
