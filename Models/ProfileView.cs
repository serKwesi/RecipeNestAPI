namespace RecipeNestAPI.Models
{
    public class ProfileView
    {
        public int Id { get; set; }
        public int ChefId { get; set; }
        public DateTime ViewDate { get; set; }
        public Chef Chef { get; set; }
    }
}