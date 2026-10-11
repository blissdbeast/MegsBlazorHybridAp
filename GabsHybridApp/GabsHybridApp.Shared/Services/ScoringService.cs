using GabsHybridApp.Shared.Models;

namespace GabsHybridApp.Shared.Services;

public sealed record QuestionScoreResult(
    int QuestionNumber,
    List<string> StudentAnswers,
    List<string> KeyAnswers,
    bool IsCorrect,
    decimal PointsEarned,
    decimal MaxPoints
);

public sealed record ScoredSubmissionResult(
    decimal TotalScore,
    decimal MaxScore,
    decimal Percentage,
    string LetterGrade,
    List<QuestionScoreResult> Details
);

public interface IScoringService
{
    ScoredSubmissionResult ScoreSubmission(
        Assessment assessment,
        AssessmentKey key,
        Dictionary<int, List<string>> studentAnswers
    );
}

public class ScoringService : IScoringService
{
    public ScoredSubmissionResult ScoreSubmission(
        Assessment assessment,
        AssessmentKey key,
        Dictionary<int, List<string>> studentAnswers)
    {
        var keyAnswers = key.GetAnswers();
        var details = new List<QuestionScoreResult>();
        decimal totalScore = 0m;
        decimal maxScore = 0m;

        for (int q = 1; q <= assessment.QuestionCount; q++)
        {
            var correctList = keyAnswers.TryGetValue(q, out var correct) ? correct : new List<string>();
            var studentList = studentAnswers.TryGetValue(q, out var stud) ? stud : new List<string>();

            decimal qPoints = key.PointsPerQuestion;
            maxScore += qPoints;

            bool isCorrect = false;
            decimal pointsEarned = 0m;

            if (correctList.Count > 0 && studentList.Count > 0)
            {
                // Exact set match for multiple answers, or single answer match
                if (correctList.Count == studentList.Count && !correctList.Except(studentList).Any())
                {
                    isCorrect = true;
                    pointsEarned = qPoints;
                }
            }

            totalScore += pointsEarned;
            details.Add(new QuestionScoreResult(q, studentList, correctList, isCorrect, pointsEarned, qPoints));
        }

        decimal pct = maxScore > 0 ? Math.Round((totalScore / maxScore) * 100m, 2) : 0m;
        string letter = pct switch
        {
            >= 90m => "A",
            >= 80m => "B",
            >= 70m => "C",
            >= 60m => "D",
            _ => "F"
        };

        return new ScoredSubmissionResult(totalScore, maxScore, pct, letter, details);
    }
}
