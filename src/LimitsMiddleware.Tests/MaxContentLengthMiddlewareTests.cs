﻿namespace LimitsMiddleware
{
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Owin.Testing;
    using Owin;
    using Xunit;

    public class MaxRequestContentLengthMiddlewareTests
    {
        [Fact]
        public async Task When_max_contentLength_is_20_and_a_get_request_is_coming_it_should_be_served()
        {
            RequestBuilder requestBuilder = CreateRequest(20);

            HttpResponseMessage response = await requestBuilder.GetAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }


        [Fact]
        public async Task
            When_max_contentLength_is_20_and_a_get_request_with_contentLength_header_is_coming_then_the_header_should_be_ignored_and_the_request_served()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            AddContentLengthHeader(requestBuilder, 10);

            HttpResponseMessage response = await requestBuilder.GetAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_a_head_request_is_coming_it_should_be_served()
        {
            RequestBuilder requestBuilder = CreateRequest(20);

            HttpResponseMessage response = await requestBuilder.SendAsync("HEAD");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_a_post_with_contentLength_15_is_coming_it_should_be_served()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            requestBuilder.And(req => AddContent(req, 15));
            AddContentLengthHeader(requestBuilder, 15);

            HttpResponseMessage response = await requestBuilder.PostAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_a_post_with_contentLength_21_is_coming_it_should_be_rejected()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            requestBuilder.And(req => AddContent(req, 21));
            AddContentLengthHeader(requestBuilder, 21);

            HttpResponseMessage response = await requestBuilder.PostAsync();

            response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_a_put_with_contentLength_15_is_coming_it_should_be_served()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            requestBuilder.And(req => AddContent(req, 15));
            AddContentLengthHeader(requestBuilder, 15);

            HttpResponseMessage response = await requestBuilder.SendAsync("PUT");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_a_put_with_contentLength_21_is_coming_it_should_be_rejected_with_custom_reasonPhrase()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            requestBuilder.And(req => AddContent(req, 21));
            AddContentLengthHeader(requestBuilder, 21);

            HttpResponseMessage response = await requestBuilder.SendAsync("PUT");

            response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
            response.ReasonPhrase.Should().Be("custom phrase");
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_a_patch_with_contentLength_15_is_coming_it_should_be_served()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            requestBuilder.And(req => AddContent(req, 15));
            AddContentLengthHeader(requestBuilder, 15);

            HttpResponseMessage response = await requestBuilder.SendAsync("PATCH");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_a_patch_with_contentLength_21_is_coming_it_should_be_rejected()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            requestBuilder.And(req => AddContent(req, 21));
            AddContentLengthHeader(requestBuilder, 21);

            HttpResponseMessage response = await requestBuilder.SendAsync("PATCH");

            response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_the_contentLength_header_in_a_post_is_absent_it_should_be_rejected()
        {
            RequestBuilder requestBuilder = CreateRequest(20);

            HttpResponseMessage response = await requestBuilder.PostAsync();

            response.StatusCode.Should().Be(HttpStatusCode.LengthRequired);
        }

        [Fact]
        public async Task When_max_contentLength_is_20_and_the_contentLength_header_is_not_a_valid_number_it_should_be_rejected()
        {
            RequestBuilder requestBuilder = CreateRequest(20);
            AddContentLengthHeader(requestBuilder, "NotAValidNumber");

            HttpResponseMessage response = await requestBuilder.PostAsync();

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task When_max_contentLength_is_2_and_the_request_is_chunked_and_longer_it_should_be_rejected()
        {
            RequestBuilder request = CreateRequest(2);

            request.And(req =>
            {
                req.Content = new StringContent("4\r\nWiki\r\n5\r\npedia\r\ne\r\nin\r\n\r\nchunks.\r\n0\r\n\r\n");
                req.Headers.TransferEncodingChunked = true;
            });

            HttpResponseMessage response = await request.PostAsync();

            response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }


        private static RequestBuilder CreateRequest(int maxContentLength)
        {
            TestServer server = TestServer.Create(builder => builder
                .UseOwin().MaxRequestContentLength(new MaxRequestContentLengthOptions(maxContentLength)
                {
                    LimitReachedReasonPhrase = code => "custom phrase"
                })
                .UseAppBuilder(builder)
                .Use(async (context, _) =>
                {
                    await new StreamReader(context.Request.Body).ReadToEndAsync();
                    await new StreamWriter(context.Response.Body).WriteAsync("Lorem ipsum");
                    context.Response.StatusCode = 200;
                    context.Response.ReasonPhrase = "OK";
                }));
            RequestBuilder request = server.CreateRequest("http://example.com");

            return request;
        }

        private static void AddContentLengthHeader(RequestBuilder request, int contentLengthValue)
        {
            AddContentLengthHeader(request, contentLengthValue.ToString(CultureInfo.InvariantCulture));
        }

        private static void AddContentLengthHeader(RequestBuilder request, string contentLengthValue)
        {
            request.AddHeader("Content-Length", contentLengthValue);
        }

        private static void AddContent(HttpRequestMessage request, int contentLength)
        {
            request.Content = new StringContent("".PadLeft(contentLength, 'a'));
        }
    }
}