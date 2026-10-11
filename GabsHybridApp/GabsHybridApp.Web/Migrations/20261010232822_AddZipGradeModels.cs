using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GabsHybridApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddZipGradeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assessments",
                schema: "GabsHybridApp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    OptionsPerQuestion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "GabsHybridApp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentIdNumber = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentKeys",
                schema: "GabsHybridApp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionLabel = table.Column<string>(type: "text", nullable: false),
                    CorrectAnswersJson = table.Column<string>(type: "text", nullable: false),
                    PointsPerQuestion = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentKeys_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalSchema: "GabsHybridApp",
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerSheetScans",
                schema: "GabsHybridApp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DetectedStudentIdNumber = table.Column<string>(type: "text", nullable: false),
                    DetectedVersion = table.Column<string>(type: "text", nullable: false),
                    TotalScore = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ScannedPhotoUrl = table.Column<string>(type: "text", nullable: true),
                    ParsedAnswersJson = table.Column<string>(type: "text", nullable: false),
                    RequiresReview = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerSheetScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerSheetScans_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalSchema: "GabsHybridApp",
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswerSheetScans_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "GabsHybridApp",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSheetScans_AssessmentId_DetectedStudentIdNumber",
                schema: "GabsHybridApp",
                table: "AnswerSheetScans",
                columns: new[] { "AssessmentId", "DetectedStudentIdNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSheetScans_StudentId",
                schema: "GabsHybridApp",
                table: "AnswerSheetScans",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentKeys_AssessmentId",
                schema: "GabsHybridApp",
                table: "AssessmentKeys",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentIdNumber",
                schema: "GabsHybridApp",
                table: "Students",
                column: "StudentIdNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerSheetScans",
                schema: "GabsHybridApp");

            migrationBuilder.DropTable(
                name: "AssessmentKeys",
                schema: "GabsHybridApp");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "GabsHybridApp");

            migrationBuilder.DropTable(
                name: "Assessments",
                schema: "GabsHybridApp");
        }
    }
}
