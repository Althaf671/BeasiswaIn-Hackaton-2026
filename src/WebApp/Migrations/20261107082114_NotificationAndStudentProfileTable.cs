using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BeasiswaIn.Web.Migrations
{
    /// <inheritdoc />
    public partial class NotificationAndStudentProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    notification_id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    content = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    type = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: false),
                    status = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: false),
                    ActionUrl = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    recepient_user_id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_notification_AspNetUsers_recepient_user_id",
                        column: x => x.recepient_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_profile",
                columns: table => new
                {
                    student_profile_id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    final_score = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    identity_user_id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_profile", x => x.student_profile_id);
                    table.ForeignKey(
                        name: "FK_student_profile_AspNetUsers_identity_user_id",
                        column: x => x.identity_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_recepient_user_id_notification_id",
                table: "notification",
                columns: new[] { "recepient_user_id", "notification_id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_student_profile_identity_user_id",
                table: "student_profile",
                column: "identity_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "scholarship_category_assignments");

            migrationBuilder.DropTable(
                name: "student_profile");

            migrationBuilder.DropTable(
                name: "user_scholarship_preferences");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "scholarships");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "scholarship_categories");
        }
    }
}
