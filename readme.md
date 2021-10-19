# Pol

[![.NET](https://github.com/santiams/Pol/actions/workflows/dotnet.yml/badge.svg)](https://github.com/santiams/Pol/actions/workflows/dotnet.yml)

Pol is so called because it's [Polly](https://github.com/App-vNext/Polly) with bits missing.

It's an opinionated take on Polly in that it deals with only a subset of Polly's excellent resilience strategies - specifically it's aimed at resilience for named `HttpClient`s configured via Microsofts `HttpClientFactory`.

What's unique is that it offers a simple and flexible way to manage what happens when any configured policies kick in.

## Getting Started
The package is availale via the official nuget feed: https://www.nuget.org/packages/Pol/

With Pacakge Manager
```
Install-Package Pol
```

With .NET CLI
```
dotnet add package Pol
```

## Configure MediatR
Pol depends on [MediatR](https://github.com/jbogard/MediatR) to tell you when your policies are executing.
We'll go into how to set up Notification Handlers in a bit but, for now, you can add this to the `ConfigureServices` method in `Startup.cs`:

```cshparp
services.AddMediatR(typeof(Startup))
```

## Configuring HttpClient calls
Currently only a subset of resilience strategies are included and there is no inbuilt support for PolicyRegistry

* TimeoutAsync
* RetryAsync
* WaitAndRetryAsync
* RetryForeverAsync
* CircuitBreakerAsync

These are available via the extension methods in Pol's `PolicyBuilder` class.

`HttpClient` configuration is done in the normal way:

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Pol;
...
public static IServiceCollection ConfigureMyHttpClient(this IServiceCollection services)
{
    services
        .AddHttpClient<IMyHttpClient, MyHttpClient>((sp, client) => {
            // HttpClient configuration goes here
        })
        .AddPolicyHandler(PolicyBuilder.RetryPolicy(3))                                     // Configure policy to retry up to 3 times
        .AddPolicyHandler(PolicyBuilder.CircuitBreaker(10, TimeSpan.FromSeconds(3)))        // Break after 10 failures, stay open for 3 seconds
        .AddPolicyHandler(PolicyBuilder.TimeoutPolicy(TimeSpan.FromMilliseconds(300)));     // Timeout after 300ms

    return services;
}
```

## Updating `HttpClient`
To take full advantage of Pol, you'll want to update your `HttpClient` code to pass some useful information into Polly's `Context`.
Pol contains some extension methods to make getting relevant information in and out of the `Context` a bit easier.

```csharp
public class MyHttpClient : IMyHttpClient
{
    private IMediator _mediator;
    private HttpClient _httpClient;
    private ILogger<MyHttpClient> _logger;

    public MyHttpClient(HttpClient httpClient, IMediator mediator, ILogger<MyHttpClient> logger)  // Pass IMediator in
    {
        _httpClient = httpClient;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task CallService()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "some-path/");
            request.AddPolicyExecutionContext(_httpClient, _mediator);  // Creates a new Pol Context and attaches useful stuff
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch(Exception e)
        {
            _logger.BadStuffHappened(response, e);
        }
    }
}
```

## Listening for MediatR Notifications
With that all configured, you can go ahead and use your service normally.  But what you really _really_ want to do is find out when bad stuff is happening. 
Sure, we have log messages being written out in the exception handler in `MyHttpClient` but this _isn't_ going to tell you about errors that are being handled by Polly.

This is where (hopefully) all this MediatR shennanigans starts to become clearer.

Whenever a Polly policy is triggered, an `INotification<T>` will be sent to `IMediator`.  `T` will be one of:
* `TimeoutNotification`
* `RetryNotification`
* `CircuitBreakerOpenNotification`
* `CircuitBreakerResetNotification`

Each notification type contains the Polly `Context` (which will give you access to the name of the `HttpClient` and the request uri) together with other, notification specific, properties.

You'd typically use this to write a log message, notice an exception in New Relic etc when something goes bad.  Of course, it's MediatR, so you can set up as many handlers as you want in response to each notification.
I would typically add a logging handler and New Relic handler for all events and a Slack message handler for circuit open events.

### Example: Handling a TimeoutNotification
Here's an example of how you might use the `TimeoutNotification` to write a log message:
```csharp
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Pol;
using Pol.Notifications;
using TargetServiceClient.Logging;

namespace TargetServiceClient.Mediator.Handlers.TimeoutHandlers
{

    public class LoggingTimeoutHandler : INotificationHandler<TimeoutNotification>
    {
        private readonly ILogger<LoggingTimeoutHandler> _logger;

        public LoggingTimeoutHandler(ILogger<LoggingTimeoutHandler> logger)
        {
            _logger = logger;
        }
        public Task Handle(TimeoutNotification notification, CancellationToken cancellationToken)
        {
            _logger.PollyTimeoutPolicyInvoked(
                notification.Context.GetHttpClientName(),   // This helper method is available on Context for all notification types
                notification.Timeout,                       // The timespan representing the timeout that triggered this notification
                notification.Context.GetRequestUri(),       // This helper method is available on Context for all notification types
                notification.Exception);                    // The exception that was caught

            return Unit.Task;                               // Standard return pattern for MediatR NotificationHandler
        }
    }
}
```

This is just a standard MediatR `INotificationHandler<T>` that uses the properties in the `TimeoutNotification` send a log message.
You don't have to register this type (assuming that it is in the same assembly as your `Startup.cs`) - MediatR will register it automagically.

And, of course, you're free to add as many `NotificationHandler`s as you wish for each notification type.  So you might log as above, then add a separate handler to log the exception to New Relic and (in the case of a Circuit Breaker when you really want immediate notification that a circuit is open), add yet another handler that sends a message to a Slack channel.
