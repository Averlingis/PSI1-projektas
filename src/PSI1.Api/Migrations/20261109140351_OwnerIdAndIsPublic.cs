using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSI1.Api.Migrations
{
    /// <inheritdoc />
    public partial class OwnerIdAndIsPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_OwnerId",
                table: "Quizzes",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Users_OwnerId",
                table: "Quizzes",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Users_OwnerId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_OwnerId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Quizzes");
        }
    }
}