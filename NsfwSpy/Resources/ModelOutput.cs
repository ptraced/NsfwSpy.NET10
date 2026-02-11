namespace NsfwSpyNS;

internal class ModelOutput
{
    public string PredictedLabel { get; set; } = string.Empty;
    public float[] Score { get; set; } = [];
}
