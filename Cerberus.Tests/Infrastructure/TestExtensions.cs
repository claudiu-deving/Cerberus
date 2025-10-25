using System.Net.Http.Json;

namespace Cerberus.Tests.Infrastructure;

/// <summary>
/// Extension methods to simplify test code with automatic CancellationToken usage
/// </summary>
public static class TestExtensions
{
    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value)
    {
        return client.PostAsJsonAsync(requestUri, value, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> PutAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value)
    {
        return client.PutAsJsonAsync(requestUri, value, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> GetAsyncForTest(
        this HttpClient client,
        string requestUri)
    {
        return client.GetAsync(requestUri, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> DeleteAsyncForTest(
        this HttpClient client,
        string requestUri)
    {
        return client.DeleteAsync(requestUri, TestContext.Current.CancellationToken);
    }

    public static Task<T?> ReadFromJsonAsync<T>(
        this HttpContent content)
    {
        return content.ReadFromJsonAsync<T>(cancellationToken: TestContext.Current.CancellationToken);
    }
}
