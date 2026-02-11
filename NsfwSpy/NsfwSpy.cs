using HeyRed.Mime;
using ImageMagick;
using Microsoft.ML;
using System.Collections.Concurrent;

namespace NsfwSpyNS;

/// <summary>
/// The NsfwSpy classifier class used to analyse images for explicit content.
/// </summary>
public class NsfwSpy : INsfwSpy
{
    private static ITransformer? _model;
    private static readonly HttpClient _defaultHttpClient = new();

    public NsfwSpy()
    {
        if (_model is null)
        {
            var modelPath = Path.Combine(AppContext.BaseDirectory, "NsfwSpyModel.zip");
            var mlContext = new MLContext();
            _model = mlContext.Model.Load(modelPath, out _);
        }
    }

    /// <summary>
    /// Classify an image from a byte array.
    /// </summary>
    /// <param name="imageData">The image content read as a byte array.</param>
    /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
    public NsfwSpyResult ClassifyImage(byte[] imageData)
    {
        var fileType = MimeGuesser.GuessFileType(imageData);
        if (fileType.Extension == "webp")
        {
            using var image = new MagickImage(imageData);
            imageData = image.ToByteArray(MagickFormat.Png);
        }

        var modelInput = new ModelInput(imageData);
        var mlContext = new MLContext();
        var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
        var modelOutput = predictionEngine.Predict(modelInput);
        return new NsfwSpyResult(modelOutput);
    }

    /// <summary>
    /// Classify an image from a file path.
    /// </summary>
    /// <param name="filePath">Path to the image to be classified.</param>
    /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
    public NsfwSpyResult ClassifyImage(string filePath)
    {
        var fileBytes = File.ReadAllBytes(filePath);
        return ClassifyImage(fileBytes);
    }

    /// <summary>
    /// Classify an image from a web url.
    /// </summary>
    /// <param name="uri">Web address of the image to be classified.</param>
    /// <param name="httpClient">A custom HttpClient to download the image with.</param>
    /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
    public NsfwSpyResult ClassifyImage(Uri uri, HttpClient? httpClient = null)
    {
        httpClient ??= _defaultHttpClient;
        var fileBytes = httpClient.GetByteArrayAsync(uri).GetAwaiter().GetResult();
        return ClassifyImage(fileBytes);
    }

    /// <summary>
    /// Classify an image from a file path asynchronously.
    /// </summary>
    /// <param name="filePath">Path to the image to be classified.</param>
    /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
    public async Task<NsfwSpyResult> ClassifyImageAsync(string filePath)
    {
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        return ClassifyImage(fileBytes);
    }

    /// <summary>
    /// Classify an image from a web url asynchronously.
    /// </summary>
    /// <param name="uri">Web address of the image to be classified.</param>
    /// <param name="httpClient">A custom HttpClient to download the image with.</param>
    /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
    public async Task<NsfwSpyResult> ClassifyImageAsync(Uri uri, HttpClient? httpClient = null)
    {
        httpClient ??= _defaultHttpClient;
        var fileBytes = await httpClient.GetByteArrayAsync(uri);
        return ClassifyImage(fileBytes);
    }

    /// <summary>
    /// Classify multiple images from a list of file paths.
    /// </summary>
    /// <param name="filesPaths">Collection of file paths to be classified.</param>
    /// <param name="actionAfterEachClassify">Action to be invoked after each file is classified.</param>
    /// <returns>A list of results with their associated file paths.</returns>
    public List<NsfwSpyValue> ClassifyImages(IEnumerable<string> filesPaths, Action<string, NsfwSpyResult>? actionAfterEachClassify = null)
    {
        var results = new ConcurrentBag<NsfwSpyValue>();
        var sync = new object();

        Parallel.ForEach(filesPaths, filePath =>
        {
            var result = ClassifyImage(filePath);
            results.Add(new NsfwSpyValue(filePath, result));

            if (actionAfterEachClassify is not null)
            {
                lock (sync)
                {
                    actionAfterEachClassify.Invoke(filePath, result);
                }
            }
        });

        return [.. results];
    }

    /// <summary>
    /// Classify a .gif file from a byte array.
    /// </summary>
    /// <param name="gifImage">The Gif content read as a byte array.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public NsfwSpyFramesResult ClassifyGif(byte[] gifImage, VideoOptions? videoOptions = null)
    {
        videoOptions ??= new VideoOptions();

        if (videoOptions.ClassifyEveryNthFrame < 1)
            throw new ArgumentOutOfRangeException(nameof(videoOptions), "VideoOptions.ClassifyEveryNthFrame must not be less than 1.");

        var results = new ConcurrentDictionary<int, NsfwSpyResult>();

        using var collection = new MagickImageCollection(gifImage);
        collection.Coalesce();
        var frameCount = collection.Count;

        Parallel.For(0, frameCount, (i, state) =>
        {
            if (i % videoOptions.ClassifyEveryNthFrame != 0)
                return;

            if (state.ShouldExitCurrentIteration)
                return;

            var frame = collection[i];
            var frameData = frame.ToByteArray();
            var result = ClassifyImage(frameData);
            results.TryAdd(i, result);

            if (result.IsNsfw && videoOptions.EarlyStopOnNsfw)
                state.Break();
        });

        var resultDictionary = results.OrderBy(r => r.Key).ToDictionary(r => r.Key, r => r.Value);
        return new NsfwSpyFramesResult(resultDictionary);
    }

