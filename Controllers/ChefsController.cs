using Microsoft.AspNetCore.Mvc;
using RecipeNestAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace RecipeNestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChefsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChefsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChef(string id)
        {
            var recipes = await _context.Recipes
                .Where(r => r.ChefId == int.Parse(id))
                .Select(r => new
                {
                    Id = r.Id.ToString(), // Convert to string for frontend
                    r.Title,
                    r.Description,
                    r.Image,
                    r.Likes,
                    r.Dislikes
                })
                .ToListAsync();

            var chef = new
            {
                name = "Test Chef",
                surname = "Doe",
                bio = "A passionate chef",
                description = "Loves cooking",
                profilePicture = "",
                instagram = "",
                linkedin = "",
                email = "",
                totalLikes = recipes.Sum(r => r.Likes),
                recipes
            };

            return Ok(chef);
        }
    }
}