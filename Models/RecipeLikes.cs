namespace RecipeNestAPI.Models
{
    public class RecipeLikes
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int ChefId { get; set; }
        public bool IsLike { get; set; } // True for like, False for dislike

        public Recipe Recipe { get; set; }
        public Chef Chef { get; set; }
    }
}