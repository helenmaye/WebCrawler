using System.Collections.Concurrent;
using System.Net;
using WebCrawler.Core.Models;

namespace WebCrawler.Core.Services;

public static class ResultsWriter
{
    public static void OutputResults(ConcurrentDictionary<Uri, CrawlResult> results)
    {
        foreach (var result in results)
        {
            Console.WriteLine();
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine($"Page : {result.Key}");
            Console.WriteLine(result.Value.StatusCode == HttpStatusCode.OK
                ? $"Status : {result.Value.StatusCode.ToString()}"
                : $"Status : {result.Value.StatusCode.ToString()} - {result.Value.Error}");
            
            if (result.Value.Links is null)
            {
                Console.WriteLine("No links found");
            }
            else
            {
                Console.WriteLine($"Links found :");
                foreach (var link in result.Value.Links)
                {
                    Console.WriteLine($"  - {link}");
                }
            }
        }
    }
}