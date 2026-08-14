// using AngleSharp;
// using Shouldly;
// using WebCrawler.Core;
// using WebCrawler.Core.Interfaces;

// namespace WebCrawler.Tests;

// public class ProcessHTMLTests
// {
//     [Fact]
//     public async Task GivenValidHTMLwithLinks_WhenGetLinks_ThenReturnsLinks()
//     {
//         //Arrange
//         IProcessHTML subject = new ProcessHTML();

//         var html = """
//                     <html>
//                     <head>
//                     <title>My Title</title>
//                     </head>
//                     <body>
//                     <p>Paragraph I <a href='/pages/link1'>JSON</a></p>
//                     <p>Paragraph II <a href='/pages/link2'>JSON</a></p>
//                     </body>
//                     </html>
//                 """;
        
//         var config = Configuration.Default;
//         using var context = BrowsingContext.New(config);
//         using var doc = await context.OpenAsync(req => req.Content(html));

//         //Act
//         var result = subject.GetLinks(doc);

//         //Assert
//         result.Count().ShouldBe(2);
//         result.FirstOrDefault().ShouldBe("http://localhost/pages/link1");
//         result.LastOrDefault().ShouldBe("http://localhost/pages/link2");
//     }



//     [Fact]
//     public async Task GivenValidHTMLwithNoLinks_WhenGetLinks_ThenReturnsEmptyEnumerable()
//     {
//         //Arrange
//         IProcessHTML subject = new ProcessHTML();

//         var html = """
//                     <html>
//                     <head>
//                     <title>My Title</title>
//                     </head>
//                     <body>
//                     <p>Paragraph I</p>
//                     <p>Paragraph I</p>
//                     </body>
//                     </html>
//                     """;
        
//         var config = Configuration.Default;
//         using var context = BrowsingContext.New(config);
//         using var doc = await context.OpenAsync(req => req.Content(html));

//         //Act
//         var result = subject.GetLinks(doc);

//         //Assert
//         result.Count().ShouldBe(0);
//     }
// }
