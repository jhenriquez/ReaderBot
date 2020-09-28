using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using JustEat.HttpClientInterception;
using CleanReaderBot.Application.Common.Interfaces;
using CleanReaderBot.Application.SearchBooksByFields;


namespace CleanReaderBot.Application.Goodreads.Tests
{
  public class GoodreadsBookProviderTest
  {
    private readonly IServiceProvider provider;
    private readonly HttpClientInterceptorOptions interceptor;

    public GoodreadsBookProviderTest(IServiceProvider provider, HttpClientInterceptorOptions interceptor) {
      this.provider = provider;
      this.interceptor = interceptor;
    }

    private Stream OpenFile(string path)
    {
      return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    [Fact]
    public async Task GoodreadsBookProvider__Search__Returns_A_List_Of_Books() {
      using(this.interceptor.BeginScope()) {
        var builder = new HttpRequestInterceptionBuilder()
          .Requests()
          .For((req) => req.RequestUri.ToString().Contains("goodreads.com/search/index.xml"))
          .Responds()
          .WithContentStream(() => Task.FromResult(OpenFile("Fixtures/EndersGame_Response.xml")))
          .RegisterWith(this.interceptor);

        var searchBooksHandler = this.provider.GetService<IHandler<SearchBooks, SearchBooksResult>>();
        
        var searchBookQuery = new SearchBooks("Ender's Game");
        
        var result = await searchBooksHandler.Execute(searchBookQuery);
        
        result.Items.Should().HaveCount(20);
      }
    }
  }
}