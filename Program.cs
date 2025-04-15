using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecipeNestAPI.Data;
using RecipeNestAPI.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=recipenest.db"));

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKey1234567890!@#$%"))
        };
    });

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated(); // Create database if it doesn't exist

    if (!dbContext.Recipes.Any())
    {
        dbContext.Recipes.AddRange(
            new Recipe { Id = 11, Title = "Recipe 1", Description = "Delicious dish", Image = "", Likes = 0, Dislikes = 0, ChefId = 1 },
            new Recipe { Id = 12, Title = "Recipe 2", Description = "Tasty treat", Image = "", Likes = 0, Dislikes = 0, ChefId = 1 }
        );
        dbContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();