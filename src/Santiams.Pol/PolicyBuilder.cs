﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Pol.Notifications;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Pol;

public static class PolicyBuilder
{
    public static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy(TimeSpan timeout)
    {
        Task OnTimeout(Context context, TimeSpan timespan, Task? cancelledTask, Exception? exception)
        {
            var retVal = cancelledTask ?? Task.CompletedTask;

            if(exception?.GetType() == typeof(TaskCanceledException))
                return retVal;
            
            var mediator = context.GetMediator();

            mediator?.Publish(new TimeoutNotification(context, timespan, exception));

            return retVal;
        }

        return Policy.TimeoutAsync<HttpResponseMessage>(timeout, OnTimeout);
    }

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

    public static IAsyncPolicy<HttpResponseMessage> WaitAndRetryPolicy(int retryCount, Func<int, TimeSpan> sleepDurationProvider)
    {
        void OnRetry(DelegateResult<HttpResponseMessage> result, TimeSpan sleepDuration, int retryAttempt, Context context)
        {
            if (result.Exception?.InnerException?.GetType() == typeof(TaskCanceledException))
            {
                return;
            }

            var mediator = context.GetMediator();
            mediator?.Publish(new RetryNotification(context, result, retryAttempt, retryCount, sleepDuration));
        }
        return StandardErrorHandlingPolicyBuilder()
            .WaitAndRetryAsync(retryCount, sleepDurationProvider, OnRetry);
    }

    public static IAsyncPolicy<HttpResponseMessage> RetryForeverPolicy()
    {
        void OnRetry(DelegateResult<HttpResponseMessage> result, int retryAttempt, Context context)
        {
            if (result.Exception?.InnerException?.GetType() == typeof(TaskCanceledException))
            {
                return;
            }

            var mediator = context.GetMediator();
            mediator?.Publish(new RetryNotification(context, result, retryAttempt));
        }

        return StandardErrorHandlingPolicyBuilder()
            .RetryForeverAsync(OnRetry);
    }

    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
    {
        void OnBreak(DelegateResult<HttpResponseMessage> result, TimeSpan durationOfBreak, Context context)
        {
            if (result.Exception?.InnerException?.GetType() == typeof(TaskCanceledException))
            {
                return;
            }
            var mediator = context.GetMediator();
            mediator?.Publish(new CircuitBreakerOpenNotification(context, result, durationOfBreak));

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