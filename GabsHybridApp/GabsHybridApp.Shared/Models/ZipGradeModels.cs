using System.Text.Json;

namespace GabsHybridApp.Shared.Models;

/// <summary>
/// Assessment or quiz definition supporting 10, 20, 50, or 100 questions.
/// Uses UUIDv7 for sequential B-Tree ordering.
/// </summary>
public class Assessment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public int QuestionCount { get; set; } = 20; // 10, 20, 50, 100
    public int OptionsPerQuestion { get; set; } = 4; // 4 (A-D) or 5 (A-E)
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<AssessmentKey> Keys { get; set; } = new();
    public List<AnswerSheetScan> Scans { get; set; } = new();
}

/// <summary>
/// Master answer key for an assessment (supports versions A, B, C, D).
/// </summary>
public class AssessmentKey
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AssessmentId { get; set; }
    public string VersionLabel { get; set; } = "A"; // "A", "B", "C", "D"
    public string CorrectAnswersJson { get; set; } = "{}"; // Map: questionNumber -> ["A"]
    public decimal PointsPerQuestion { get; set; } = 1.0m;

    public Assessment? Assessment { get; set; }

    public Dictionary<int, List<string>> GetAnswers()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, List<string>>>(CorrectAnswersJson) 
                   ?? new Dictionary<int, List<string>>();
        }
        catch
        {
            return new Dictionary<int, List<string>>();
        }
    }

    public void SetAnswers(Dictionary<int, List<string>> answers)
    {
        CorrectAnswersJson = JsonSerializer.Serialize(answers);
    }
}

/// <summary>
/// Individual student scanned answer sheet result.
/// </summary>
public class AnswerSheetScan
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AssessmentId { get; set; }
    public Guid? StudentId { get; set; }
    public string DetectedStudentIdNumber { get; set; } = string.Empty;
    public string DetectedVersion { get; set; } = "A";
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage => MaxScore > 0 ? Math.Round((TotalScore / MaxScore) * 100m, 2) : 0;
    public DateTime ScannedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ScannedPhotoUrl { get; set; }
    public string ParsedAnswersJson { get; set; } = "{}"; // questionNumber -> ["A"]
    public bool RequiresReview { get; set; }
    public string? ReviewNotes { get; set; }

    public Assessment? Assessment { get; set; }
    public Student? Student { get; set; }

    public Dictionary<int, List<string>> GetAnswers()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, List<string>>>(ParsedAnswersJson) 
                   ?? new Dictionary<int, List<string>>();
        }
        catch
        {
            return new Dictionary<int, List<string>>();
        }
    }

    public void SetAnswers(Dictionary<int, List<string>> answers)
    {
        ParsedAnswersJson = JsonSerializer.Serialize(answers);
    }
}

/// <summary>
/// Student roster identity entity.
/// Uses UUIDv4 for personal identity enumeration resistance.
/// </summary>
public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid(); // UUIDv4 for privacy
    public string StudentIdNumber { get; set; } = string.Empty; // e.g. "10234"
    public string FullName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<AnswerSheetScan> Scans { get; set; } = new();
}

/// <summary>
/// Psychometric item analysis metrics for individual assessment questions.
/// </summary>
public sealed class ItemAnalysisMetric
{
    public int QuestionNumber { get; set; }
    public List<string> CorrectAnswers { get; set; } = new();
    public int TotalAttempts { get; set; }
    public int TotalCorrect { get; set; }
    
    /// <summary>
    /// Item Difficulty (P) = Proportion of examinees who answered correctly (0.00 to 1.00).
    /// </summary>
    public double DifficultyIndex => TotalAttempts > 0 ? Math.Round((double)TotalCorrect / TotalAttempts, 2) : 0.0;

    /// <summary>
    /// Item Discrimination (D) = Difference between upper 27% and lower 27% correct rate (-1.00 to 1.00).
    /// </summary>
    public double DiscriminationIndex { get; set; }

    /// <summary>
    /// Distribution of marks across options: A, B, C, D, E, Blank, Multi.
    /// </summary>
    public Dictionary<string, int> OptionDistribution { get; set; } = new();

    public string DifficultyLabel => DifficultyIndex switch
    {
        < 0.20 => "Very Difficult",
        <= 0.50 => "Difficult",
        <= 0.80 => "Moderate",
        _ => "Very Easy"
    };

    public string DiscriminationLabel => DiscriminationIndex switch
    {
        < 0.20 => "Poor / Misleading",
        <= 0.29 => "Fair / Review",
        <= 0.39 => "Good",
        _ => "Excellent"
    };
}
