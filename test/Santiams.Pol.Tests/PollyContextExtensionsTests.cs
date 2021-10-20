using System;
using System.Net.Http;
using FluentAssertions;
using MediatR;
using Moq;
using Polly;
using Xunit;

namespace Pol.Tests
{
    public class PollyContextExtensionsTests
    {
        [Fact]
        public void WithMediator_Fail_MediatorIsNull()
        {
            var context = new Context();
            var thrown = Assert.Throws<ArgumentNullException>(()=>context.WithMediator(null!));
            thrown.Message.Should().Be("Value cannot be null. (Parameter 'mediator')");
        }

        [Fact]
        public void WithMediator_ReturnsNullWhenNotSet()
        {
            var context = new Context();
            context.GetMediator().Should().BeNull();
        }
        
        [Fact]
        public void WithMediator_Success_MediatorStoredAgainstContext()
        {
            var context = new Context();
            var expected = Mock.Of<IMediator>();
            context.WithMediator(expected);

            context.GetMediator().Should().Be(expected);
        }

        [Fact]
        public void WithClientRequest_Fail_TypedClientNameIsNull()
        {
            var context = new Context();
            var thrown = Assert.Throws<ArgumentNullException>(()=>
                context.WithClientRequest(null!, new HttpClient(), new HttpRequestMessage()));
            thrown.Message.Should().Be("Value cannot be null. (Parameter 'typedClientName')");
        }

        [Fact]
        public void WithClientRequest_Fail_HttpClientIsNull()
        {
            var context = new Context();
            var thrown = Assert.Throws<ArgumentNullException>(()=>
                context.WithClientRequest("someTypedClientName", null!, new HttpRequestMessage()));
            thrown.Message.Should().Be("Value cannot be null. (Parameter 'client')");
        }

        [Fact]
        public void WithClientRequest_Fail_RequestIsNull()
        {
            var context = new Context();
            var thrown = Assert.Throws<ArgumentNullException>(()=>
                context.WithClientRequest("someTypedClientName", new HttpClient(), null!));
            thrown.Message.Should().Be("Value cannot be null. (Parameter 'request')");
        }

        [Fact]
        public void WithClientRequest_Success_RequestUriIsNullWhenNeitherClientBaseAddressNorRequestUriSet()
        {
            var context = new Context();
            context.WithClientRequest("someTypedClient", new HttpClient(), new HttpRequestMessage());
            context.GetRequestUri().Should().BeNull();
        }

        [Fact]
        public void WithClientRequest_Success_RequestUriIsClientBaseAddressWhenOnlyClientBaseAddressSet()
        {
            var context = new Context();
            var expected = new Uri("https://some-uri/");
            context.WithClientRequest("someTypedClient", new HttpClient{BaseAddress = expected}, new HttpRequestMessage());
            context.GetRequestUri().Should().Be(expected);
        }

        [Fact]
        public void WithClientRequest_Success_RequestUriIsRequestWhenOnlyRequestUriSet()
        {
            var context = new Context();
            var expected = "/some/path";
            context.WithClientRequest("someTypedClient", new HttpClient(), new HttpRequestMessage(HttpMethod.Get, expected));
            context.GetRequestUri().Should().Be(expected);
        }

        [Fact]
        public void WithClientRequest_Success_RequestUriClientBaseUrlAndRequestUriWhenBothSet()
        {
            var context = new Context();
            var baseAddress = new Uri("https://some-uri/");
            var requestUri = "/some/path";
            context.WithClientRequest("someTypedClient", new HttpClient{BaseAddress = baseAddress}, new HttpRequestMessage(HttpMethod.Get, requestUri));
            context.GetRequestUri().Should().Be(new Uri("https://some-uri/some/path"));
        }
    }
}