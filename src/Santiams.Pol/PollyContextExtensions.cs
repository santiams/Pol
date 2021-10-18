using System;
using System.Net.Http;
using MediatR;
using Polly;

namespace Pol;

/// <summary>
/// Extends <see cref="Context"/> to add convenience methods for attaching services
/// </summary>
public static class PollyContextExtensions
{
    private static readonly string MediatRKey = "MediatR";
    private static readonly string HttpClientNameKey = "ServiceName";
    private static readonly string RequestUriKey = "RequestUri";
    private static readonly string RequestKey = "Request";

    /// <summary>
    /// Adds an <see cref="IMediator"/> to the <see cref="Context"/>
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <param name="mediator">The <see cref="IMediator"/></param>
    /// <returns>The original <see cref="Context"/></returns>>
    public static Context WithMediator(this Context context, IMediator mediator)
    {
        context[MediatRKey] = mediator;
        return context;
    }

    /// <summary>
    /// Returns the attached <see cref="IMediator"/> or null if none has been added
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <returns>The previously stored <see cref="IMediator"/></returns>
    public static IMediator? GetMediator(this Context context)
    {
        if (context.TryGetValue(MediatRKey, out var mediator))
        {
            return mediator as IMediator;
        }
        return null;
    }
    
    /// <summary>
    /// Adds the HttpClient name to the <see cref="Context"/>.  This is useful when logging events raised by Polly
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <param name="httpClientName">The HttpClient name</param>
    /// <returns>The original <see cref="Context"/></returns>
    private static Context WithHttpClientName(this Context context, string httpClientName)
    {
        context[HttpClientNameKey] = httpClientName;
        return context;
    }
        
    /// <summary>
    /// Returns the HttpClient name
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <returns><see cref="string"/></returns>
    public static string? GetHttpClientName(this Context context)
    {
        if (context.TryGetValue(HttpClientNameKey, out var serviceName))
        {
            return serviceName as string;
        }
        return null;
    }
    
    /// <summary>
    /// Adds the request uri to the <see cref="Context"/>.  This is useful when logging events raised by Polly
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <param name="requestUri">The request Uri</param>
    /// <returns>The original <see cref="Context"/></returns>
    private static Context WithRequestUri(this Context context, Uri requestUri)
    {
        context[RequestUriKey] = requestUri;
        return context;
    }

    /// <summary>
    /// Gets the previously stored request uri
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <returns>The previously stored request Uri or null</returns>
    public static Uri? GetRequestUri(this Context context)
    {
        if (context.TryGetValue(RequestUriKey, out var requestUri))
        {
            return requestUri as Uri;
        }
        return null;
    }

    /// <summary>
    /// The <see cref="HttpRequestMessage"/> that is associated with the <see cref="Context"/>
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <returns></returns>
    public static HttpRequestMessage? GetRequest(this Context context)
    {
        if (context.TryGetValue(RequestKey, out var request))
        {
            return request as HttpRequestMessage;
        }
        return null;
    }

    
    /// <summary>
    /// Sets the HttpClientName and RequestUri on the provided <see cref="Context"/>
    /// </summary>
    /// <param name="context">The <see cref="Context"/></param>
    /// <param name="client">The <see cref="HttpClient"/></param>
    /// <param name="request">The <see cref="HttpRequestMessage"/></param>
    /// <returns></returns>
    public static Context WithClientRequest(this Context context, HttpClient client, HttpRequestMessage request)
    {
        context[RequestKey] = request;

        WithHttpClientName(context, client.GetType().Name);

        if(client.BaseAddress == null && request.RequestUri == null)
            return context;

        if (client.BaseAddress == null)
        {
            WithRequestUri(context, request.RequestUri);
            return context;
        }

        if (request.RequestUri == null)
        {
            WithRequestUri(context, client.BaseAddress);
            return context;
        }
        WithRequestUri(context, new Uri(client.BaseAddress, request.RequestUri));
        return context;
    }
}