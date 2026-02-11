using NsfwSpyNS;
using NsfwSpyNS.PerformanceTesting;

var assetsPath = @"E:\NsfwSpy\Images";
var testImagesPath = @"E:\NsfwSpy\Test";

string[] classificationTypes = ["Hentai", "Neutral", "Pornography", "Sexy"];

var results = new List<PerformanceResult>();
var nsfwSpy = new NsfwSpy();

foreach (var classificationType in classificationTypes)
{
    var testFilesDirectory = Path.Combine(testImagesPath, classificationType);
    var testFiles = Directory.Exists(testFilesDirectory)
        ? Directory.GetFiles(testFilesDirectory).ToList()
        : [];

    if (testFiles.Count == 0)
    {
        var directory = Path.Combine(assetsPath, classificationType);
        var files = Directory.GetFiles(directory).OrderBy(_ => Guid.NewGuid()).ToList();

        var length = Math.Min(files.Count, 10000);
        files = files.Take(length).ToList();

        Console.WriteLine($"Copying {classificationType} test files...");

        if (!Directory.Exists(testFilesDirectory))
            Directory.CreateDirectory(testFilesDirectory);

        Parallel.ForEach(files, file =>
        {
            var filename = Path.GetFileName(file);
            var dest = Path.Combine(testFilesDirectory, filename);
            File.Copy(file, dest);
        });

        testFiles = [.. Directory.GetFiles(testFilesDirectory)];
    }

    var pr = new PerformanceResult(classificationType);

    nsfwSpy.ClassifyImages(testFiles, (filePath, result) =>
    {
        pr.Results.Add(result);
        Console.WriteLine($"{pr.Key} | Correct Asserts: {pr.CorrectAsserts} / {pr.TotalAsserts} ({(double)pr.CorrectAsserts / pr.TotalAsserts * 100}%) | IsNsfw: {pr.NsfwAsserts} / {pr.TotalAsserts} ({(double)pr.NsfwAsserts / pr.TotalAsserts * 100}%)");
    });

    results.Add(pr);
}

Console.WriteLine(Environment.NewLine);
foreach (var pr in results)
{
    Console.WriteLine($"{pr.Key} | Correct Asserts: {pr.CorrectAsserts} / {pr.TotalAsserts} ({(double)pr.CorrectAsserts / pr.TotalAsserts * 100}%) | IsNsfw: {pr.NsfwAsserts} / {pr.TotalAsserts} ({(double)pr.NsfwAsserts / pr.TotalAsserts * 100}%)");
}

Console.WriteLine(Environment.NewLine);
Console.WriteLine("Confusion Matrix\n");

Console.WriteLine("\t\t\tPredicted Label");
Console.WriteLine("Actual Label\t\tHentai\t\tNeutral\t\tPornography\tSexy");
Console.WriteLine();
foreach (var pr in results)
{
    Console.WriteLine($"{pr.Key}\t\t{(pr.Key != "Pornography" ? "\t" : "")}{pr.HentaiAsserts}\t\t{pr.NeutralAsserts}\t\t{pr.PornographyAsserts}\t\t{pr.SexyAsserts}");
}

Console.WriteLine(Environment.NewLine);
Console.WriteLine("Average Confidence\n");

foreach (var pr in results)
{
    Console.WriteLine($"{pr.Key}\t\t{(pr.Key != "Pornography" ? "\t" : "")}{pr.Results.Sum(r => r.ToDictionary()[pr.Key]) / pr.Results.Count}");
}