    /// <summary>
    /// Classify a .gif file from a path.
    /// </summary>
    /// <param name="filePath">Path to the .gif to be classified.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public NsfwSpyFramesResult ClassifyGif(string filePath, VideoOptions? videoOptions = null)
    {
        var gifImage = File.ReadAllBytes(filePath);
        return ClassifyGif(gifImage, videoOptions);
    }

    /// <summary>
    /// Classify a .gif file from a web url.
    /// </summary>
    /// <param name="uri">Web address of the Gif to be classified.</param>
    /// <param name="httpClient">A custom HttpClient to download the Gif with.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public NsfwSpyFramesResult ClassifyGif(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null)
    {
        httpClient ??= _defaultHttpClient;
        var gifImage = httpClient.GetByteArrayAsync(uri).GetAwaiter().GetResult();
        return ClassifyGif(gifImage, videoOptions);
    }

    /// <summary>
    /// Classify a .gif file from a path asynchronously.
    /// </summary>
    /// <param name="filePath">Path to the .gif to be classified.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public async Task<NsfwSpyFramesResult> ClassifyGifAsync(string filePath, VideoOptions? videoOptions = null)
    {
        var gifImage = await File.ReadAllBytesAsync(filePath);
        return ClassifyGif(gifImage, videoOptions);
    }

    /// <summary>
    /// Classify a .gif file from a web url asynchronously.
    /// </summary>
    /// <param name="uri">Web address of the Gif to be classified.</param>
    /// <param name="httpClient">A custom HttpClient to download the Gif with.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public async Task<NsfwSpyFramesResult> ClassifyGifAsync(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null)
    {
        httpClient ??= _defaultHttpClient;
        var gifImage = await httpClient.GetByteArrayAsync(uri);
        return ClassifyGif(gifImage, videoOptions);
    }

    /// <summary>
    /// Classify a video file from a byte array.
    /// </summary>
    /// <param name="video">The video content read as a byte array.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public NsfwSpyFramesResult ClassifyVideo(byte[] video, VideoOptions? videoOptions = null)
    {
        videoOptions ??= new VideoOptions();

        if (videoOptions.ClassifyEveryNthFrame < 1)
            throw new ArgumentOutOfRangeException(nameof(videoOptions), "VideoOptions.ClassifyEveryNthFrame must not be less than 1.");

        var results = new ConcurrentDictionary<int, NsfwSpyResult>();

        using var collection = new MagickImageCollection(video, MagickFormat.Mp4);
        collection.Coalesce();
        var frameCount = collection.Count;

        Parallel.For(0, frameCount, (i, state) =>
        {
            if (i % videoOptions.ClassifyEveryNthFrame != 0)
                return;

            if (state.ShouldExitCurrentIteration)
                return;

            var frame = collection[i];
            frame.Format = MagickFormat.Jpg;

            var result = ClassifyImage(frame.ToByteArray());
            results.TryAdd(i, result);

            if (result.IsNsfw && videoOptions.EarlyStopOnNsfw)
                state.Break();
        });

        var resultDictionary = results.OrderBy(r => r.Key).ToDictionary(r => r.Key, r => r.Value);
        return new NsfwSpyFramesResult(resultDictionary);
    }

    /// <summary>
    /// Classify a video file from a path.
    /// </summary>
    /// <param name="filePath">Path to the video to be classified.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public NsfwSpyFramesResult ClassifyVideo(string filePath, VideoOptions? videoOptions = null)
    {
        var video = File.ReadAllBytes(filePath);
        return ClassifyVideo(video, videoOptions);
    }

    /// <summary>
    /// Classify a video file from a web url.
    /// </summary>
    /// <param name="uri">Web address of the video to be classified.</param>
    /// <param name="httpClient">A custom HttpClient to download the video with.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public NsfwSpyFramesResult ClassifyVideo(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null)
    {
        httpClient ??= _defaultHttpClient;
        var video = httpClient.GetByteArrayAsync(uri).GetAwaiter().GetResult();
        return ClassifyVideo(video, videoOptions);
    }

    /// <summary>
    /// Classify a video file from a path asynchronously.
    /// </summary>
    /// <param name="filePath">Path to the video to be classified.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public async Task<NsfwSpyFramesResult> ClassifyVideoAsync(string filePath, VideoOptions? videoOptions = null)
    {
        var video = await File.ReadAllBytesAsync(filePath);
        return ClassifyVideo(video, videoOptions);
    }

    /// <summary>
    /// Classify a video file from a web url asynchronously.
    /// </summary>
    /// <param name="uri">Web address of the video to be classified.</param>
    /// <param name="httpClient">A custom HttpClient to download the video with.</param>
    /// <param name="videoOptions">VideoOptions to customise how the frames of the file are classified.</param>
    /// <returns>A NsfwSpyFramesResult with results for each frame classified.</returns>
    public async Task<NsfwSpyFramesResult> ClassifyVideoAsync(Uri uri, HttpClient? httpClient = null, VideoOptions? videoOptions = null)
    {
        httpClient ??= _defaultHttpClient;
        var video = await httpClient.GetByteArrayAsync(uri);
        return ClassifyVideo(video, videoOptions);
    }
}
