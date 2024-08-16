namespace MessagingAdapter.Services;

using System.Net.Http.Headers;
using IdentityModel.Client;
using MessagingAdapter.Services.HttpClients;

public static class HttpClientSetup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services, MessagingAdapterConfiguration config)
    {
        services.AddHttpClient<IAccessTokenClient, AccessTokenClient>();

        services.AddHttpClientWithBaseAddress<IEdtCoreClient, EdtCoreClient>(config.EdtClient.Url)
            .ConfigureHttpClient(c => c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.EdtClient.ApiKey));


        return services;
    }

    public static IHttpClientBuilder AddHttpClientWithBaseAddress<TClient, TImplementation>(this IServiceCollection services, string baseAddress)
        where TClient : class
        where TImplementation : class, TClient
        => services.AddHttpClient<TClient, TImplementation>(client => client.BaseAddress = new Uri(baseAddress.EnsureTrailingSlash()));

    public static IHttpClientBuilder WithBearerToken<T>(this IHttpClientBuilder builder, T credentials) where T : ClientCredentialsTokenRequest
    {
        builder.Services.AddSingleton(credentials)
            .AddTransient<BearerTokenHandler<T>>();

        builder.AddHttpMessageHandler<BearerTokenHandler<T>>();

        return builder;
    }
}
