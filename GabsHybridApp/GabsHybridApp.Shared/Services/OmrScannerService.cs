using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace GabsHybridApp.Shared.Services;

public sealed record OmrPoint(
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y
);

public sealed class OmrScanOutput
{
    [JsonPropertyName("detectedStudentId")]
    public string DetectedStudentId { get; set; } = string.Empty;

    [JsonPropertyName("detectedVersion")]
    public string DetectedVersion { get; set; } = "A";

    [JsonPropertyName("studentIdConfidence")]
    public double StudentIdConfidence { get; set; }

    [JsonPropertyName("detectedAnswers")]
    public Dictionary<string, List<string>> DetectedAnswersRaw { get; set; } = new();

    [JsonPropertyName("detectedCorners")]
    public List<OmrPoint> DetectedCorners { get; set; } = new();

    [JsonPropertyName("detectedAuto")]
    public bool DetectedAuto { get; set; }

    [JsonPropertyName("rectifiedImageUrl")]
    public string? RectifiedImageUrl { get; set; }

    [JsonPropertyName("requiresReview")]
    public bool RequiresReview { get; set; }

    public Dictionary<int, List<string>> GetAnswers()
    {
        var result = new Dictionary<int, List<string>>();
        foreach (var (k, v) in DetectedAnswersRaw)
        {
            if (int.TryParse(k, out int qNum))
            {
                result[qNum] = v ?? new List<string>();
            }
        }
        return result;
    }
}

public interface IOmrScannerService
{
    Task<OmrScanOutput?> ProcessSheetAsync(
        string imageDataUrl,
        int questionCount,
        int optionsCount,
        Dictionary<int, List<string>> keyAnswers,
        List<OmrPoint>? customCorners = null
    );
}

public class OmrScannerService : IOmrScannerService
{
    private readonly IJSRuntime _js;

    public OmrScannerService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<OmrScanOutput?> ProcessSheetAsync(
        string imageDataUrl,
        int questionCount,
        int optionsCount,
        Dictionary<int, List<string>> keyAnswers,
        List<OmrPoint>? customCorners = null)
    {
        try
        {
            // Convert int keys to string for JSON serialization
            var keyMap = keyAnswers.ToDictionary(k => k.Key.ToString(), v => v.Value);

            var result = await _js.InvokeAsync<OmrScanOutput>(
                "ZipGradeOmr.processSheetImage",
                imageDataUrl,
                questionCount,
                optionsCount,
                keyMap,
                customCorners
            );

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OmrScannerService] Error: {ex.Message}");
            return null;
        }
    }
}
