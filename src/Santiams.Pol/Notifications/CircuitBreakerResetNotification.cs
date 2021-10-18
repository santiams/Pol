using MediatR;
using Polly;

namespace Pol.Notifications;

public class CircuitBreakerResetNotification  : INotification
{
    public Context Context { get; }

    public CircuitBreakerResetNotification(Context context)
    {
        Context = context;
    }
}