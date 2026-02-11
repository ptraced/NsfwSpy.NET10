namespace NsfwSpyNS;

public interface INsfwSpy
{
    NsfwSpyResult ClassifyImage(byte[] imageData);
    NsfwSpyResult ClassifyImage(string filePath);
    NsfwSpyResult ClassifyImage(Uri uri, HttpClient? httpClient = null);
    Task<NsfwSpyResult> ClassifyImageAsync(string filePath);
    Task<NsfwSpyResult> ClassifyImageAsync(Uri uri, HttpClient? httpClient = null);
    List<NsfwSpyValue> ClassifyImages(IEnumerable<string> filesPaths, Action<string, NsfwSpyResult>? actionAfterEachClassify = null);

    NsfwSpyFramesResult ClassifyGif(byte[] gifImage, VideoOptions? videoOptions = null);
    NsfwSpyFramesResult ClassifyGif(string filePath, VideoOptions? videoOptions = null);
    NsfwSpyFramesResult ClassifyGif(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null);
    Task<NsfwSpyFramesResult> ClassifyGifAsync(string filePath, VideoOptions? videoOptions = null);
    Task<NsfwSpyFramesResult> ClassifyGifAsync(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null);

    NsfwSpyFramesResult ClassifyVideo(byte[] video, VideoOptions? videoOptions = null);
    NsfwSpyFramesResult ClassifyVideo(string filePath, VideoOptions? videoOptions = null);
    NsfwSpyFramesResult ClassifyVideo(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null);
    Task<NsfwSpyFramesResult> ClassifyVideoAsync(string filePath, VideoOptions? videoOptions = null);
    Task<NsfwSpyFramesResult> ClassifyVideoAsync(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null);
}
