using System;
using System.Net.Http;
using FluentAssertions;
using MediatR;
using Moq;
using Pol;
using Polly;
using Xunit;

namespace Santiams.Pol.Tests
{
    public class HttpRequestMessageExtensionsTests
    {
        [Fact]
        public void AddPolicyExecutionContext_Fail_HttpRequestMessageIsNull()
        {
            var thrown = Assert.Throws<ArgumentNullException>(()=>
                ((HttpRequestMessage)null)!.AddPolicyExecutionContext(
                    "myTypedClientName", 
                    new HttpClient(), 
                    Mock.Of<IMediator>()));

            thrown.Message.Should().Be("Value cannot be null. (Parameter 'request')");
        }

        [Fact]
        public void AddPolicyExecutionContext_Fail_TypedClientNameIsNull()
        {
            var msg = new HttpRequestMessage();
            var thrown = Assert.Throws<ArgumentNullException>(()=> 
                msg.AddPolicyExecutionContext(
                    null!, 
                    new HttpClient(), 
                    Mock.Of<IMediator>()));

            thrown.Message.Should().Be("Value cannot be null. (Parameter 'typedClientName')");
        }

        [Fact]
        public void AddPolicyExecutionContext_Fail_HttpClientIsNull()
        {
            var msg = new HttpRequestMessage();
            var thrown = Assert.Throws<ArgumentNullException>(()=> 
                msg.AddPolicyExecutionContext(
                    "someTypedClientName", 
                    null!, 
                    Mock.Of<IMediator>()));

            thrown.Message.Should().Be("Value cannot be null. (Parameter 'httpClient')");
        }

        [Fact]
        public void AddPolicyExecutionContext_Fail_MediatorIsNull()
        {
            var msg = new HttpRequestMessage();
            var thrown = Assert.Throws<ArgumentNullException>(()=> 
                msg.AddPolicyExecutionContext(
                    "someTypedClientName", 
                    new HttpClient(), 
                    null!));

            thrown.Message.Should().Be("Value cannot be null. (Parameter 'mediator')");
        }

        [Fact]
        public void AddPolicyExecutionContext_Success_ContextIsAttachedToRequest()
        {
            var typedClientName = "myClientName";
            var httpClient = new HttpClient();
            var httpRequestMessage = new HttpRequestMessage();
            var mediator = Mock.Of<IMediator>();
            httpRequestMessage.AddPolicyExecutionContext(typedClientName, httpClient, mediator);
            var context = httpRequestMessage.GetPolicyExecutionContext();
            context.Should().NotBeNull();

            context.GetTypedClientName().Should().Be(typedClientName);
            context.GetMediator().Should().Be(mediator);
            context.GetRequest().Should().Be(httpRequestMessage);
            context.GetRequestUri().Should().BeNull();
        }

    }
}