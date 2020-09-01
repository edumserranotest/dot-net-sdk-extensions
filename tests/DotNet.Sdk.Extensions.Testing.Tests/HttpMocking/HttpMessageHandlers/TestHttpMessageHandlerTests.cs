﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Sdk.Extensions.Testing.HttpMocking.HttpMessageHandlers;
using DotNet.Sdk.Extensions.Testing.HttpMocking.HttpMessageHandlers.ResponseMocking;
using Shouldly;
using Xunit;

namespace DotNet.Sdk.Extensions.Testing.Tests.HttpMocking.HttpMessageHandlers
{
    public class TestHttpMessageHandlerTests
    {
        /// <summary>
        /// Validates the arguments for the <seealso cref="TestHttpMessageHandler.MockHttpResponse(HttpResponseMessageMock)"/> method.
        /// </summary>
        [Fact]
        public void ValidateArguments1()
        {
            var handler = new TestHttpMessageHandler();
            var exception = Should.Throw<ArgumentNullException>(() => handler.MockHttpResponse((HttpResponseMessageMock)null!));
            exception.Message.ShouldBe("Value cannot be null. (Parameter 'httpResponseMock')");
        }

        /// <summary>
        /// Validates the arguments for the <seealso cref="TestHttpMessageHandler.MockHttpResponse(Action{HttpResponseMessageMockBuilder})"/> method.
        /// </summary>
        [Fact]
        public void ValidateArguments2()
        {
            var handler = new TestHttpMessageHandler();
            var exception = Should.Throw<ArgumentNullException>(() => handler.MockHttpResponse((Action<HttpResponseMessageMockBuilder>)null!));
            exception.Message.ShouldBe("Value cannot be null. (Parameter 'configure')");
        }

        /// <summary>
        /// Tests that the <seealso cref="TestHttpMessageHandler"/> throws an exception if it gets executed
        /// but no mocks were defined.
        /// </summary>
        [Fact]
        public async Task NoMockDefined()
        {
            var handler = new TestHttpMessageHandler();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
            var httpMessageInvoker = new HttpMessageInvoker(handler);
            var exception = await Should.ThrowAsync<InvalidOperationException>(httpMessageInvoker.SendAsync(request, CancellationToken.None));
            exception.Message.ShouldBe("No response mock defined for GET to https://test.com/.");
        }

        /// <summary>
        /// Tests that the <seealso cref="TestHttpMessageHandler"/> throws an exception if it gets executed
        /// but no mocks are executed because none match the HttpRequestMessage.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task NoMockMatches()
        {
            var httpMockResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var handler = new TestHttpMessageHandler();
            handler.MockHttpResponse(builder =>
            {
                builder
                    .Where(httpRequestMessage => httpRequestMessage.RequestUri.Host.Equals("microsoft"))
                    .RespondWith(httpMockResponseMessage);
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
            var httpMessageInvoker = new HttpMessageInvoker(handler);
            var exception = await Should.ThrowAsync<InvalidOperationException>(httpMessageInvoker.SendAsync(request, CancellationToken.None));
            exception.Message.ShouldBe("No response mock defined for GET to https://test.com/.");
        }

        /// <summary>
        /// Tests that the <seealso cref="TestHttpMessageHandler"/> returns the mocked HttpResponseMessage.
        /// In this test no predicate is defined which means the default "always true" predicate takes effect
        /// and the mock is always returned. 
        /// Using <seealso cref="TestHttpMessageHandler.MockHttpResponse(HttpResponseMessageMock)"/>
        /// </summary>
        [Fact]
        public async Task DefaultPredicate1()
        {
            var httpMockResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var builder = new HttpResponseMessageMockBuilder();
            var httpResponseMessageMock = builder
                .RespondWith(httpMockResponseMessage)
                .Build();
            var handler = new TestHttpMessageHandler();
            handler.MockHttpResponse(httpResponseMessageMock);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
            var httpMessageInvoker = new HttpMessageInvoker(handler);
            var httpResponseMessage = await httpMessageInvoker.SendAsync(request, CancellationToken.None);

            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
            httpResponseMessage.Content.ShouldBeNull();
            httpResponseMessage.Headers.ShouldBeEmpty();
        }

        /// <summary>
        /// Tests that the <seealso cref="TestHttpMessageHandler"/> returns the mocked HttpResponseMessage.
        /// In this test no predicate is defined which means the default "always true" predicate takes effect
        /// and the mock is always returned.
        /// Using <seealso cref="TestHttpMessageHandler.MockHttpResponse(Action{HttpResponseMessageMockBuilder})"/>
        /// </summary>
        [Fact]
        public async Task DefaultPredicate2()
        {
            var httpMockResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var handler = new TestHttpMessageHandler();
            handler.MockHttpResponse(builder => builder.RespondWith(httpMockResponseMessage));

            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
            var httpMessageInvoker = new HttpMessageInvoker(handler);
            var httpResponseMessage = await httpMessageInvoker.SendAsync(request, CancellationToken.None);

            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
            httpResponseMessage.Content.ShouldBeNull();
            httpResponseMessage.Headers.ShouldBeEmpty();
        }

        /// <summary>
        /// Tests that the <seealso cref="TestHttpMessageHandler"/> returns the mocked HttpResponseMessage
        /// for the FIRST match.
        /// </summary>
        [Fact]
        public async Task FirstMatchWins()
        {
            var handler = new TestHttpMessageHandler();
            handler.MockHttpResponse(builder =>
            {
                builder
                    .Where(httpRequestMessage=>httpRequestMessage.RequestUri.Host.Equals("test.com"))
                    .RespondWith(new HttpResponseMessage(HttpStatusCode.BadRequest));
            });
            handler.MockHttpResponse(builder =>
            {
                builder
                    .Where(httpRequestMessage => httpRequestMessage.RequestUri.Host.Equals("test.com"))
                    .RespondWith(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
            var httpMessageInvoker = new HttpMessageInvoker(handler);
            var httpResponseMessage = await httpMessageInvoker.SendAsync(request, CancellationToken.None);

            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the <seealso cref="TestHttpMessageHandler"/> returns the mocked HttpResponseMessage
        /// for the appropriate predicate match.
        /// </summary>
        [Fact]
        public async Task MultipleMocks()
        {
            var handler = new TestHttpMessageHandler();
            handler
                .MockHttpResponse(builder =>
                {
                    builder
                        .Where(httpRequestMessage => httpRequestMessage.RequestUri.Host.Equals("google.com"))
                        .RespondWith(new HttpResponseMessage(HttpStatusCode.BadRequest));
                })
                .MockHttpResponse(builder =>
                {
                    builder
                        .Where(httpRequestMessage => httpRequestMessage.RequestUri.Host.Equals("microsoft.com"))
                        .RespondWith(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                });

            var httpMessageInvoker = new HttpMessageInvoker(handler);

            var request1 = new HttpRequestMessage(HttpMethod.Get, "https://google.com");
            var httpResponseMessage1 = await httpMessageInvoker.SendAsync(request1, CancellationToken.None);
            httpResponseMessage1.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            var request2 = new HttpRequestMessage(HttpMethod.Get, "https://microsoft.com");
            var httpResponseMessage2 = await httpMessageInvoker.SendAsync(request2, CancellationToken.None);
            httpResponseMessage2.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }
    }
}
