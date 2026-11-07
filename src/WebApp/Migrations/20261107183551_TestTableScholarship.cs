using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeasiswaIn.Web.Migrations
{
    /// <inheritdoc />
    public partial class TestTableScholarship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "submitted_scholarship",
                columns: table => new
                {
                    submitted_scholarship_id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    scholarship_source_url = table.Column<string>(type: "character varying(2100)", maxLength: 2100, nullable: true),
                    scholarship_content_url = table.Column<string>(type: "character varying(2100)", maxLength: 2100, nullable: true),
                    scholarship_content_public_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    student_profile_id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submitted_scholarship", x => x.submitted_scholarship_id);
                    table.ForeignKey(
                        name: "FK_submitted_scholarship_student_profile_student_profile_id",
                        column: x => x.student_profile_id,
                        principalTable: "student_profile",
                        principalColumn: "student_profile_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_submitted_scholarship_student_profile_id",
                table: "submitted_scholarship",
                column: "student_profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submitted_scholarship");
        }
    }
}
