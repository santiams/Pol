using System.Net.Http;
using MediatR;
using Polly;

namespace Pol;

public static class HttpRequestMessageExtensions
{
    public static void AddPolicyExecutionContext(this HttpRequestMessage request, HttpClient httpClient, IMediator mediator)
    {
        var context = new Context()
            .WithClientRequest(httpClient, request)
            .WithMediator(mediator);
        request.SetPolicyExecutionContext(context);
    }
}