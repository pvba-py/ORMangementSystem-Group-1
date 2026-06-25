namespace ORManagement.Application.DTOs.Requests;

public class ClinicalScoringOptions
{
    public bool UseOnnx { get; set; } = true;

    public string ModelPath { get; set; } = string.Empty;

    public string VocabPath { get; set; } = string.Empty;

    public int MaxSequenceLength { get; set; } = 128;
}