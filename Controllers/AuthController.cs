using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeNestAPI.Data;
using RecipeNestAPI.Models;
using RecipeNestAPI.DTOs;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace RecipeNestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.Password != model.ConfirmPassword)
                return BadRequest(new { error = "Passwords do not match" });

            if (await _context.Chefs.AnyAsync(c => c.Email == model.Email))
                return BadRequest(new { error = "Email already exists" });

            if (model.Password.Length < 8)
                return BadRequest(new { error = "Password must be at least 8 characters" });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var chef = new Chef
            {
                Name = model.Name,
                Surname = model.Surname,
                Email = model.Email,
                PasswordHash = passwordHash
            };

            _context.Chefs.Add(chef);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chef registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var chef = await _context.Chefs.SingleOrDefaultAsync(c => c.Email == model.Email);
            if (chef == null || !BCrypt.Net.BCrypt.Verify(model.Password, chef.PasswordHash))
                return Unauthorized(new { error = "Invalid email or password" });

            var token = GenerateJwtToken(chef);
            return Ok(new { message = "Chef logged in successfully", token });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var chef = await _context.Chefs.FindAsync(int.Parse(userId));
            if (chef == null)
                return NotFound(new { error = "Chef not found" });

            return Ok(new
            {
                name = chef.Name,
                surname = chef.Surname,
                email = chef.Email,
                profilePicture = chef.ProfilePicture,
                bio = chef.Bio,
                description = chef.Description,
                instagram = chef.Instagram,
                linkedin = chef.Linkedin
            });
        }

        [Authorize]
        [HttpPatch("me")]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var chef = await _context.Chefs.FindAsync(int.Parse(userId));
            if (chef == null)
                return NotFound(new { error = "Chef not found" });

            if (!string.IsNullOrEmpty(model?.Email) && model.Email != chef.Email)
            {
                if (await _context.Chefs.AnyAsync(c => c.Email == model.Email))
                    return BadRequest(new { error = "Email already exists" });
                chef.Email = model.Email;
            }

            if (!string.IsNullOrEmpty(model?.Password))
            {
                if (model.Password.Length < 8)
                    return BadRequest(new { error = "Password must be at least 8 characters" });
                chef.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            _context.Chefs.Update(chef);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Account updated successfully" });
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(IFormCollection formData)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var chef = await _context.Chefs.FindAsync(int.Parse(userId));
            if (chef == null)
                return NotFound(new { error = "Chef not found" });

            if (formData.ContainsKey("name") && !string.IsNullOrEmpty(formData["name"]))
                chef.Name = formData["name"];
            if (formData.ContainsKey("surname") && !string.IsNullOrEmpty(formData["surname"]))
                chef.Surname = formData["surname"];
            if (formData.ContainsKey("bio") && !string.IsNullOrEmpty(formData["bio"]))
                chef.Bio = formData["bio"];
            if (formData.ContainsKey("description"))
                chef.Description = formData["description"];
            if (formData.ContainsKey("instagram") && !string.IsNullOrEmpty(formData["instagram"]))
                chef.Instagram = formData["instagram"];
            if (formData.ContainsKey("linkedin") && !string.IsNullOrEmpty(formData["linkedin"]))
                chef.Linkedin = formData["linkedin"];

            if (formData.Files["profilePicture"] != null)
            {
                var file = formData.Files["profilePicture"];
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();
                    chef.ProfilePicture = Convert.ToBase64String(imageBytes);
                }
            }

            _context.Chefs.Update(chef);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully" });
        }

        [Authorize]
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var chef = await _context.Chefs.FindAsync(int.Parse(userId));
            if (chef == null)
                return NotFound(new { error = "Chef not found" });

            _context.Chefs.Remove(chef);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account deleted successfully" });
        }

        [HttpGet("chefs")]
        public async Task<IActionResult> GetAllChefs()
        {
            var chefs = await _context.Chefs
                .Select(c => new
                {
                    chefId = c.Id,
                    Name = c.Name,
                    Surname = c.Surname,
                    ProfilePicture = c.ProfilePicture,
                    Bio = c.Bio,
                    TotalLikes = c.Recipes.Sum(r => r.Likes)
                })
                .ToListAsync();
            return Ok(chefs);
        }

        [HttpGet("chefs/{id}")]
        public async Task<IActionResult> GetChefById(int id)
        {
            var chef = await _context.Chefs
                .Include(c => c.Recipes)
                .ThenInclude(r => r.RecipeLikes)
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Surname,
                    c.ProfilePicture,
                    c.Bio,
                    c.Description,
                    c.Instagram,
                    c.Linkedin,
                    c.Email,
                    TotalLikes = c.Recipes.Sum(r => r.Likes),
                    Recipes = c.Recipes.Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.Description,
                        r.Image,
                        r.Likes,
                        r.Dislikes
                    })
                })
                .FirstOrDefaultAsync();

            if (chef == null) return NotFound(new { error = "Chef not found" });

            _context.ProfileViews.Add(new ProfileView { ChefId = id, ViewDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            return Ok(chef);
        }

        [Authorize]
        [HttpGet("recipes")]
        public async Task<IActionResult> GetChefRecipes()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipes = await _context.Recipes
                .Where(r => r.ChefId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Description,
                    r.Image,
                    r.Likes,
                    r.Dislikes
                })
                .ToListAsync();
            return Ok(recipes);
        }

        [HttpGet("recipes/all")]
        public async Task<IActionResult> GetAllRecipes()
        {
            
        var recipes = await _context.Recipes
        .Select(r => new
        {
            r.Id,
            r.Title,
            r.Description,
            r.Image,
            r.Likes,
            r.Dislikes
        })
        .ToListAsync();
    return Ok(recipes);
}

        [Authorize]
        [HttpPost("recipes")]
        public async Task<IActionResult> AddRecipe([FromBody] RecipeDto recipeDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipe = new Recipe
            {
                Title = recipeDto.Title,
                Description = recipeDto.Description,
                Image = recipeDto.Image,
                ChefId = userId,
                Likes = 0,
                Dislikes = 0
            };
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Recipe added", recipe.Id });
        }

        [Authorize]
        [HttpPut("recipes/{id}")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromBody] RecipeDto recipeDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.ChefId == userId);
            if (recipe == null) return NotFound(new { error = "Recipe not found" });

            recipe.Title = recipeDto.Title;
            recipe.Description = recipeDto.Description;
            recipe.Image = recipeDto.Image;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Recipe updated" });
        }

        [Authorize]
        [HttpDelete("recipes/{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.ChefId == userId);
            if (recipe == null) return NotFound(new { error = "Recipe not found" });

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Recipe deleted" });
        }

        [Authorize]
        [HttpPost("{recipeId}/like")]
        public async Task<IActionResult> LikeRecipe(int recipeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipe = await _context.Recipes.FindAsync(recipeId);
            if (recipe == null) return NotFound();

            var existingAction = await _context.RecipeLikes
                .FirstOrDefaultAsync(rl => rl.RecipeId == recipeId && rl.ChefId == userId);

            if (existingAction != null)
            {
                if (existingAction.IsLike) // Already liked, remove like
                {
                    _context.RecipeLikes.Remove(existingAction);
                    recipe.Likes--;
                }
                else // Disliked, switch to like
                {
                    existingAction.IsLike = true;
                    recipe.Dislikes--;
                    recipe.Likes++;
                }
            }
            else // No prior action, add like
            {
                _context.RecipeLikes.Add(new RecipeLikes
                {
                    RecipeId = recipeId,
                    ChefId = userId,
                    IsLike = true
                });
                recipe.Likes++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { recipe.Likes, recipe.Dislikes });
        }

        [Authorize]
        [HttpPost("{recipeId}/unlike")]
        public async Task<IActionResult> UnlikeRecipe(int recipeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipe = await _context.Recipes.FindAsync(recipeId);
            if (recipe == null) return NotFound();

            var existingAction = await _context.RecipeLikes
                .FirstOrDefaultAsync(rl => rl.RecipeId == recipeId && rl.ChefId == userId);

            if (existingAction != null && existingAction.IsLike)
            {
                _context.RecipeLikes.Remove(existingAction);
                recipe.Likes--;
                await _context.SaveChangesAsync();
            }

            return Ok(new { recipe.Likes, recipe.Dislikes });
        }

        [Authorize]
        [HttpPost("{recipeId}/dislike")]
        public async Task<IActionResult> DislikeRecipe(int recipeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipe = await _context.Recipes.FindAsync(recipeId);
            if (recipe == null) return NotFound();

            var existingAction = await _context.RecipeLikes
                .FirstOrDefaultAsync(rl => rl.RecipeId == recipeId && rl.ChefId == userId);

            if (existingAction != null)
            {
                if (!existingAction.IsLike) // Already disliked, remove dislike
                {
                    _context.RecipeLikes.Remove(existingAction);
                    recipe.Dislikes--;
                }
                else // Liked, switch to dislike
                {
                    existingAction.IsLike = false;
                    recipe.Likes--;
                    recipe.Dislikes++;
                }
            }
            else // No prior action, add dislike
            {
                _context.RecipeLikes.Add(new RecipeLikes
                {
                    RecipeId = recipeId,
                    ChefId = userId,
                    IsLike = false
                });
                recipe.Dislikes++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { recipe.Likes, recipe.Dislikes });
        }

        [Authorize]
        [HttpPost("{recipeId}/undislike")]
        public async Task<IActionResult> UndislikeRecipe(int recipeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recipe = await _context.Recipes.FindAsync(recipeId);
            if (recipe == null) return NotFound();

            var existingAction = await _context.RecipeLikes
                .FirstOrDefaultAsync(rl => rl.RecipeId == recipeId && rl.ChefId == userId);

            if (existingAction != null && !existingAction.IsLike)
            {
                _context.RecipeLikes.Remove(existingAction);
                recipe.Dislikes--;
                await _context.SaveChangesAsync();
            }

            return Ok(new { recipe.Likes, recipe.Dislikes });
        }

        [Authorize]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardData()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var recipesCount = await _context.Recipes.CountAsync(r => r.ChefId == userId);
            var profileViews = await _context.ProfileViews.CountAsync(v => v.ChefId == userId);
            var recentRecipes = await _context.Recipes
                .Where(r => r.ChefId == userId)
                .OrderByDescending(r => r.Id)
                .Take(3)
                .Select(r => new { r.Id, r.Title, r.Description })
                .ToListAsync();
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var viewAnalytics = await _context.ProfileViews
                .Where(v => v.ChefId == userId && v.ViewDate >= sevenDaysAgo)
                .GroupBy(v => v.ViewDate.Date)
                .Select(g => new { Date = g.Key, Views = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                RecipesCount = recipesCount,
                ProfileViews = profileViews,
                RecentRecipes = recentRecipes,
                ViewAnalytics = viewAnalytics
            });
        }

        private string GenerateJwtToken(Chef chef)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, chef.Id.ToString()),
                new Claim(ClaimTypes.Name, chef.Name),
                new Claim(ClaimTypes.Email, chef.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKey1234567890!@#$%"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "RecipeNestAPI",
                audience: "RecipeNestFrontend",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterDto
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class UpdateAccountDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RecipeDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
    }
}