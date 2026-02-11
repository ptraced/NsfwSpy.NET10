namespace NsfwSpyNS.PerformanceTesting;

internal class PerformanceResult(string key)
{
    public string Key { get; set; } = key;
    public List<NsfwSpyResult> Results { get; set; } = [];
    public int CorrectAsserts => Results.Count(r => r.PredictedLabel == Key);
    public int NsfwAsserts => Results.Count(r => r.IsNsfw);
    public int HentaiAsserts => Results.Count(r => r.PredictedLabel == "Hentai");
    public int NeutralAsserts => Results.Count(r => r.PredictedLabel == "Neutral");
    public int PornographyAsserts => Results.Count(r => r.PredictedLabel == "Pornography");
    public int SexyAsserts => Results.Count(r => r.PredictedLabel == "Sexy");
    public int TotalAsserts => Results.Count;
}
