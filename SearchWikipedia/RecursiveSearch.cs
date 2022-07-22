using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SearchWikipedia
{
    public class RecursiveSearch
    {
        public class WikiPathResult
        {
            public string Path { get; set; }
            public int DeepLevel { get; set; }
            public TimeSpan Time { get; set; }
        }

        private const string siteWikipedia = "https://pt.wikipedia.org";
        private readonly string[] StringToSkipInUrl = { ":", "#", "index.php", "wiktionary" };
        private readonly Regex regexMainContentDiv;
        private readonly Regex regexAnchorHtmlTag;
        private readonly HashSet<string> visitedWikis;
        private readonly PriorityQueue<Func<Task<bool>>, int> wikisToVisitQueue;
        private readonly Stopwatch timer;
        private readonly HttpClient httpClient;
        private string finalWiki;
        private string startWiki;
        private readonly int skipInitialString = "<a href=\"/".Length;
        private int SizeOfMultipleTaskQueusAtTheSameTime = 10;
        private static WikiPathResult? wikiPathResult = null;
        private static List<Task<bool>> runningThread;

        public RecursiveSearch()
        {
            regexMainContentDiv = new Regex("<div id=\"bodyContent\"(.|\n)*?<footer");
            regexAnchorHtmlTag = new Regex("<a href=\"(.|\n)*?\"");
            visitedWikis = new HashSet<string>();
            visitedWikis = new HashSet<string>();
            wikisToVisitQueue = new PriorityQueue<Func<Task<bool>>, int>();
            httpClient = new HttpClient();
            timer = new Stopwatch();
            runningThread = new List<Task<bool>>();
        }

        public async Task<WikiPathResult> StartSearch(string initialWiki_URL, string finalWiki_URL, int numberOfMultipleTaskAtTheSameTime = 10)
        {
            timer.Start();
            SizeOfMultipleTaskQueusAtTheSameTime = numberOfMultipleTaskAtTheSameTime;
            startWiki = initialWiki_URL;
            finalWiki = finalWiki_URL;

            bool foundUrlsToVisit = await GetAllHrefUrlsFromAnchorHtmlTags(startWiki, initialWiki_URL);

            if (foundUrlsToVisit)
            {
                await VisitWikiUntilFindFinalWiki();
            }

            timer.Stop();
            return wikiPathResult;
        }

        private async Task VisitWikiUntilFindFinalWiki()
        {
            while (runningThread.Count < SizeOfMultipleTaskQueusAtTheSameTime)
            {
                if (wikisToVisitQueue.Count == 0)
                    break;
                Func<Task<bool>> action = wikisToVisitQueue.Dequeue();
                if (action is null)
                    continue;
                var actionTask = action();
                runningThread.Add(actionTask);
                actionTask.ContinueWith(taskDone => runningThread.Remove(taskDone));
            }
            if (runningThread.Any())
                await Task.WhenAll(runningThread?.ToArray());
            if (wikiPathResult is not null)
                return;
            await VisitWikiUntilFindFinalWiki();
        }

        private async Task<bool> GetAllHrefUrlsFromAnchorHtmlTags(string url, string caminho, int deepLevel = 0)
        {
            string html = await httpClient.GetStringAsync(url);
            string htmlContentWithoutLineBreak = html.Replace("\n", " ");

            string? mainContentString = regexMainContentDiv.Matches(htmlContentWithoutLineBreak)?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(mainContentString))
                return false;

            MatchCollection? HtmlAnchorMatches = regexAnchorHtmlTag.Matches(mainContentString);

            if (HtmlAnchorMatches.Count == 0)
                return false;

            foreach (Match match in HtmlAnchorMatches.ToArray())
            {
                string link = match.Value.Substring(skipInitialString - 1, match.Value.Length - skipInitialString);
                if (StringToSkipInUrl.Any(a => link.Contains(a)))
                    continue;

                if (!link.StartsWith("https://"))
                    link = $"{siteWikipedia}{link}";

                if (link == finalWiki)
                {
                    wikiPathResult = new WikiPathResult()
                    {
                        DeepLevel = deepLevel + 1,
                        Path = caminho + " > " + link,
                        Time = timer.Elapsed
                    };
                }

                if (!visitedWikis.Add(link))
                    continue;

                wikisToVisitQueue.Enqueue(() => GetAllHrefUrlsFromAnchorHtmlTags(link, caminho + " > " + link, deepLevel + 1), deepLevel);
            }

            return true;
        }
    }
}
