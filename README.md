# WebCrawler

A simple, single-subdomain web crawler. Given a starting URL, it visits every
page it can reach on that same host, printing each page visited along with
the links found on it.

## Running it

```
dotnet run --project src/WebCrawler.Console
```

You'll be prompted for a starting URL, e.g.:

```
Please enter starting URL
https://crawlme.monzo.com/
```

The `.../*` wildcard form from the task description is also accepted and
treated the same as the bare URL (the trailing `/*` is stripped).

## Running the tests

```
dotnet test
```

## Project structure

```
src/
  WebCrawler.Core/        crawling logic, no console or DI concerns
    Interfaces/           IPageFetcher, ILinkExtractor, INormaliser
    Models/                CrawlResult, FetchResult, ConfigurationOptions
    Services/              Crawler, HttpPageFetcher, LinkExtractor, Normaliser, ResultsWriter
  WebCrawler.Console/     composition root - DI wiring, entry point, console I/O
tests/
  WebCrawler.Tests/
    Services/              unit tests per component
    IntegrationTests/      full crawl behaviour against a mocked HttpClient
```

The split exists so that `WebCrawler.Core` has no dependency on the console
or on how it's hosted - it could be dropped behind a web API or a different
front end without changes.

### Key components

- **`Crawler`** - orchestrates the crawl: owns the work queue, concurrency,
  and deduplication. Delegates fetching to `IPageFetcher` and parsing to
  `ILinkExtractor`.
- **`HttpPageFetcher`** - thin wrapper over `HttpClient` (injected via
  `IHttpClientFactory`, so connections are pooled rather than creating a new
  `HttpClient` per request). Converts non-success responses and exceptions
  into a `FetchResult` rather than throwing, so one bad page can't take down
  the crawl.
- **`LinkExtractor`** - parses HTML with AngleSharp (a parsing library, not
  a crawling framework) and yields every `href` found, resolved to an
  absolute `Uri` via `INormaliser`.
- **`Normaliser`** - turns a raw `href` + base URL into a canonical absolute
  `Uri`, or `null` if it isn't something worth crawling (see "Design
  decisions" below).
- **`ResultsWriter`** - prints the final results: page, status, links found.

## Concurrency

The crawl is a producer/consumer pipeline over a `System.Threading.Channels`
channel:

- A fixed pool of worker tasks (`ConfigurationOptions.MaxConcurrency`, default
  10) read URLs from the channel and process them concurrently.
- Processing a page can enqueue more URLs (its same-host links), so the
  channel is also being written to by the same workers that are reading
  from it.
- Because the total amount of work isn't known upfront, completion is
  tracked with an in-flight counter: enqueuing a URL increments it, and
  finishing a URL decrements it. When it hits zero, every discovered page
  has been processed and the channel is closed. The increment for a page's
  children always happens before the decrement for that page (enqueuing
  happens *inside* processing, the decrement happens *after* processing
  returns), so the channel can't close while sibling work is still in
  flight.
- Deduplication uses a `ConcurrentDictionary<Uri, bool>` with `TryAdd` as an
  atomic "have I seen this?" check, so the same URL is never enqueued twice
  even when multiple pages link to it concurrently.

`ConfigurationOptions.MaxPages` (default 500) caps the crawl as a safety net
against unbounded sites.

## Design decisions and assumptions

These are documented in code as comments alongside the relevant tests, but
summarised here:

- **Same host, not same domain.** "Subdomain" is taken literally: a link is
  only followed if its host matches the starting URL's host exactly. A page
  linking from `crawlme.monzo.com` to `www.monzo.com` or `community.monzo.com`
  is recorded in that page's list of links but not crawled, per the brief.
- **Fragments are stripped.** `/page#section` and `/page` are treated as the
  same page, since the fragment identifies a location within a page rather
  than a different resource.
- **Query strings are treated as distinct pages.** `/page` and `/page?id=1`
  can represent meaningfully different content (pagination, filters, IDs),
  so they're not collapsed.
- **Trailing slashes are normalised away.** `/page` and `/page/` are treated
  as the same page. This is a simplification - a site that actually serves
  different content at those two paths would be mis-deduplicated - but it
  avoids crawling obvious duplicates on the vast majority of sites.
- **`javascript:`, `mailto:`, and `tel:` links are dropped** at the point of
  resolution, since they aren't crawlable resources.
- **A failed fetch doesn't propagate.** Non-2xx responses and request
  exceptions are captured as a `CrawlResult` with an error/status rather than
  throwing, so a broken page doesn't stop the rest of the crawl. Its links
  are consequently never discovered.

## Known limitations / what's next

Scoped out to stay within the ~4 hour guideline:

- **No `robots.txt` support.** A real crawler should respect it.
- **No per-host rate limiting or backoff.** `MaxConcurrency` bounds total
  parallelism but nothing throttles requests to a single slow or
  rate-limiting host, and there's no retry/backoff on 429s or transient
  errors.
- **No cancellation support.** The crawl can't be interrupted gracefully
  (e.g. Ctrl+C) once started.
- **No content-type checking.** Any response is treated as HTML and handed
  to the parser, so a link to a PDF or image would be fetched and parsed as
  (empty) HTML rather than skipped.
- **Output is print-to-console only** - no structured export (JSON/CSV/sitemap),
  since the brief was explicit that format wasn't the focus.
