using System;
using System.Net.Http;
using MediatR;
using Polly;

namespace Pol;

/// <summary>
/// Provides the <see cref="AddPolicyExecutionContext"/> method for easy configuration of a Polly <see cref="Context"/>
/// </summary>
public static class HttpRequestMessageExtensions
{
    /// <summary>
    /// An extension method on <see cref="HttpResponseMessage"/> that allows you to attach the <see cref="HttpRequestMessage"/> and <see cref="HttpClient"/> for the request.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/> to attach the <see cref="Context"/> to.</param>
    /// <param name="typedClientName">The name of the Typed Client</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> that will be used to send the <see cref="HttpRequestMessage"/></param>
    /// <param name="mediator">An instance of <see cref="IMediator"/> for publishing notifications related to the execution of the request</param>
    public static void AddPolicyExecutionContext(this HttpRequestMessage request, string typedClientName, HttpClient httpClient, IMediator mediator)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        if (typedClientName == null)
        {
            throw new ArgumentNullException(nameof(typedClientName));
        }
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }
        if (mediator == null)
        {
            throw new ArgumentNullException(nameof(mediator));
        }
        
        var context = new Context()
            .WithClientRequest(typedClientName, httpClient, request)
            .WithMediator(mediator);
        request.SetPolicyExecutionContext(context);
    }
}