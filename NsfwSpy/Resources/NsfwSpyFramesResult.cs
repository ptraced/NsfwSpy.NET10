namespace NsfwSpyNS;

/// <summary>
/// The result from classifying a Gif or video file.
/// </summary>
public class NsfwSpyFramesResult(Dictionary<int, NsfwSpyResult> frames)
{
    /// <summary>
    /// The NsfwSpyResults for each of the frames classified with the key being the frame index.
    /// </summary>
    public Dictionary<int, NsfwSpyResult> Frames { get; set; } = frames;

    /// <summary>
    /// The amount of frames classified.
    /// </summary>
    public int FrameCount => Frames.Count;

    /// <summary>
    /// True if any of the frames have been classified as NSFW.
    /// </summary>
    public bool IsNsfw => Frames.Any(f => f.Value.IsNsfw);
}
