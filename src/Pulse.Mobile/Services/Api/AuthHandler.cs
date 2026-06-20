using System.Net;
using System.Net.Http.Headers;
using Pulse.Services.Auth;

namespace Pulse.Services.Api;

/// <summary>
/// Attaches the Supabase bearer token to every outgoing request and, on a 401, refreshes the token
/// once and retries. Centralises what was previously copy-pasted across the client's JSON and
/// multipart send paths.
/// </summary>
public class AuthHandler(IAuthService auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AuthorizeAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // A 401 means the token went stale server-side; force one refresh and retry. A request can't
        // be re-sent as-is, so clone it (buffering the content) before the second attempt.
        response.Dispose();
        using var retry = await CloneAsync(request, cancellationToken);
        await AuthorizeAsync(retry, cancellationToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task AuthorizeAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await auth.GetAccessTokenAsync(cancellationToken));

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content is not null)
        {
            // Buffer the original content so it can be replayed on the retry.
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        foreach (var header in request.Headers)
        {
            // The Authorization header is re-set with a fresh token by AuthorizeAsync.
            if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        clone.Version = request.Version;
        return clone;
    }
}
