namespace RecipeNestAPI.Models
{
    public class Chef
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public string? Description { get; set; }
        public string? Instagram { get; set; }
        public string? Linkedin { get; set; }
        public float Rating { get; set; }
        public List<Recipe> Recipes { get; set; } = new List<Recipe>();
    }

   public class Recipe
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public int ChefId { get; set; }
        public int Likes { get; set; } // Keep these from your existing migration
        public int Dislikes { get; set; }
        public Chef Chef { get; set; }
        public ICollection<RecipeLikes> RecipeLikes { get; set; } = new List<RecipeLikes>(); // Add this
    }
}