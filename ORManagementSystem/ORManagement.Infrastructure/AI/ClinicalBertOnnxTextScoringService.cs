using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.Interfaces.Services;
using ORManagement.Application.Services;

namespace ORManagement.Infrastructure.AI
{
    public class ClinicalBertOnnxTextScoringService : IClinicalTextScoringService, IDisposable
    {
        private static readonly (string Label, string Text, decimal Score)[] SeverityAnchors =
{
    (
        "VeryLow",
        "Routine elective surgical request. Patient is clinically stable. No acute deterioration. No severe pain. No bleeding. No sepsis. Surgery can be scheduled routinely.",
        10m
    ),
    (
        "Low",
        "Low urgency surgical request. Patient has mild stable symptoms. Delay is unlikely to cause significant harm or complication. Routine operating room scheduling is appropriate.",
        30m
    ),
    (
        "Medium",
        "Moderate urgency surgical request. Patient has persistent symptoms, pain, obstruction, wound risk, or clinical issue where delay may increase complication risk but patient is currently stable.",
        55m
    ),
    (
        "High",
        "High urgency surgical request. Patient has worsening symptoms, progressive neurological deficit, severe pain, ischemia, obstruction, infection risk, or risk of loss of function if surgery is delayed.",
        80m
    ),
    (
        "Critical",
        "Critical emergency surgical request. Patient has acute deterioration, unstable clinical status, sepsis, shock, active bleeding, acute neurological deterioration, or high risk of permanent deficit or death if delayed.",
        100m
    )
};

        private readonly ClinicalScoringOptions _options;
        private readonly FallbackClinicalKeywordScoringService _fallbackScoringService;
        private readonly ILogger<ClinicalBertOnnxTextScoringService> _logger;

        private readonly InferenceSession? _session;
        private readonly BertTokenizer? _tokenizer;

        private List<SeverityAnchorEmbedding>? _severityAnchorEmbeddings;
        private readonly object _severityAnchorLock = new();

        public ClinicalBertOnnxTextScoringService(
            IOptions<ClinicalScoringOptions> options,
            FallbackClinicalKeywordScoringService fallbackScoringService,
            ILogger<ClinicalBertOnnxTextScoringService> logger)
        {
            _options = options.Value;
            _fallbackScoringService = fallbackScoringService;
            _logger = logger;

            _logger.LogInformation("ClinicalBertOnnxTextScoringService constructor called.");

            if (!_options.UseOnnx)
            {
                return;
            }

            var modelPath = Path.Combine(AppContext.BaseDirectory, _options.ModelPath);
var vocabPath = Path.Combine(AppContext.BaseDirectory, _options.VocabPath);

_logger.LogInformation("ClinicalBERT configured model path: {ModelPath}", modelPath);
_logger.LogInformation("ClinicalBERT configured vocab path: {VocabPath}", vocabPath);

if (!File.Exists(modelPath) || !File.Exists(vocabPath))
{
    _logger.LogWarning(
        "ClinicalBERT ONNX model or vocab file was not found. ModelExists: {ModelExists}, VocabExists: {VocabExists}",
        File.Exists(modelPath),
        File.Exists(vocabPath));

    return;
}

_session = new InferenceSession(modelPath);
_tokenizer = new BertTokenizer(vocabPath);

foreach (var inputName in _session.InputMetadata.Keys)
{
    _logger.LogInformation("ClinicalBERT ONNX input name: {InputName}", inputName);
}

foreach (var outputName in _session.OutputMetadata.Keys)
{
    _logger.LogInformation("ClinicalBERT ONNX output name: {OutputName}", outputName);
}

_logger.LogInformation("ClinicalBERT ONNX scorer initialized successfully.");
        }

