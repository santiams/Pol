using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Moq;
using Pol.Notifications;
using Polly;
using Polly.Timeout;
using Xunit;

namespace Pol.Tests
{
    public class PolicyBuilderTests
    {
        // TimeoutPolicy

        [Fact]
        public void TimeoutPolicy_Success_CreatesAnAsyncTimeoutPolicy()
        {
            var ts = TimeSpan.FromMilliseconds(50);
            var result = PolicyBuilder.TimeoutPolicy(ts);
            result.Should().NotBeNull();
            result.Should().BeOfType<AsyncTimeoutPolicy<HttpResponseMessage>>();
        }

        [Fact]
        public async Task TimeoutPolicy_Success_CallsMediatorWithCorrectTimeoutNotificationOnTimeout()
        {
            var ts = TimeSpan.FromMilliseconds(50);
            var mediator = Mock.Of<IMediator>();
            var context = new Context
            {
                ["MediatR"] = mediator
            };
            
            var policy = PolicyBuilder.TimeoutPolicy(ts);
            
            var thrown = await Assert.ThrowsAsync<TimeoutRejectedException>(()=>policy.ExecuteAsync(async (ctx, token) =>
            {
                await Task.Delay((int)ts.TotalMilliseconds + 100, token);
                return new HttpResponseMessage();
            }, context, CancellationToken.None));

            Func<TimeoutNotification, bool> isMatch = tn => tn.Context == context &&
                                                            tn.Timeout == ts &&
                                                            tn!.Exception!.GetType() == typeof(TaskCanceledException);

            Mock.Get(mediator).Verify(m=>m.Publish(
                It.Is<TimeoutNotification>(tn => isMatch(tn)), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task TimeoutPolicy_Success_DoesNotFailIfMediatorNotConfiguredInContext()
        {
            var ts = TimeSpan.FromMilliseconds(50);
            var context = Mock.Of<Context>();
            
            var policy = PolicyBuilder.TimeoutPolicy(ts);
            
            var thrown = await Assert.ThrowsAsync<TimeoutRejectedException>(()=>policy.ExecuteAsync(async (ctx, token) =>
            {
                await Task.Delay((int)ts.TotalMilliseconds + 100, token);
                return new HttpResponseMessage();
            }, context, CancellationToken.None));
        }

        // RetryPolicy
        public async Task RetryPolicy_Success_CreatesAnAsyncRetryPolicy()
        {
            new TimeoutRejectedException();
        }
    }
}