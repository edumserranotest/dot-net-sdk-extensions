using Microsoft.Extensions.DependencyInjection;
#if !NETCOREAPP3_1 && !NET5_0
using Microsoft.Extensions.Hosting;
#endif

namespace DotNet.Sdk.Extensions.Testing.Tests.HostedServices.Auxiliary
{
    internal static class ConfigureHostExtensions
    {
        /// <summary>
        /// Avoid exceptions logged to the output when running tests in linux OS because the IHost is stopped when the background service is stopped.
        /// <see href="https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method only has effect in target frameworks equal to or greater than NET6_0.
        /// </para>
        /// <para>
        /// This should not have any impact on the tests. Its purpose is to avoid noise on the output logs when running dotnet test.
        /// </para>
        /// The exception log being disabled is as follows:
        /// <para>
        /// crit: Microsoft.Extensions.Hosting.Internal.Host[10]
        /// The HostOptions.BackgroundServiceExceptionBehavior is configured to StopHost.
        /// A BackgroundService has thrown an unhandled exception, and the IHost instance is stopping.
        /// To avoid this behavior, configure this to Ignore; however the BackgroundService will not be restarted.
        /// System.Threading.Tasks.TaskCanceledException: A task was canceled.
        /// </para>
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        public static IServiceCollection DisableHostStoppingLogs(this IServiceCollection services)
        {
#if NETCOREAPP3_1 || NET5_0
            // do nothing
            return services;
#else
            return services.Configure<HostOptions>(hostOptions =>
            {
                hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });
#endif
        }
    }
}
