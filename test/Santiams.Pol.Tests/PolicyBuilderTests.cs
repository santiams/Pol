using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Moq;
using Pol.Notifications;
using Polly;
using Polly.Retry;
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
        [Fact]
        public void RetryPolicy_Success_CreatesAnAsyncRetryPolicy()
        {
            var sut = PolicyBuilder.RetryPolicy(1);
            sut.Should().NotBeNull();
            sut.Should().BeOfType<AsyncRetryPolicy<HttpResponseMessage>>();
        }

        [Fact]
        public async Task RetryPolicy_Success_CallsMediatorWithCorrectRetryNotificationOnRetry()
        {
            var mediator = Mock.Of<IMediator>();
            var context = new Context
            {
                ["MediatR"] = mediator
            };
            
            var policy = PolicyBuilder.RetryPolicy(1);
            
            await Assert.ThrowsAsync<TimeoutRejectedException>(()=>policy.ExecuteAsync(
                (_, _) => throw new TimeoutRejectedException("oops"), context, CancellationToken.None));

            Func<RetryNotification, bool> isMatch = tn => tn.Context == context &&
                                                            tn.RetryAttempt == 1 &&
                                                            tn.OfTotalRetries == 1 &&
                                                            tn.SleepDuration == null &&
                                                            tn.Result.Exception.GetType() == typeof(TimeoutRejectedException);

            Mock.Get(mediator).Verify(m=>m.Publish(
                It.Is<RetryNotification>(tn => isMatch(tn)), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task RetryPolicy_Success_DoesNotFailIfMediatorNotConfiguredInContext()
        {
            var context = Mock.Of<Context>();
            
            var policy = PolicyBuilder.RetryPolicy(1);
            
            await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
                policy.ExecuteAsync((_, _) => throw new TimeoutRejectedException("oops"), context, CancellationToken.None));
        }

        // WaitAndRetryPolicy
        [Fact]
        public void WaitAndRetryPolicy_Success_CreatesAnAsyncRetryPolicy()
        {
            var sut = PolicyBuilder.WaitAndRetryPolicy(1, _ =>TimeSpan.FromMilliseconds(1));
            sut.Should().NotBeNull();
            sut.Should().BeOfType<AsyncRetryPolicy<HttpResponseMessage>>();
        }

        [Fact]
        public async Task WaitAndRetryPolicy_Success_CallsMediatorWithCorrectRetryNotificationOnRetry()
        {
            var mediator = Mock.Of<IMediator>();
            var context = new Context
            {
                ["MediatR"] = mediator
            };
            
            var policy = PolicyBuilder.WaitAndRetryPolicy(1, _ =>TimeSpan.FromMilliseconds(1));
            
            await Assert.ThrowsAsync<TimeoutRejectedException>(()=>policy.ExecuteAsync(
                (_, _) => throw new TimeoutRejectedException("oops"), context, CancellationToken.None));

            Func<RetryNotification, bool> isMatch = tn => tn.Context == context &&
                                                          tn.RetryAttempt == 1 &&
                                                          tn.OfTotalRetries == 1 &&
                                                          tn.SleepDuration == TimeSpan.FromMilliseconds(1) &&
                                                          tn.Result.Exception.GetType() == typeof(TimeoutRejectedException);

            Mock.Get(mediator).Verify(m=>m.Publish(
                It.Is<RetryNotification>(tn => isMatch(tn)), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WaitAndRetryPolicy_Success_DoesNotFailIfMediatorNotConfiguredInContext()
        {
            var context = Mock.Of<Context>();
            
            var policy = PolicyBuilder.WaitAndRetryPolicy(1, _ => TimeSpan.FromMilliseconds(1));
            
            await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
                policy.ExecuteAsync((_, _) => throw new TimeoutRejectedException("oops"), context, CancellationToken.None));
        }
        
        // RetryForeverPolicy
        [Fact]
        public void RetryForeverPolicy_Success_CreatesAnAsyncRetryPolicy()
        {
            var sut = PolicyBuilder.RetryForeverPolicy();
            sut.Should().NotBeNull();
            sut.Should().BeOfType<AsyncRetryPolicy<HttpResponseMessage>>();
        }

        [Fact]
        public async Task RetryForeverPolicy_Success_CallsMediatorWithCorrectRetryNotificationOnRetry()
        {
            var mediator = Mock.Of<IMediator>();
            var context = new Context
            {
                ["MediatR"] = mediator
            };
            
            var policy = PolicyBuilder.RetryForeverPolicy();
            
            var iteration = -1;

            await policy.ExecuteAsync(
                (_, _) =>
                {
                    iteration++;
                    if(iteration == 0)
                        throw new TimeoutRejectedException("oops");
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }, context, CancellationToken.None);

            Func<RetryNotification, bool> isMatch = tn => tn.Context == context &&
                                                          tn.RetryAttempt == 1 &&
                                                          tn.OfTotalRetries == null &&
                                                          tn.SleepDuration == null &&
                                                          tn.Result.Exception.GetType() == typeof(TimeoutRejectedException);

            Mock.Get(mediator).Verify(m=>m.Publish(
                It.Is<RetryNotification>(tn => isMatch(tn)), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task RetryForeverPolicy_Success_DoesNotFailIfMediatorNotConfiguredInContext()
        {
            var context = Mock.Of<Context>();
            
            var policy = PolicyBuilder.RetryForeverPolicy();
            
            var iteration = -1;

            await policy.ExecuteAsync(
                (_, _) =>
                {
                    iteration++;
                    if(iteration == 0)
                        throw new TimeoutRejectedException("oops");
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }, context, CancellationToken.None);
        }
    }
}