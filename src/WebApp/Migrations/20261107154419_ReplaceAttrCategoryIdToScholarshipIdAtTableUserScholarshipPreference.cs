using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeasiswaIn.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAttrCategoryIdToScholarshipIdAtTableUserScholarshipPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_scholarship_preferences_scholarship_categories_categor~",
                table: "user_scholarship_preferences");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "user_scholarship_preferences",
                newName: "scholarship_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_scholarship_preferences_category_id",
                table: "user_scholarship_preferences",
                newName: "IX_user_scholarship_preferences_scholarship_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_scholarship_preferences_scholarship_categories_scholar~",
                table: "user_scholarship_preferences",
                column: "scholarship_id",
                principalTable: "scholarship_categories",
                principalColumn: "scholarship_category_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_scholarship_preferences_scholarship_categories_scholar~",
                table: "user_scholarship_preferences");

            migrationBuilder.RenameColumn(
                name: "scholarship_id",
                table: "user_scholarship_preferences",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_scholarship_preferences_scholarship_id",
                table: "user_scholarship_preferences",
                newName: "IX_user_scholarship_preferences_category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_scholarship_preferences_scholarship_categories_categor~",
                table: "user_scholarship_preferences",
                column: "category_id",
                principalTable: "scholarship_categories",
                principalColumn: "scholarship_category_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
