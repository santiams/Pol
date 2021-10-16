using System.Net.Http;
using MediatR;
using Polly;

namespace Pol.Notifications;

public class RetryNotification : INotification
{
    public RetryNotification(Context context, DelegateResult<HttpResponseMessage> result, int retryAttempt, int ofTotalRetries)
    {
        Context = context;
        Result = result;
        RetryAttempt = retryAttempt;
        OfTotalRetries = ofTotalRetries;
    }
    
    public Context Context { get; }
    public DelegateResult<HttpResponseMessage> Result { get; }
    public int RetryAttempt { get; }
    public int OfTotalRetries { get; }
}