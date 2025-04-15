using Microsoft.EntityFrameworkCore.Migrations;

namespace RecipeNestAPI.Migrations
{
    public partial class AddRecipeLikesDislikes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Dislikes",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Likes",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dislikes",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Likes",
                table: "Recipes");
        }
    }
}