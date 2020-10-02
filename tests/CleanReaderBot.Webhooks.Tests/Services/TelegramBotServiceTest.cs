using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanReaderBot.Application.Common.Entities;
using CleanReaderBot.Application.SearchForBooks;
using CleanReaderBot.Webhooks.Models;
using CleanReaderBot.Webhooks.Services;
using CleanReaderBot.Webhooks.Tests.Comparers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Xunit;

namespace CleanReaderBot.Webhooks.Tests.Services {
    public class TelegramBotServiceTest {
        private ITelegramBotClient TelegramBotClient;
        private IOptions<TelegramSettings> TelegramSettings;
        private ILogger<TelegramBotService> TelegramBotServiceLogger;

        private ITelegramBotService TelegramBotService;

        public TelegramBotServiceTest () {
            TelegramSettings = Options.Create<TelegramSettings> (new TelegramSettings {
                Token = "SomeToken",
                    WebhookUrl = "SomeWebhookUrl"
            });

            TelegramBotClient = Substitute.For<ITelegramBotClient> ();

            TelegramBotServiceLogger = Substitute.For<ILogger<TelegramBotService>> ();

            TelegramBotService = new TelegramBotService (TelegramBotClient, TelegramSettings, TelegramBotServiceLogger);
        }

        private Book GetExampleBook () {
            return new Book () {
                Id = 375802,
                Title = "Ender's Game (Ender's Saga, #1)",
                AverageRating = 4.30,
                Author = new Author {
                Id = 589,
                Name = "Orson Scott Card"
                },
                ImageUrl = "https://i.gr-assets.com/images/S/compressed.photo.goodreads.com/books/1408303130l/375802._SY160_.jpg",
                SmallImageUrl = "https://i.gr-assets.com/images/S/compressed.photo.goodreads.com/books/1408303130l/375802._SY75_.jpg"
            };
        }

        [Fact]
        public async Task TelegramBotService__StartWebhook__Uses_SetWebhookAsync_With_The_Given_WebhookUrl () {
            await TelegramBotService.StartWebhook ();
            await TelegramBotClient.Received ().SetWebhookAsync (TelegramSettings.Value.WebhookUrl);
        }

        [Fact]
        public async Task TelegramBotService__SendSearchResults__Uses_AnswerInlineQueryAsync__With_Items_As_InlineQueryResultArticles () {
            var booksSearchResult = SearchBooksResult.For (new Book[] { GetExampleBook() });
            var inlineQueryResults = booksSearchResult.Items.Select ((b) => TelegramBotService.CreateInlineQueryResultArticle(b, TelegramBotService.CreateInputTextMessageContent)).ToList ();
            var inlineQueryId = "SomeFakeId";

            await TelegramBotService.SendSearchResults (booksSearchResult, inlineQueryId);

            await TelegramBotClient.Received ().AnswerInlineQueryAsync (
                inlineQueryId: inlineQueryId,
                results: Arg.Is<IList<InlineQueryResultArticle>> (iqras => iqras.SequenceEqual(inlineQueryResults, new InlineQueryResultArticleComparer()))
            );
        }

        [Fact]
        public void TelegramBotService__CreateInputTextMessageContent__Creates_HTML_Content_When_Given_A_Book () {
            var book = GetExampleBook();
            var inputTextMessageContent = TelegramBotService.CreateInputTextMessageContent (book);

            inputTextMessageContent.MessageText.Should().Be ($"<a href=\"{book.ImageUrl}\" target=\"_black\">&#8203;</a><b>{book.Title}</b>\nBy <a href=\"https://www.goodreads.com/author/show/{book.Author.Id}\">{book.Author.Name}</a>\n\nRead more about this book on <a href=\"https://www.goodreads.com/book/show/{book.Id}\">Goodreads</a>.");
        }

        [Fact]
        public void TelegramBotService__CreateInlineQueryResultArticle__Returns_A_Valid_Article_Given_A_Book () {
            var book = GetExampleBook();
            var createInputMessageContent = Substitute.For<Func<Book, InputMessageContentBase>>();
            var inlineQueryResultArticle = TelegramBotService.CreateInlineQueryResultArticle(book, createInputMessageContent);

            inlineQueryResultArticle.Title.Should().Be(book.Title);
            inlineQueryResultArticle.Description.Should().Be(book.Author.Name);
            inlineQueryResultArticle.ThumbUrl.Should().Be(book.SmallImageUrl);
            createInputMessageContent.ReceivedCalls();
        }
    }
}