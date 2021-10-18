using System;
using System.Net.Http;
using MediatR;
using Polly;

namespace Pol.Notifications;

public class RetryNotification : INotification
{
    /// <summary>
    /// The notification that is fired when a Retry occurs.  Use <see cref="INotificationHandler{RetryNotification}"/> to capture and act upon it.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <param name="retryAttempt"></param>
    /// <param name="ofTotalRetries"></param>
    /// <param name="sleepDuration"></param>
    public RetryNotification(Context context, DelegateResult<HttpResponseMessage> result, int retryAttempt, int? ofTotalRetries = null, TimeSpan? sleepDuration = null)
    {
        Context = context;
        Result = result;
        RetryAttempt = retryAttempt;
        OfTotalRetries = ofTotalRetries;
        SleepDuration = sleepDuration;
    }
    
    public Context Context { get; }
    public DelegateResult<HttpResponseMessage> Result { get; }
    public int RetryAttempt { get; }
    public int? OfTotalRetries { get; }
    public TimeSpan? SleepDuration { get; }
}