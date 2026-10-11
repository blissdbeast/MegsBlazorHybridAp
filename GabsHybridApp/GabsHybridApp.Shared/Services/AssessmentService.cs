using System.Text;
using GabsHybridApp.Shared.Data;
using GabsHybridApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GabsHybridApp.Shared.Services;

public interface IAssessmentService
{
    Task<List<Assessment>> GetAssessmentsAsync();
    Task<Assessment?> GetAssessmentByIdAsync(Guid id);
    Task<Assessment> SaveAssessmentAsync(Assessment assessment);
    Task<bool> DeleteAssessmentAsync(Guid id);
    Task<AssessmentKey> SaveAssessmentKeyAsync(AssessmentKey key);
    Task<AnswerSheetScan> SaveScanAsync(AnswerSheetScan scan);
    Task<bool> DeleteScanAsync(Guid scanId);

    Task<List<Student>> GetStudentsAsync();
    Task<Student?> FindStudentByIdNumberAsync(string idNumber);
    Task<Student> SaveStudentAsync(Student student);
    Task<bool> DeleteStudentAsync(Guid studentId);
    Task<int> ImportStudentsFromCsvAsync(string csvContent);
    string ExportGradebookCsv(Assessment assessment);
}

public class AssessmentService : IAssessmentService
{
    private readonly IDbContextFactory<HybridAppDbContext> _dbFactory;

    public AssessmentService(IDbContextFactory<HybridAppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Assessment>> GetAssessmentsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Assessments
            .Include(a => a.Keys)
            .Include(a => a.Scans)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Assessment?> GetAssessmentByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Assessments
            .Include(a => a.Keys)
            .Include(a => a.Scans)
                .ThenInclude(s => s.Student)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Assessment> SaveAssessmentAsync(Assessment assessment)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Assessments
            .Include(a => a.Keys)
            .FirstOrDefaultAsync(a => a.Id == assessment.Id);

        if (existing == null)
        {
            if (assessment.Keys.Count == 0)
            {
                // Default Version A key
                assessment.Keys.Add(new AssessmentKey
                {
                    Id = Guid.CreateVersion7(),
                    AssessmentId = assessment.Id,
                    VersionLabel = "A",
                    PointsPerQuestion = 1.0m,
                    CorrectAnswersJson = "{}"
                });
            }
            db.Assessments.Add(assessment);
        }
        else
        {
            existing.Title = assessment.Title;
            existing.Subject = assessment.Subject;
            existing.Section = assessment.Section;
            existing.QuestionCount = assessment.QuestionCount;
            existing.OptionsPerQuestion = assessment.OptionsPerQuestion;
        }

        await db.SaveChangesAsync();
        return assessment;
    }

    public async Task<bool> DeleteAssessmentAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Assessments.FindAsync(id);
        if (existing == null) return false;

        db.Assessments.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<AssessmentKey> SaveAssessmentKeyAsync(AssessmentKey key)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.AssessmentKeys.FindAsync(key.Id);
        if (existing == null)
        {
            db.AssessmentKeys.Add(key);
        }
        else
        {
            existing.VersionLabel = key.VersionLabel;
            existing.CorrectAnswersJson = key.CorrectAnswersJson;
            existing.PointsPerQuestion = key.PointsPerQuestion;
        }

        await db.SaveChangesAsync();
        return key;
    }

    public async Task<AnswerSheetScan> SaveScanAsync(AnswerSheetScan scan)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Match student if not linked
        if (!scan.StudentId.HasValue && !string.IsNullOrWhiteSpace(scan.DetectedStudentIdNumber))
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.StudentIdNumber == scan.DetectedStudentIdNumber);
            if (student != null)
            {
                scan.StudentId = student.Id;
            }
        }

        var existing = await db.AnswerSheetScans.FindAsync(scan.Id);
        if (existing == null)
        {
            db.AnswerSheetScans.Add(scan);
        }
        else
        {
            existing.StudentId = scan.StudentId;
            existing.DetectedStudentIdNumber = scan.DetectedStudentIdNumber;
            existing.DetectedVersion = scan.DetectedVersion;
            existing.TotalScore = scan.TotalScore;
            existing.MaxScore = scan.MaxScore;
            existing.ParsedAnswersJson = scan.ParsedAnswersJson;
            existing.RequiresReview = scan.RequiresReview;
            existing.ReviewNotes = scan.ReviewNotes;
            existing.ScannedPhotoUrl = scan.ScannedPhotoUrl;
        }

        await db.SaveChangesAsync();
        return scan;
    }

    public async Task<bool> DeleteScanAsync(Guid scanId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.AnswerSheetScans.FindAsync(scanId);
        if (existing == null) return false;

        db.AnswerSheetScans.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Student>> GetStudentsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Students
            .OrderBy(s => s.StudentIdNumber)
            .ToListAsync();
    }

    public async Task<Student?> FindStudentByIdNumberAsync(string idNumber)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Students.FirstOrDefaultAsync(s => s.StudentIdNumber == idNumber);
    }

    public async Task<Student> SaveStudentAsync(Student student)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Students.FindAsync(student.Id);
        if (existing == null)
        {
            db.Students.Add(student);
        }
        else
        {
            existing.StudentIdNumber = student.StudentIdNumber;
            existing.FullName = student.FullName;
            existing.Section = student.Section;
            existing.Email = student.Email;
        }

        await db.SaveChangesAsync();
        return student;
    }

    public async Task<bool> DeleteStudentAsync(Guid studentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Students.FindAsync(studentId);
        if (existing == null) return false;

        db.Students.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> ImportStudentsFromCsvAsync(string csvContent)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        int added = 0;

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            string idNumber = parts[0].Trim().Trim('"');
            string name = parts[1].Trim().Trim('"');
            string section = parts.Length > 2 ? parts[2].Trim().Trim('"') : "";
            string email = parts.Length > 3 ? parts[3].Trim().Trim('"') : "";

            if (string.Equals(idNumber, "StudentId", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(idNumber, "ID", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Skip header
            }

            var existing = await db.Students.FirstOrDefaultAsync(s => s.StudentIdNumber == idNumber);
            if (existing == null)
            {
                db.Students.Add(new Student
                {
                    Id = Guid.NewGuid(),
                    StudentIdNumber = idNumber,
                    FullName = name,
                    Section = section,
                    Email = email
                });
                added++;
            }
            else
            {
                existing.FullName = name;
                if (!string.IsNullOrWhiteSpace(section)) existing.Section = section;
                if (!string.IsNullOrWhiteSpace(email)) existing.Email = email;
            }
        }

        await db.SaveChangesAsync();
        return added;
    }

    public string ExportGradebookCsv(Assessment assessment)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Student ID,Student Name,Section,Version,Score,Max Score,Percentage,Letter Grade,Scanned At");

        foreach (var scan in assessment.Scans.OrderByDescending(s => s.Percentage))
        {
            var studentName = scan.Student?.FullName ?? "Unknown Student";
            var section = scan.Student?.Section ?? assessment.Section;
            var letterGrade = scan.Percentage switch
            {
                >= 90m => "A",
                >= 80m => "B",
                >= 70m => "C",
                >= 60m => "D",
                _ => "F"
            };

            sb.AppendLine($"\"{scan.DetectedStudentIdNumber}\",\"{studentName}\",\"{section}\",\"{scan.DetectedVersion}\",{scan.TotalScore},{scan.MaxScore},{scan.Percentage}%,{letterGrade},\"{scan.ScannedAtUtc:yyyy-MM-dd HH:mm}\"");
        }

        return sb.ToString();
    }
}
