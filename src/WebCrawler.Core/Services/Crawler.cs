using System.Collections.Concurrent;
using System.Threading.Channels;
using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Models;
using Microsoft.Extensions.Options;

namespace WebCrawler.Core.Services;

public class Crawler(IPageFetcher fetcher, IProcessHtml processor, IOptions<ConfigurationOptions> options)
{
    public async Task Run()
    {
        Console.WriteLine("Please enter starting URL");

        var url = Console.ReadLine();
        if (!string.IsNullOrEmpty(url))
        {
            await Start(url);
        }
    }

    public async Task<ConcurrentBag<Uri>> Start(string seedUrl)
    {
        var discovered = new ConcurrentBag<Uri>();

        var channel = Channel.CreateBounded<Uri>(new BoundedChannelOptions(options.Value.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        
        var inFlight = 0;
        
        await Enqueue(new Uri(seedUrl));

        var workers = Enumerable.Range(0, options.Value.MaxConcurrency).Select(_ => Task.Run(async () => 
        {
            await foreach (var url in channel.Reader.ReadAllAsync())
            {
                try
                {
                    await ProcessUrl(url, discovered, Enqueue);
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
        return discovered;
        
        async Task Enqueue(Uri uri)
        {
            Interlocked.Increment(ref inFlight);
            discovered.Add(uri);
            await channel.Writer.WriteAsync(uri);
        }
    }

    private async Task ProcessUrl(Uri url, ConcurrentBag<Uri> discovered, Func<Uri, Task> enqueue)
    {
        var fetchResult = await fetcher.FetchAsync(url);

        if (fetchResult.Success && !string.IsNullOrEmpty(fetchResult.Html))
        {
            foreach (var link in processor.ExtractLinks(fetchResult.Html, url))
            {
                if (discovered.Contains(link))
                {
                    continue;
                }
                await enqueue(link);
            }
        }
        
    }
}