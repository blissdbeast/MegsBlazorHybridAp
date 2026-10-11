using System.Text.Json;
using GabsHybridApp.Shared.Models;

namespace GabsHybridApp.Shared.Data;

public static class ZipGradeDataSeeder
{
    public static void SeedZipGradeData(this HybridAppDbContext db)
    {
        if (db.Students.Any() || db.Assessments.Any()) return;

        // Seed Sample Students
        var student1 = new Student
        {
            Id = Guid.NewGuid(),
            StudentIdNumber = "10001",
            FullName = "Alex Mercer",
            Section = "Grade 10 - Newton",
            Email = "alex.mercer@school.edu"
        };
        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            StudentIdNumber = "10002",
            FullName = "Bianca Vance",
            Section = "Grade 10 - Newton",
            Email = "bianca.vance@school.edu"
        };
        var student3 = new Student
        {
            Id = Guid.NewGuid(),
            StudentIdNumber = "10003",
            FullName = "Carlos Mendoza",
            Section = "Grade 10 - Newton",
            Email = "carlos.mendoza@school.edu"
        };
        var student4 = new Student
        {
            Id = Guid.NewGuid(),
            StudentIdNumber = "10004",
            FullName = "Diana Prince",
            Section = "Grade 10 - Newton",
            Email = "diana.prince@school.edu"
        };
        var student5 = new Student
        {
            Id = Guid.NewGuid(),
            StudentIdNumber = "10005",
            FullName = "Ethan Hunt",
            Section = "Grade 10 - Newton",
            Email = "ethan.hunt@school.edu"
        };

        db.Students.AddRange(student1, student2, student3, student4, student5);

        // Seed Sample Assessment: 20 Questions
        var assessmentId = Guid.CreateVersion7();
        var assessment = new Assessment
        {
            Id = assessmentId,
            Title = "Cellular Biology & Genetics",
            Subject = "General Science",
            Section = "Grade 10 - Newton",
            QuestionCount = 20,
            OptionsPerQuestion = 4,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        // Key Version A
        var keyAAnswers = new Dictionary<int, List<string>>();
        string[] pattern = { "A", "B", "C", "D", "B", "A", "C", "D", "A", "B", "C", "D", "A", "C", "B", "D", "A", "B", "C", "D" };
        for (int i = 0; i < 20; i++)
        {
            keyAAnswers[i + 1] = new List<string> { pattern[i] };
        }

        var keyA = new AssessmentKey
        {
            Id = Guid.CreateVersion7(),
            AssessmentId = assessmentId,
            VersionLabel = "A",
            CorrectAnswersJson = JsonSerializer.Serialize(keyAAnswers),
            PointsPerQuestion = 1.0m
        };

        // Key Version B
        var keyBAnswers = new Dictionary<int, List<string>>();
        string[] patternB = { "D", "C", "B", "A", "C", "D", "B", "A", "D", "C", "B", "A", "D", "B", "C", "A", "D", "C", "B", "A" };
        for (int i = 0; i < 20; i++)
        {
            keyBAnswers[i + 1] = new List<string> { patternB[i] };
        }

        var keyB = new AssessmentKey
        {
            Id = Guid.CreateVersion7(),
            AssessmentId = assessmentId,
            VersionLabel = "B",
            CorrectAnswersJson = JsonSerializer.Serialize(keyBAnswers),
            PointsPerQuestion = 1.0m
        };

        assessment.Keys.Add(keyA);
        assessment.Keys.Add(keyB);

        // Seed initial graded scans for Alex (18/20), Bianca (15/20), Carlos (19/20)
        var scan1Answers = new Dictionary<int, List<string>>(keyAAnswers);
        scan1Answers[4] = new List<string> { "B" }; // Incorrect
        scan1Answers[12] = new List<string> { "A" }; // Incorrect

        var scan1 = new AnswerSheetScan
        {
            Id = Guid.CreateVersion7(),
            AssessmentId = assessmentId,
            StudentId = student1.Id,
            DetectedStudentIdNumber = student1.StudentIdNumber,
            DetectedVersion = "A",
            TotalScore = 18.0m,
            MaxScore = 20.0m,
            ScannedAtUtc = DateTime.UtcNow.AddDays(-1),
            ParsedAnswersJson = JsonSerializer.Serialize(scan1Answers),
            RequiresReview = false
        };

        var scan2Answers = new Dictionary<int, List<string>>(keyAAnswers);
        scan2Answers[1] = new List<string> { "C" };
        scan2Answers[5] = new List<string> { "D" };
        scan2Answers[8] = new List<string> { "A" };
        scan2Answers[15] = new List<string> { "C" };
        scan2Answers[20] = new List<string> { "A" };

        var scan2 = new AnswerSheetScan
        {
            Id = Guid.CreateVersion7(),
            AssessmentId = assessmentId,
            StudentId = student2.Id,
            DetectedStudentIdNumber = student2.StudentIdNumber,
            DetectedVersion = "A",
            TotalScore = 15.0m,
            MaxScore = 20.0m,
            ScannedAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(1),
            ParsedAnswersJson = JsonSerializer.Serialize(scan2Answers),
            RequiresReview = false
        };

        var scan3Answers = new Dictionary<int, List<string>>(keyAAnswers);
        scan3Answers[7] = new List<string> { "A" };

        var scan3 = new AnswerSheetScan
        {
            Id = Guid.CreateVersion7(),
            AssessmentId = assessmentId,
            StudentId = student3.Id,
            DetectedStudentIdNumber = student3.StudentIdNumber,
            DetectedVersion = "A",
            TotalScore = 19.0m,
            MaxScore = 20.0m,
            ScannedAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(2),
            ParsedAnswersJson = JsonSerializer.Serialize(scan3Answers),
            RequiresReview = false
        };

        assessment.Scans.Add(scan1);
        assessment.Scans.Add(scan2);
        assessment.Scans.Add(scan3);

        db.Assessments.Add(assessment);
        db.SaveChanges();
    }
}
