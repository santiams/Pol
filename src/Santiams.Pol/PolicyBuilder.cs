using System;
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
                return;
            
            var mediator = context.GetMediator();
                
            mediator?.Publish(new RetryNotification(context, result, retryAttempt, retryCount));
        }

        return StandardErrorHandlingPolicyBuilder()
            .RetryAsync(retryCount, OnRetry);
    }

    private static PolicyBuilder<HttpResponseMessage> StandardErrorHandlingPolicyBuilder()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>();
    }
}