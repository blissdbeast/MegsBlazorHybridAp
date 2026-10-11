using GabsHybridApp.Shared.Models;

namespace GabsHybridApp.Shared.Services;

public interface IItemAnalysisService
{
    List<ItemAnalysisMetric> AnalyzeAssessment(Assessment assessment, AssessmentKey key);
}

public class ItemAnalysisService : IItemAnalysisService
{
    public List<ItemAnalysisMetric> AnalyzeAssessment(Assessment assessment, AssessmentKey key)
    {
        var result = new List<ItemAnalysisMetric>();
        var scans = assessment.Scans.Where(s => s.DetectedVersion == key.VersionLabel).ToList();
        if (scans.Count == 0) scans = assessment.Scans.ToList(); // Fallback to all scans

        var keyAnswers = key.GetAnswers();
        int totalStudents = scans.Count;

        if (totalStudents == 0)
        {
            // Empty analysis skeleton
            for (int q = 1; q <= assessment.QuestionCount; q++)
            {
                result.Add(new ItemAnalysisMetric
                {
                    QuestionNumber = q,
                    CorrectAnswers = keyAnswers.TryGetValue(q, out var c) ? c : new List<string>(),
                    TotalAttempts = 0,
                    TotalCorrect = 0,
                    DiscriminationIndex = 0.0,
                    OptionDistribution = new Dictionary<string, int>()
                });
            }
            return result;
        }

        // Rank students by total score to find upper and lower groups for Discrimination (D)
        var rankedScans = scans.OrderByDescending(s => s.TotalScore).ToList();
        int groupSize = Math.Max(1, (int)Math.Round(totalStudents * 0.27));
        var upperGroup = rankedScans.Take(groupSize).ToList();
        var lowerGroup = rankedScans.TakeLast(groupSize).ToList();

        for (int q = 1; q <= assessment.QuestionCount; q++)
        {
            var correctList = keyAnswers.TryGetValue(q, out var c) ? c : new List<string>();
            int correctCount = 0;
            var dist = new Dictionary<string, int>
            {
                ["A"] = 0, ["B"] = 0, ["C"] = 0, ["D"] = 0, ["E"] = 0, ["Blank"] = 0, ["Multi"] = 0
            };

            foreach (var scan in scans)
            {
                var answers = scan.GetAnswers();
                if (answers.TryGetValue(q, out var studAnswers) && studAnswers.Count > 0)
                {
                    if (studAnswers.Count == 1)
                    {
                        var opt = studAnswers[0].ToUpperInvariant();
                        if (dist.ContainsKey(opt)) dist[opt]++;
                    }
                    else
                    {
                        dist["Multi"]++;
                    }

                    if (correctList.Count > 0 && 
                        studAnswers.Count == correctList.Count && 
                        !studAnswers.Except(correctList).Any())
                    {
                        correctCount++;
                    }
                }
                else
                {
                    dist["Blank"]++;
                }
            }

            // Calculate Discrimination index (D = (R_upper - R_lower) / groupSize)
            int upperCorrect = 0;
            foreach (var scan in upperGroup)
            {
                var answers = scan.GetAnswers();
                if (answers.TryGetValue(q, out var studAnswers) && 
                    correctList.Count > 0 &&
                    studAnswers.Count == correctList.Count &&
                    !studAnswers.Except(correctList).Any())
                {
                    upperCorrect++;
                }
            }

            int lowerCorrect = 0;
            foreach (var scan in lowerGroup)
            {
                var answers = scan.GetAnswers();
                if (answers.TryGetValue(q, out var studAnswers) && 
                    correctList.Count > 0 &&
                    studAnswers.Count == correctList.Count &&
                    !studAnswers.Except(correctList).Any())
                {
                    lowerCorrect++;
                }
            }

            double discrimination = groupSize > 0 
                ? Math.Round((double)(upperCorrect - lowerCorrect) / groupSize, 2)
                : 0.0;

            result.Add(new ItemAnalysisMetric
            {
                QuestionNumber = q,
                CorrectAnswers = correctList,
                TotalAttempts = totalStudents,
                TotalCorrect = correctCount,
                DiscriminationIndex = discrimination,
                OptionDistribution = dist
            });
        }

        return result;
    }
}
