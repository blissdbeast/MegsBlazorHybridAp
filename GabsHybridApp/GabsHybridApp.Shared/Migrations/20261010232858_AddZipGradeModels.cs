using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GabsHybridApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddZipGradeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", nullable: false),
                    QuestionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OptionsPerQuestion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentIdNumber = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionLabel = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectAnswersJson = table.Column<string>(type: "TEXT", nullable: false),
                    PointsPerQuestion = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentKeys_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerSheetScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DetectedStudentIdNumber = table.Column<string>(type: "TEXT", nullable: false),
                    DetectedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    TotalScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScannedPhotoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ParsedAnswersJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequiresReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReviewNotes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerSheetScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerSheetScans_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswerSheetScans_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSheetScans_AssessmentId_DetectedStudentIdNumber",
                table: "AnswerSheetScans",
                columns: new[] { "AssessmentId", "DetectedStudentIdNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSheetScans_StudentId",
                table: "AnswerSheetScans",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentKeys_AssessmentId",
                table: "AssessmentKeys",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentIdNumber",
                table: "Students",
                column: "StudentIdNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerSheetScans");

            migrationBuilder.DropTable(
                name: "AssessmentKeys");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Assessments");
        }
    }
}