        public async Task<ClinicalTextScoreResultDto> ScoreAsync(
            string? remarks,
            string? surgeryType,
            string? priority,
            string? patientReadiness)
{
    var fallbackResult = await _fallbackScoringService.ScoreAsync(
        remarks,
        surgeryType,
        priority,
        patientReadiness);

    if (_session is null || _tokenizer is null)
    {
        fallbackResult.UsedFallback = true;
        fallbackResult.ModelName = "FallbackClinicalKeywordScorer";
        return fallbackResult;
    }

    try
    {
        var text = BuildClinicalText(
            remarks,
            surgeryType,
            priority,
            patientReadiness);

        var transformerSignal = CalculateAnchorSimilarityTransformerScore(text);

        var clinicalScore = Math.Round(
            (fallbackResult.ClinicalScore * 0.75m) +
            (transformerSignal * 0.25m),
            2);

        clinicalScore = Math.Clamp(
            clinicalScore,
            0m,
            100m);

        return new ClinicalTextScoreResultDto
        {
            ClinicalScore = clinicalScore,
            Explanation =
                $"{fallbackResult.Explanation} ClinicalBERT anchor-similarity transformer score: {transformerSignal:0.00}.",
            ModelName = "ClinicalBERT-ONNX-AnchorSimilarity-DemoHybrid",
            UsedFallback = false
        };
    }
    catch (Exception ex)
    {
        _logger.LogWarning(
            ex,
            "ClinicalBERT ONNX scoring failed. Falling back to local clinical keyword scorer.");

        fallbackResult.UsedFallback = true;
        fallbackResult.ModelName = "FallbackClinicalKeywordScorer";
        fallbackResult.Explanation =
            $"{fallbackResult.Explanation} ONNX scoring failed and fallback was used.";

        return fallbackResult;
    }
}

public Task<(decimal TransformerSignalScore, string Explanation, string ModelName)> ScoreTransformerSignalOnlyAsync(
    string remarks)
{
    if (_session is null || _tokenizer is null)
    {
        throw new InvalidOperationException(
            "ClinicalBERT ONNX model is not loaded. Check model.onnx, model.onnx.data, vocab.txt, and ClinicalScoring configuration.");
    }

    var text = string.Join(
        " ",
        new[]
        {
                    "Clinical remarks:",
                    remarks
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    var transformerSignal = CalculateAnchorSimilarityTransformerScore(text);

    return Task.FromResult(
        (
            TransformerSignalScore: transformerSignal,
            Explanation: $"Transformer-only ClinicalBERT anchor-similarity score: {transformerSignal:0.00}. This compares the request text embedding against five severity anchor embeddings.",
            ModelName: "ClinicalBERT-ONNX-AnchorSimilarity-TransformerOnly"
        ));
}

private decimal CalculateAnchorSimilarityTransformerScore(string text)
{
    if (_session is null || _tokenizer is null)
    {
        return 50m;
    }

    EnsureSeverityAnchorEmbeddings();

    if (_severityAnchorEmbeddings is null ||
        _severityAnchorEmbeddings.Count == 0)
    {
        return 50m;
    }

    var inputEmbedding = GetTextEmbedding(text);

    if (inputEmbedding.Length == 0)
    {
        return 50m;
    }

    var similarities = _severityAnchorEmbeddings
        .Select(anchor => new
        {
            anchor.Label,
            anchor.Score,
            Similarity = CosineSimilarity(inputEmbedding, anchor.Embedding)
        })
        .ToList();

    var probabilities = SoftmaxSimilarities(
        similarities.Select(item => item.Similarity).ToArray());

    if (probabilities.Length == 0)
    {
        return 50m;
    }

    decimal finalScore = 0;

    for (var index = 0; index < similarities.Count; index++)
    {
        finalScore += (decimal)probabilities[index] * similarities[index].Score;
    }

    return Math.Round(
        Math.Clamp(finalScore, 0m, 100m),
        2);
}

private void EnsureSeverityAnchorEmbeddings()
{
    if (_severityAnchorEmbeddings is not null)
    {
        return;
    }

    lock (_severityAnchorLock)
    {
        if (_severityAnchorEmbeddings is not null)
        {
            return;
        }

        _severityAnchorEmbeddings = SeverityAnchors
            .Select(anchor => new SeverityAnchorEmbedding
            {
                Label = anchor.Label,
                Score = anchor.Score,
                Embedding = GetTextEmbedding(anchor.Text)
            })
            .Where(anchor => anchor.Embedding.Length > 0)
            .ToList();
    }
}

private float[] GetTextEmbedding(string text)
{
    if (_session is null || _tokenizer is null)
    {
        throw new InvalidOperationException(
            "ClinicalBERT ONNX model is not loaded.");
    }

    var maxSequenceLength = _options.MaxSequenceLength <= 0
        ? 128
        : _options.MaxSequenceLength;

    var tokenized = _tokenizer.Tokenize(
        text,
        maxSequenceLength);

    var inputIds = CreateTensor(
        tokenized.InputIds,
        maxSequenceLength);

    var attentionMask = CreateTensor(
        tokenized.AttentionMask,
        maxSequenceLength);

    var tokenTypeIds = CreateTensor(
        tokenized.TokenTypeIds,
        maxSequenceLength);

    var inputNames = _session.InputMetadata.Keys.ToList();

    var inputs = new List<NamedOnnxValue>();

    if (inputNames.Contains("input_ids"))
    {
        inputs.Add(
            NamedOnnxValue.CreateFromTensor(
                "input_ids",
                inputIds));
    }

    if (inputNames.Contains("attention_mask"))
    {
        inputs.Add(
            NamedOnnxValue.CreateFromTensor(
                "attention_mask",
                attentionMask));
    }

    if (inputNames.Contains("token_type_ids"))
    {
        inputs.Add(
            NamedOnnxValue.CreateFromTensor(
                "token_type_ids",
                tokenTypeIds));
    }

    if (inputs.Count == 0)
    {
        throw new InvalidOperationException(
            "ClinicalBERT ONNX model did not expose expected input names: input_ids, attention_mask, token_type_ids.");
    }

    using var results = _session.Run(inputs);

    return ExtractEmbeddingVector(results);
}

private static DenseTensor<long> CreateTensor(
    long[] values,
    int maxSequenceLength)
{
    return new DenseTensor<long>(
        values,
        new[]
        {
                    1,
                    maxSequenceLength
        });
}

private static string BuildClinicalText(
    string? remarks,
    string? surgeryType,
    string? priority,
    string? patientReadiness)
{
    var parts = new[]
    {
                "Clinical remarks:",
                remarks,
                "Surgery:",
                surgeryType,
                "Priority:",
                priority,
                "Readiness:",
                patientReadiness
            };

    return string.Join(
        " ",
        parts.Where(value => !string.IsNullOrWhiteSpace(value)));
}

private static float[] ExtractEmbeddingVector(
    IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
{
    var poolerOutput = results.FirstOrDefault(result =>
        string.Equals(
            result.Name,
            "pooler_output",
            StringComparison.OrdinalIgnoreCase));

    if (poolerOutput is not null)
    {
        return poolerOutput
            .AsTensor<float>()
            .ToArray();
    }

    var lastHiddenState = results.FirstOrDefault(result =>
        result.Name.Contains(
            "last_hidden_state",
            StringComparison.OrdinalIgnoreCase));

    var selectedOutput = lastHiddenState ?? results.FirstOrDefault();

    if (selectedOutput is null)
    {
        return Array.Empty<float>();
    }

    var tensor = selectedOutput.AsTensor<float>();
    var dimensions = tensor.Dimensions.ToArray();
    var values = tensor.ToArray();

    if (values.Length == 0)
    {
        return Array.Empty<float>();
    }

    if (dimensions.Length == 3)
    {
        var hiddenSize = dimensions[2];

        return values
            .Take(hiddenSize)
            .ToArray();
    }

    if (dimensions.Length == 2)
    {
        var hiddenSize = dimensions[1];

        return values
            .Take(hiddenSize)
            .ToArray();
    }

    return values
        .Take(Math.Min(values.Length, 768))
        .ToArray();
}

private static double CosineSimilarity(
    float[] left,
    float[] right)
{
    var length = Math.Min(left.Length, right.Length);

    if (length == 0)
    {
        return 0;
    }

    double dotProduct = 0;
    double leftNorm = 0;
    double rightNorm = 0;

    for (var index = 0; index < length; index++)
    {
        dotProduct += left[index] * right[index];
        leftNorm += left[index] * left[index];
        rightNorm += right[index] * right[index];
    }

    if (leftNorm == 0 || rightNorm == 0)
    {
        return 0;
    }

    return dotProduct / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
}

private static double[] SoftmaxSimilarities(double[] similarities)
{
    if (similarities.Length == 0)
    {
        return Array.Empty<double>();
    }

    const double temperature = 0.05;

    var logits = similarities
        .Select(similarity => similarity / temperature)
        .ToArray();

    var maxLogit = logits.Max();

    var exponents = logits
        .Select(logit => Math.Exp(logit - maxLogit))
        .ToArray();

    var sum = exponents.Sum();

    if (sum == 0)
    {
        return similarities
            .Select(_ => 1.0 / similarities.Length)
            .ToArray();
    }

    return exponents
        .Select(value => value / sum)
        .ToArray();
}

public void Dispose()
{
    _session?.Dispose();
}

private sealed class SeverityAnchorEmbedding
{
    public string Label { get; init; } = string.Empty;

    public decimal Score { get; init; }

    public float[] Embedding { get; init; } = Array.Empty<float>();
}
    }
}

