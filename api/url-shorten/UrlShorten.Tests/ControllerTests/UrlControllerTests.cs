﻿namespace UrlShorten.Tests
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UrlShorten.Contexts;
    using UrlShorten.Controllers;
    using UrlShorten.Models;
    using UrlShorten.Services;
    using Xunit;

    public class UrlControllerTests
    {
        private const string _hostname = "https://localhost:7139";
        private UrlController _urlController;

        public UrlControllerTests()
        {
            var options = new DbContextOptionsBuilder<UrlContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new UrlContext(options);

            var data = new List<ShortUrl>
            {
                new ShortUrl { Id = "test1", Url = "https://test.com" },
                new ShortUrl { Id = "test2", Url = "https://test2.com" },
                new ShortUrl { Id = "test3", Url = "https://test3.com" },
            };

            context.AddRange(data);
            context.SaveChanges();

            var mockUrlValidator = new Mock<IUrlValidator>();
            mockUrlValidator.Setup(x => x.ValidateUrl(It.IsAny<string>())).Returns("https//www.test.com");

            var mockIdFactory = new Mock<IIdFactory>();
            mockIdFactory.Setup(x => x.CreateId()).Returns("TestCode");

            var mockLogger = Mock.Of<ILogger<UrlController>>();

            var inMemoryConfig = new Dictionary<string, string> {
                {"Hostname", _hostname},
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            _urlController = new UrlController(mockLogger, mockUrlValidator.Object, mockIdFactory.Object, context, configuration);
        }

        [Fact]
        public async Task CreateShortUrl_Returns_ShortUrl_Response()
        {
            // Arrange
            var request = new ShortenRequest
            {
                Url = "https//www.test.com"
            };

            // Act
            var actual = await _urlController.CreateShortUrl(request);

            // Assert
            Assert.NotNull(actual?.Value?.ShortUrl);
            Assert.Equal(actual.Value.ShortUrl, $"{_hostname}/TestCode");
        }

        [Theory]
        [InlineData("test1")]
        [InlineData("test2")]
        [InlineData("test3")]
        public async Task GetByCode_Returns_Redirect_If_Found(string id)
        {
            // Act
            var actual = await _urlController.GetById(id);

            // Assert
            Assert.IsType<RedirectResult>(actual);
        }

        [Theory]
        [InlineData("test4")]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetByCode_Returns_Not_Found_If_Not_Found(string id)
        {
            // Act
            var actual = await _urlController.GetById(id);

            // Assert
            Assert.IsType<NotFoundObjectResult>(actual);
        }
    }
}
