using System;
using System.Net.Http;
using System.Threading.Tasks;
using MediatR;
using Pol.Notifications;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Timeout;

namespace Pol;

/// <summary>
/// Helper class which provides methods for creating supported Polly policies
/// </summary>
public static class PolicyBuilder
{
    /// <summary>
    /// Configures an <see cref="AsyncTimeoutPolicy{TResult}"/> policy which publishes a <see cref="TimeoutNotification"/> to <see cref="IMediator"/> on timeout.
    /// </summary>
    /// <param name="timeout">The amount of time to wait before timing out</param>
    /// <returns>The configured <see cref="AsyncTimeoutPolicy{TResult}"/> instance</returns>
    public static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy(TimeSpan timeout)
    {
        Task OnTimeout(Context context, TimeSpan timespan, Task? cancelledTask, Exception? exception)
        {
            var retVal = cancelledTask ?? Task.CompletedTask;
            
            var mediator = context.GetMediator();

            mediator?.Publish(new TimeoutNotification(context, timespan, exception));

            return retVal;
        }

        return Policy.TimeoutAsync<HttpResponseMessage>(timeout, OnTimeout);
    }

    /// <summary>
    /// Configures an <see cref="AsyncRetryPolicy{TResult}"/> which publishes a <see cref="RetryNotification"/> to <see cref="IMediator"/> on each retry.
    /// </summary>
    /// <param name="retryCount">The number of times to retry</param>
    /// <returns>The configured <see cref="AsyncRetryPolicy{TResult}"/> instance</returns>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy(int retryCount)
    {
        void OnRetry(DelegateResult<HttpResponseMessage> result, int retryAttempt, Context context)
        {
            if(result.Exception?.InnerException?.GetType() == typeof(TaskCanceledException))
            {
                return;
            }

            var mediator = context.GetMediator();
                
            mediator?.Publish(new RetryNotification(context, result, retryAttempt, retryCount));
        }

        return StandardErrorHandlingPolicyBuilder()
            .RetryAsync(retryCount, OnRetry);
    }

    /// <summary>
    /// Configures an <see cref="AsyncRetryPolicy{TResult}"/> which waits between retries, publishing a <see cref="RetryNotification"/> to <see cref="IMediator"/> on each retry.
    /// </summary>
    /// <param name="retryCount">The number of times to retry</param>
    /// <param name="sleepDurationProvider">A function that is called to determine the duration to wait before retrying for each failure</param>
    /// <returns>The configured <see cref="AsyncRetryPolicy{TResult}"/> instance</returns>
    public static IAsyncPolicy<HttpResponseMessage> WaitAndRetryPolicy(int retryCount, Func<int, TimeSpan> sleepDurationProvider)
    {
        void OnRetry(DelegateResult<HttpResponseMessage> result, TimeSpan sleepDuration, int retryAttempt, Context context)
        {
            if(result.Exception?.InnerException?.GetType() == typeof(TaskCanceledException))
            {
                return;
            }

            var mediator = context.GetMediator();
            mediator?.Publish(new RetryNotification(context, result, retryAttempt, retryCount, sleepDuration));
        }
        return StandardErrorHandlingPolicyBuilder()
            .WaitAndRetryAsync(retryCount, sleepDurationProvider, OnRetry);
    }

    /// <summary>
    /// Configures an <see cref="AsyncRetryPolicy{TResult}"/> which retries forever (until success) publishing a <see cref="RetryNotification"/> to <see cref="IMediator"/> on each retry.
    /// </summary>
    /// <returns>The configured <see cref="AsyncRetryPolicy{TResult}"/> instance</returns>
    public static IAsyncPolicy<HttpResponseMessage> RetryForeverPolicy()
    {
        void OnRetry(DelegateResult<HttpResponseMessage> result, int retryAttempt, Context context)
        {
            if(result.Exception?.InnerException?.GetType() == typeof(TaskCanceledException))
            {
                return;
            }

            var mediator = context.GetMediator();
            mediator?.Publish(new RetryNotification(context, result, retryAttempt));
        }

        return StandardErrorHandlingPolicyBuilder()
            .RetryForeverAsync(OnRetry);
    }
    
    /// <summary>
    /// Configures a <see cref="AsyncCircuitBreakerPolicy{TResult}"/> publishing a <see cref="CircuitBreakerOpenNotification"/> when the circuit opens and a <see cref="CircuitBreakerResetNotification"/> on reset.
    /// </summary>
    /// <param name="handledEventsAllowedBeforeBreaking">The number of faults before the circuit breakers</param>
    /// <param name="durationOfBreak">How long the circuit should stay open for after breaking</param>
    /// <returns></returns>
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
    {
        void OnBreak(DelegateResult<HttpResponseMessage> result, TimeSpan duration, Context context)
        {
            if(result.Exception?.InnerException?.GetType() == typeof(TaskCanceledException))
            {
                return;
            }

            var mediator = context.GetMediator();
            mediator?.Publish(new CircuitBreakerOpenNotification(context, result, duration));
        }

        void OnReset(Context context)
        {
            var mediator = context.GetMediator();
            mediator?.Publish(new CircuitBreakerResetNotification(context));
        }

        return StandardErrorHandlingPolicyBuilder()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking, durationOfBreak, OnBreak, OnReset);
    }

    private static PolicyBuilder<HttpResponseMessage> StandardErrorHandlingPolicyBuilder()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>();
    }
}