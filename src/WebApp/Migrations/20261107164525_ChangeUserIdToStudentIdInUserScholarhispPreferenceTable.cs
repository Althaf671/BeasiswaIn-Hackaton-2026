using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeasiswaIn.Web.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserIdToStudentIdInUserScholarhispPreferenceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_scholarship_preferences_AspNetUsers_user_id",
                table: "user_scholarship_preferences");

            migrationBuilder.DropForeignKey(
                name: "FK_user_scholarship_preferences_scholarship_categories_scholar~",
                table: "user_scholarship_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_scholarship_preferences",
                table: "user_scholarship_preferences");

            migrationBuilder.RenameTable(
                name: "user_scholarship_preferences",
                newName: "student_scholarship_preferences");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "student_scholarship_preferences",
                newName: "student_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_scholarship_preferences_scholarship_id",
                table: "student_scholarship_preferences",
                newName: "IX_student_scholarship_preferences_scholarship_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_student_scholarship_preferences",
                table: "student_scholarship_preferences",
                columns: new[] { "student_id", "scholarship_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_student_scholarship_preferences_scholarship_categories_scho~",
                table: "student_scholarship_preferences",
                column: "scholarship_id",
                principalTable: "scholarship_categories",
                principalColumn: "scholarship_category_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_scholarship_preferences_student_profile_student_id",
                table: "student_scholarship_preferences",
                column: "student_id",
                principalTable: "student_profile",
                principalColumn: "student_profile_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_scholarship_preferences_scholarship_categories_scho~",
                table: "student_scholarship_preferences");

            migrationBuilder.DropForeignKey(
                name: "FK_student_scholarship_preferences_student_profile_student_id",
                table: "student_scholarship_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_student_scholarship_preferences",
                table: "student_scholarship_preferences");

            migrationBuilder.RenameTable(
                name: "student_scholarship_preferences",
                newName: "user_scholarship_preferences");

            migrationBuilder.RenameColumn(
                name: "student_id",
                table: "user_scholarship_preferences",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_student_scholarship_preferences_scholarship_id",
                table: "user_scholarship_preferences",
                newName: "IX_user_scholarship_preferences_scholarship_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_scholarship_preferences",
                table: "user_scholarship_preferences",
                columns: new[] { "user_id", "scholarship_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_user_scholarship_preferences_AspNetUsers_user_id",
                table: "user_scholarship_preferences",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_scholarship_preferences_scholarship_categories_scholar~",
                table: "user_scholarship_preferences",
                column: "scholarship_id",
                principalTable: "scholarship_categories",
                principalColumn: "scholarship_category_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
