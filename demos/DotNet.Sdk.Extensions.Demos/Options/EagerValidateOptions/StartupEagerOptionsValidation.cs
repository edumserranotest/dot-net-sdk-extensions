using DotNet.Sdk.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotNet.Sdk.Extensions.Demos.Options.EagerValidateOptions
{
    public class StartupEagerOptionsValidation
    {
        private readonly IConfiguration _configuration;

        public StartupEagerOptionsValidation(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISomeClassEager, SomeClassEager>();
            services
                .AddOptions<MyOptionsEager>()
                .Bind(_configuration.GetSection("MyOptionsSectionEagerValidation"))
                .ValidateDataAnnotations()
                .ValidateEagerly();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
                .UseWhen(x => env.IsDevelopment(), appBuilder => appBuilder.UseDeveloperExceptionPage())
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async context =>
                    {
                        await context.Response.WriteAsync("hi from asp.net core");
                    });
                    endpoints.MapGet("/my-awesome-endpoint", async context =>
                    {
                        // because eager validation is enabled you don't have to wait for the app to run
                        // this path of the code to realise that a required configuration value is missing
                        var someClass = context.RequestServices.GetRequiredService<ISomeClassEager>();
                        var message = someClass.GetMessage();
                        await context.Response.WriteAsync(message);
                    });
                });
        }
    }
}
