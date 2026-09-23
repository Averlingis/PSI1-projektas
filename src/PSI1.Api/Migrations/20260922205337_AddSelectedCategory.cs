using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSI1.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedCategory",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedCategory",
                table: "Users");
        }
    }
}
