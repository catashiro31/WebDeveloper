using System.Text;
using System.Text.RegularExpressions;

namespace WebDeveloper.Moderation;

// Multinomial Naive Bayes, Laplace smoothing, uniform class priors.
// Scores rank moderation suggestions; they are not calibrated probabilities.
public sealed class ReviewClassifier
{
    public static ReviewClassifier Instance { get; } = new();
    private readonly string[] dictionary;
    private readonly Dictionary<string, Dictionary<string, int>> counts = new();
    private readonly Dictionary<string, int> totals = new();
    private readonly HashSet<string> vocabulary = new();
    private static string Normalize(string text) => text.Normalize(NormalizationForm.FormC).ToLowerInvariant();

    private ReviewClassifier()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Moderation");
        dictionary = File.ReadLines(Path.Combine(root, "vietnamese-offensive-words.txt"))
            .Select(s => s.Trim()).Where(s => s.Length > 1 && !s.StartsWith('#'))
            .Select(Normalize).Distinct().ToArray();
        foreach (var line in File.ReadLines(Path.Combine(root, "training.tsv")))
        {
            var row = line.Split('\t', 2);
            if (row.Length != 2) continue;
            if (!counts.ContainsKey(row[0])) { counts[row[0]] = new(); totals[row[0]] = 0; }
            foreach (var feature in Features(row[1]))
            {
                vocabulary.Add(feature);
                counts[row[0]][feature] = counts[row[0]].GetValueOrDefault(feature) + 1;
                totals[row[0]]++;
            }
        }
    }

    private IEnumerable<string> Features(string input)
    {
        var text = Normalize(input.Length > 10000 ? input[..10000] : input);
        var words = Regex.Matches(text, @"[\p{L}\p{M}\p{N}]+", RegexOptions.None, TimeSpan.FromSeconds(1))
            .Select(m => m.Value).Take(300).ToArray();
        foreach (var word in words) yield return "w:" + word;
        for (int i = 1; i < words.Length; i++) yield return "b:" + words[i - 1] + " " + words[i];
        var padded = " " + string.Join(' ', words) + " ";
        if (dictionary.Any(term => padded.Contains(" " + term + " ", StringComparison.Ordinal)))
            for (int i = 0; i < 5; i++) yield return "signal:lexicon";
        if (text.Contains("http://") || text.Contains("https://") || text.Contains("www.")) yield return "signal:link";
        if (words.GroupBy(w => w).Any(g => g.Count() >= 4))
            for (int i = 0; i < 3; i++) yield return "signal:repeat";
    }

    public Dictionary<string, double> Classify(string? text)
    {
        var features = Features(text ?? "").Where(vocabulary.Contains).ToArray();
        if (features.Length == 0) return counts.Keys.ToDictionary(k => k, k => k == "Thông thường" ? 100d : 0d);
        var logs = counts.ToDictionary(c => c.Key, c => features.Sum(f =>
            Math.Log((c.Value.GetValueOrDefault(f) + 1d) / (totals[c.Key] + vocabulary.Count))));
        var max = logs.Values.Max();
        var sum = logs.Values.Sum(v => Math.Exp(v - max));
        return logs.ToDictionary(c => c.Key, c => Math.Round(100 * Math.Exp(c.Value - max) / sum, 2));
    }

    public static string? Suggestion(Dictionary<string, double> scores)
    {
        var top = scores.MaxBy(s => s.Value);
        return top.Key != "Thông thường" && top.Value >= 60 ? top.Key : null;
    }
}
