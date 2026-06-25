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
        private readonly ClinicalScoringOptions _options;
        private readonly FallbackClinicalKeywordScoringService _fallbackScoringService;
        private readonly ILogger<ClinicalBertOnnxTextScoringService> _logger;

        private readonly InferenceSession? _session;
        private readonly BertTokenizer? _tokenizer;
        private const string LowUrgencyAnchorText =
    "Patient is clinically stable. Routine elective procedure. No acute deterioration. No severe pain. No active bleeding. No sepsis. No neurological deficit.";

        private const string HighUrgencyAnchorText =
            "Patient has acute clinical deterioration, unstable condition, sepsis, active bleeding, worsening neurological deficit, severe pain, risk of permanent loss of function if delayed. Emergency surgery required.";

        private float[]? _lowUrgencyAnchorEmbedding;
        private float[]? _highUrgencyAnchorEmbedding;

        private readonly object _anchorLock = new();
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
                _logger.LogInformation("ClinicalBERT ONNX scoring is disabled. Fallback scorer will be used.");
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
                var maxSequenceLength = _options.MaxSequenceLength <= 0
                    ? 128
                    : _options.MaxSequenceLength;

                var text = BuildClinicalText(
                    remarks,
                    surgeryType,
                    priority,
                    patientReadiness);

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

                var transformerSignal = ExtractTransformerSignalScore(results);

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
                        $"{fallbackResult.Explanation} Transformer signal score: {transformerSignal:0.00}.",
                    ModelName = "ClinicalBERT-ONNX-DemoHybrid",
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

        private static decimal ExtractTransformerSignalScore(
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
        {
            var firstResult = results.FirstOrDefault();

            if (firstResult is null)
            {
                return 50m;
            }

            var tensor = firstResult.AsTensor<float>();
            var dimensions = tensor.Dimensions.ToArray();
            var values = tensor.ToArray();

            if (values.Length == 0)
            {
                return 50m;
            }

            /*
                Case 1:
                If model returns classifier logits shaped [1, 2],
                use positive class probability.
            */
            if (dimensions.Length == 2 &&
                dimensions[0] == 1 &&
                dimensions[1] == 2)
            {
                var logit0 = values[0];
                var logit1 = values[1];

                var max = Math.Max(logit0, logit1);

                var exp0 = Math.Exp(logit0 - max);
                var exp1 = Math.Exp(logit1 - max);

                var probability = exp1 / (exp0 + exp1);

                return Math.Round(
                    (decimal)(probability * 100.0),
                    2);
            }

            /*
                Case 2:
                Base BERT output is usually embeddings [1, sequence, hidden].
                Since this is not fine-tuned for urgency classification,
                this becomes a demo transformer activation signal.
            */
            var sampleSize = Math.Min(
                values.Length,
                768);

            var meanAbsoluteActivation = values
    .Take(sampleSize)
    .Select(Math.Abs)
    .Average();

            /*
                Demo calibration:
                Base ClinicalBERT embeddings usually produce stable activation magnitudes.
                This rescales the raw activation into a wider 0-100 range for demo readability.
            */
            var calibratedSignal = ((meanAbsoluteActivation - 0.20f) / 0.25f) * 100.0f;

            var signal = Math.Clamp(
                calibratedSignal,
                0f,
                100f);

            return Math.Round(
                (decimal)signal,
                2);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}