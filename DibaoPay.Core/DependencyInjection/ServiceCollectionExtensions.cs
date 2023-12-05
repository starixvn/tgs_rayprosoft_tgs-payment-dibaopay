using System.Reflection;
using Tgs.Payment.DibaoPay;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDibaoPay(
       this IServiceCollection services,
       Action<DibaoPayOptions> configureOptions,
       Action<IHttpClientBuilder> ConfigureAiClient)
    {
        ConfigureAiClient?.Invoke(services.AddHttpClient<DibaoPayClient>());

        return services
            .AddOptions<DibaoPayOptions>()
            .Configure(configureOptions)
            .Services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<DibaoPayHasher>();
    }
}
