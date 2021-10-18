using System;
using MediatR;
using Polly;

namespace Pol.Notifications;

public class TimeoutNotification : INotification
{
    /// <summary>
    /// The notification that is fired when a Timeout occurs.  Use <see cref="INotificationHandler{TimeoutNotification}"/> to capture and act upon it.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="timeout"></param>
    /// <param name="exception"></param>
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