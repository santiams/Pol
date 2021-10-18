using System;
using System.Net.Http;
using MediatR;
using Polly;

namespace Pol.Notifications;

public class CircuitBreakerOpenNotification : INotification
{
    public Context Context { get; }
    public DelegateResult<HttpResponseMessage> Result { get; }
    public TimeSpan DurationOfBreak { get; }

    public CircuitBreakerOpenNotification(Context context, DelegateResult<HttpResponseMessage> result, TimeSpan durationOfBreak)
    {
        Context = context;
        Result = result;
        DurationOfBreak = durationOfBreak;
    }
}