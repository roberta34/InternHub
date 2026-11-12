using System.Text.Json;
using System.Text.Json.Serialization;

namespace InternHub.Tests.AI.Evaluation.Dataset;

/// <summary>Loads and validates the dataset JSON from a file or from a JSON string.</summary>
public static class EvaluationDatasetLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, "AI", "Evaluation", "Dataset", "cv-evaluation-dataset.json");

    public static EvaluationDataset Load() => Load(DefaultPath);

    public static EvaluationDataset Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Evaluation dataset not found at '{path}'. Check that the test project copies it to the output directory.",
                path);

        return Parse(File.ReadAllText(path));
    }

    public static EvaluationDataset Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        EvaluationDataset? dataset;
        try
        {
            dataset = JsonSerializer.Deserialize<EvaluationDataset>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Evaluation dataset JSON is malformed or contains an invalid value at '{exception.Path}': {exception.Message}",
                exception);
        }

        if (dataset is null)
            throw new InvalidOperationException("Evaluation dataset JSON is empty.");

        if (dataset.Cases is null || dataset.Cases.Count == 0)
            throw new InvalidOperationException("Evaluation dataset contains no cases.");

        return dataset;
    }
}
