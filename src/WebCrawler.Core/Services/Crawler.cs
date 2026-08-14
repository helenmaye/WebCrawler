using AngleSharp.Dom;
using WebCrawler.Core.Interfaces;

public class Crawler(IPageFetcher fetcher, IProcessHTML processor)
{

    public async Task Run()
    {
        Console.WriteLine("run program y/n");

        var response = Console.ReadLine();

        if (response == "y" || response == "Y")
        {
            //var html = await fetcher.GetHtmlAsStringAsync("https://www.bbc.co.uk/");

            //var res = processor.ExtractLinks(html, new Uri("https://www.bbc.co.uk/"));

            await FindAllLinks(new Uri("https://www.bbc.co.uk/"), new CancellationToken());

        }
    }

    // public async Task<IEnumerable<Uri>> FindAllLinks(Uri url, CancellationToken ct)
    // {
    //     var fetchResult = await fetcher.FetchAsync(url, ct);
    //     var results = new List<Uri>();
    //     if (fetchResult.Success && !string.IsNullOrEmpty(fetchResult.Html))
    //     {
    //         foreach (var link in processor.ExtractLinks(fetchResult.Html, url))
    //         {
    //             results.Add(link);
    //         }
    //     }
    //     return results;
    // }

    public async Task FindAllLinks(Uri url, CancellationToken ct)
    {
        var fetchResult = await fetcher.FetchAsync(url, ct);
        if (fetchResult.Success && !string.IsNullOrEmpty(fetchResult.Html))
        {
            foreach (var link in processor.ExtractLinks(fetchResult.Html, url))
            {
                Console.WriteLine(link);
            }
        }
    }
}