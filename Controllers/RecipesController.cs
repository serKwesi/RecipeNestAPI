using Microsoft.AspNetCore.Mvc;
using RecipeNestAPI.Data;
using Microsoft.EntityFrameworkCore;
using RecipeNestAPI.Models;

namespace RecipeNestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RecipesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipe(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound("Recipe not found.");

            var guestEmail = Request.Headers["Guest-Email"].ToString();
            if (!string.IsNullOrEmpty(guestEmail))
            {
                var interaction = await _context.GuestInteractions
                    .FirstOrDefaultAsync(gi => gi.GuestEmail == guestEmail && gi.RecipeId == id);
                if (interaction != null)
                {
                    recipe.Likes += interaction.IsLiked ? 1 : 0;
                    recipe.Dislikes += !interaction.IsLiked ? 1 : 0;
                }
            }

            return Ok(new
            {
                recipe.Id,
                recipe.Title,
                recipe.Description,
                recipe.Image,
                recipe.Likes,
                recipe.Dislikes
            });
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> LikeRecipe(int id)
        {
            var guestEmail = Request.Headers["Guest-Email"].ToString();
            if (string.IsNullOrEmpty(guestEmail))
                return Unauthorized("Guest email is required.");

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound("Recipe not found.");

            var interaction = await _context.GuestInteractions
                .FirstOrDefaultAsync(gi => gi.GuestEmail == guestEmail && gi.RecipeId == id);

            if (interaction == null)
            {
                // First like
                recipe.Likes += 1;
                _context.GuestInteractions.Add(new GuestInteraction
                {
                    GuestEmail = guestEmail,
                    RecipeId = id,
                    IsLiked = true
                });
            }
            else if (!interaction.IsLiked)
            {
                // Switch from dislike to like
                recipe.Dislikes -= 1;
                recipe.Likes += 1;
                interaction.IsLiked = true;
            }
            else
            {
                // Undo like
                recipe.Likes -= 1;
                _context.GuestInteractions.Remove(interaction);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                recipe.Id,
                recipe.Title,
                recipe.Description,
                recipe.Image,
                recipe.Likes,
                recipe.Dislikes
            });
        }

        [HttpPost("{id}/dislike")]
        public async Task<IActionResult> DislikeRecipe(int id)
        {
            var guestEmail = Request.Headers["Guest-Email"].ToString();
            if (string.IsNullOrEmpty(guestEmail))
                return Unauthorized("Guest email is required.");

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound("Recipe not found.");

            var interaction = await _context.GuestInteractions
                .FirstOrDefaultAsync(gi => gi.GuestEmail == guestEmail && gi.RecipeId == id);

            if (interaction == null)
            {
                // First dislike
                recipe.Dislikes += 1;
                _context.GuestInteractions.Add(new GuestInteraction
                {
                    GuestEmail = guestEmail,
                    RecipeId = id,
                    IsLiked = false
                });
            }
            else if (interaction.IsLiked)
            {
                // Switch from like to dislike
                recipe.Likes -= 1;
                recipe.Dislikes += 1;
                interaction.IsLiked = false;
            }
            else
            {
                // Undo dislike
                recipe.Dislikes -= 1;
                _context.GuestInteractions.Remove(interaction);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                recipe.Id,
                recipe.Title,
                recipe.Description,
                recipe.Image,
                recipe.Likes,
                recipe.Dislikes
            });
        }
    }
}

