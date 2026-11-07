using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeasiswaIn.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentScholarshipPreferencesFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_scholarship_preferences_scholarship_categories_scho~",
                table: "student_scholarship_preferences");

            migrationBuilder.AddForeignKey(
                name: "FK_student_scholarship_preferences_scholarships_scholarship_id",
                table: "student_scholarship_preferences",
                column: "scholarship_id",
                principalTable: "scholarships",
                principalColumn: "scholarship_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_scholarship_preferences_scholarships_scholarship_id",
                table: "student_scholarship_preferences");

            migrationBuilder.AddForeignKey(
                name: "FK_student_scholarship_preferences_scholarship_categories_scho~",
                table: "student_scholarship_preferences",
                column: "scholarship_id",
                principalTable: "scholarship_categories",
                principalColumn: "scholarship_category_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
