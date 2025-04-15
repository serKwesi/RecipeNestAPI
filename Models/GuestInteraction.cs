namespace RecipeNestAPI.Models
{
    public class GuestInteraction
    {
        public int Id { get; set; }
        public string GuestEmail { get; set; }
        public int RecipeId { get; set; }
        public bool IsLiked { get; set; } 
    }
}