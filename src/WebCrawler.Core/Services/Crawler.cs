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
                url =  url[..^2];
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
                    if (AllQueuedItemsProcessed(ref inFlight))
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
            if (MaxPagesExceeded(ref pagesEnqueued) || Duplicate(discovered, uri))
            {
                return;
            }
            Interlocked.Increment(ref pagesEnqueued);
            Interlocked.Increment(ref inFlight);
            
            await WriteToChannel(channel, uri);
        }
    }

    private static bool AllQueuedItemsProcessed(ref int inFlight)
    {
        return Interlocked.Decrement(ref inFlight) == 0;
    }

    private static ValueTask WriteToChannel(Channel<Uri> channel, Uri uri)
    {
        return channel.Writer.WriteAsync(uri);
    }

    private static bool Duplicate(ConcurrentDictionary<Uri, bool> discovered, Uri uri)
    {
        return !discovered.TryAdd(uri, true);
    }

    private bool MaxPagesExceeded(ref int pagesEnqueued)
    {
        return Volatile.Read(ref pagesEnqueued) >= options.Value.MaxPages;
    }

    private async Task ProcessUrl(Uri url, ConcurrentDictionary<Uri, CrawlResult> crawlResults, Func<Uri, Task> enqueue)
    {
        var fetchResult = await fetcher.FetchAsync(url);
        var links = new List<string>();

        if (fetchResult.Success && !string.IsNullOrEmpty(fetchResult.Html))
        {
            await foreach (var link in processor.AsyncExtractLinks(fetchResult.Html, url))
            {
                if (UnaddedLink(links, link))
                {
                    links.Add(link.ToString());
                }
                
                if (HostIsHostBeingSearched(url, link))
                {
                    await enqueue(link);
                }
            }
        }
        crawlResults.TryAdd(url, new CrawlResult(links, fetchResult.StatusCode, fetchResult.Error));
    }

    private static bool HostIsHostBeingSearched(Uri url, Uri link)
    {
        return link.Host.Equals(url.Host, StringComparison.OrdinalIgnoreCase);
    }

    private static bool UnaddedLink(List<string> links, Uri link)
    {
        return !links.Contains(link.ToString());
    }
}