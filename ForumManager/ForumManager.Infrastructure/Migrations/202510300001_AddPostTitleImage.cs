using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForumManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostTitleImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TitleImageBase64",
                table: "Posts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TitleImageBase64",
                table: "Posts");
        }
    }
}


