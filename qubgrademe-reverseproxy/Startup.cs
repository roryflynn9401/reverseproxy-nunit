using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(qubgrademe_reverseproxy.Startup))]

namespace qubgrademe_reverseproxy
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<MemoryCache>();
            builder.Services.AddHttpClient();
        }
    }
}
