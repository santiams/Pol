using Polly;

namespace Pol.Notifications;

public class CircuitBreakerResetNotification
{
    public Context Context { get; }

    public CircuitBreakerResetNotification(Context context)
    {
        Context = context;
    }
}