using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebCrawler.Core;
using WebCrawler.Core.Interfaces;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<IPageFetcher, HttpPageFetcher>();
builder.Services.AddScoped<IProcessHTML, ProcessHTML>();
builder.Services.AddScoped<IBuildUri, BuildUri>();
builder.Services.AddScoped<IHtmlFetcher, HtmlFetcher>();
builder.Services.AddHttpClient<IPageFetcher, HttpPageFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<Crawler>();

var provider = builder.Services.BuildServiceProvider();

var app = provider.GetRequiredService<Crawler>();
await app.Run();

