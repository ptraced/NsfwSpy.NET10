using HeyRed.Mime;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace NsfwSpyNS.App.Controllers;

[ApiController]
[Route("[controller]")]
public class NsfwSpyController(INsfwSpy nsfwSpy, IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("url/{url}")]
    public async Task<FileContentResult> GetUrlMediaAsync(string url)
    {
        url = HttpUtility.UrlDecode(url);
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:140.0) Gecko/20100101 Firefox/140.0");
        var byteArray = await httpClient.GetByteArrayAsync(url);
        var mimeType = MimeGuesser.GuessMimeType(byteArray);
        return new FileContentResult(byteArray, mimeType);
    }

    [HttpPost("image")]
    public ActionResult<NsfwSpyResult> ClassifyImage(IFormFile file)
    {
        var fileBytes = ConvertFormFileToByteArray(file);
        var result = nsfwSpy.ClassifyImage(fileBytes);
        return Ok(result);
    }

    [HttpPost("gif")]
    public ActionResult<NsfwSpyFramesResult> ClassifyGif(IFormFile file)
    {
        var fileBytes = ConvertFormFileToByteArray(file);
        var videoOptions = new VideoOptions
        {
            EarlyStopOnNsfw = false
        };
        var result = nsfwSpy.ClassifyGif(fileBytes, videoOptions);
        return Ok(result);
    }

    [HttpPost("video")]
    public ActionResult<NsfwSpyFramesResult> ClassifyVideo(IFormFile file)
    {
        var fileBytes = ConvertFormFileToByteArray(file);
        var videoOptions = new VideoOptions
        {
            EarlyStopOnNsfw = false
        };
        var result = nsfwSpy.ClassifyVideo(fileBytes, videoOptions);
        return Ok(result);
    }

    private static byte[] ConvertFormFileToByteArray(IFormFile file)
    {
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        return ms.ToArray();
    }
}

public class MediaInfo
{
    public IFormFile? File { get; set; }
    public string? MimeType { get; set; }
}
