using System;
using MediatR;
using Polly;

namespace Pol.Notifications;

public class TimeoutNotification : INotification
{
    public TimeoutNotification(Context context, TimeSpan timeout, Exception? exception)
    {
        Timeout = timeout;
        Context = context;
        Exception = exception;
    }

    public Context Context { get; }
    public TimeSpan Timeout { get; }
    public Exception? Exception { get; }
}