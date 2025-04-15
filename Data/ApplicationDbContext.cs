using Microsoft.EntityFrameworkCore;
using RecipeNestAPI.Models; // Add this line

namespace RecipeNestAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Chef> Chefs { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<ProfileView> ProfileViews { get; set; }
        public DbSet<RecipeLikes> RecipeLikes { get; set; }
        public DbSet<GuestInteraction> GuestInteractions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecipeLikes>()
                .HasIndex(rl => new { rl.RecipeId, rl.ChefId })
                .IsUnique();
        }
    }
}





