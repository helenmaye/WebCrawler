using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebCrawler.Console;
using WebCrawler.Core.Models;
using WebCrawler.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCrawlerServices();
builder.Services.Configure<ConfigurationOptions>(
    builder.Configuration.GetSection(nameof(ConfigurationOptions)));

var provider = builder.Services.BuildServiceProvider();
builder.Build();

var app = provider.GetRequiredService<Crawler>();
await app.Run();

