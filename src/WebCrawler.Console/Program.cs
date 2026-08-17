using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawler.Console;
using WebCrawler.Core.Models;
using WebCrawler.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCrawlerServices();
builder.Services.Configure<ConfigurationOptions>(
    builder.Configuration.GetSection(nameof(ConfigurationOptions)));
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

var host = builder.Build();
var app = host.Services.GetRequiredService<Crawler>();
await app.Run();

