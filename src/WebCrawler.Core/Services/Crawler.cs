using System.Collections.Concurrent;
using System.Threading.Channels;
using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Models;
using Microsoft.Extensions.Options;

namespace WebCrawler.Core.Services;

public class Crawler(IPageFetcher fetcher, ILinkExtractor processor, IOptions<ConfigurationOptions> options)
{
    public async Task Run()
    {
        Console.WriteLine("Please enter starting URL");

        var url = Console.ReadLine();
        if (!string.IsNullOrEmpty(url))
        {
            if (url.EndsWith("/*"))
            {
                url =  url.Substring(0, url.Length - 2);
            }
            var results = await Start(url);
            ResultsWriter.OutputResults(results);
        }
    }

    public async Task<ConcurrentDictionary<Uri, CrawlResult>> Start(string seedUrl)
    {
        var crawlResults = new ConcurrentDictionary<Uri, CrawlResult>();
        var discovered = new ConcurrentDictionary<Uri, bool>();
        
        var channel = Channel.CreateUnbounded<Uri>();
        
        var inFlight = 0;
        var pagesEnqueued = 0;
        
        await Enqueue(new Uri(seedUrl));

        var workers = Enumerable.Range(0, options.Value.MaxConcurrency).Select(_ => Task.Run(async () => 
        {
            await foreach (var url in channel.Reader.ReadAllAsync())
            {
                try
                {
                    await ProcessUrl(url, crawlResults, Enqueue);
                }
                finally
                {
                    if (Interlocked.Decrement(ref inFlight) == 0)
                    {
                        channel.Writer.TryComplete();
                    }
                }
            }
        })).ToArray();

        await Task.WhenAll(workers);
        return crawlResults;
        
        async Task Enqueue(Uri uri)
        {
            if (Volatile.Read(ref pagesEnqueued) >= options.Value.MaxPages)
            {
                return; // cap reached, stop discovering new work
            }
            if (!discovered.TryAdd(uri, true))
            {
                return; // already added
            }
            Interlocked.Increment(ref pagesEnqueued);
            Interlocked.Increment(ref inFlight);
            await channel.Writer.WriteAsync(uri);
        }
    }

    private async Task ProcessUrl(Uri url, ConcurrentDictionary<Uri, CrawlResult> crawlResults, Func<Uri, Task> enqueue)
    {
        var fetchResult = await fetcher.FetchAsync(url);
        var links = new List<string>();

        if (fetchResult.Success && !string.IsNullOrEmpty(fetchResult.Html))
        {
            await foreach (var link in processor.AsyncExtractLinks(fetchResult.Html, url))
            {
                if (!links.Contains(link.ToString()))
                {
                    links.Add(link.ToString());
                }
                
                if (link.Host.Equals(url.Host, StringComparison.OrdinalIgnoreCase))
                {
                    await enqueue(link);
                }
            }
        }
        crawlResults.TryAdd(url, new CrawlResult(links, fetchResult.StatusCode, fetchResult.Error));
    }
}