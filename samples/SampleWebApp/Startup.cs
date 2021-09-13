using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Handlers;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Services;
using Microsoft.Extensions.Caching.Memory;

namespace SampleWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Maskinporten requires a memory cache implementation
            services.AddSingleton<IMemoryCache, MemoryCache>();

            // We also need at least one HTTP client in order to fetch tokens
            services.AddHttpClient();
            
            // We only need a single Maskinporten-service. This can be used directly if low level access is required.
            services.AddSingleton<IMaskinportenService, MaskinportenService>();

            // Add some configurations that will be injected for the respective client definitions
            services.Configure<MaskinportenSettings<SettingsJwkClientDefinition>>(Configuration.GetSection("MaskinportenSettingsForJwkSettings"));
            services.Configure<MaskinportenSettings<Pkcs12ClientDefinition>>(Configuration.GetSection("MyMaskinportenSettingsForCertFile"));
            services.Configure<MaskinportenSettings<CertificateStoreClientDefinition>>(Configuration.GetSection("MyMaskinportenSettingsForThumbprint"));
            // Add some custom configuration which will be injected for the supplied definition
            services.Configure<MaskinportenSettings<Pkcs12ClientDefinition<IMyCustomMaskinportenSettings>>>(Configuration.GetSection("MyCustomMaskinportenSettingsForCertFile"));

            // Add some client definitions
            services.AddSingleton<SettingsJwkClientDefinition>();   
            services.AddSingleton<Pkcs12ClientDefinition>();
            services.AddSingleton<CertificateStoreClientDefinition>();
            
            // Add a custom client definition for using a custom configuration which will be injected
            services.AddSingleton<Pkcs12ClientDefinition<IMyCustomMaskinportenSettings>>();

            // Add another client definition for exisiting implementation but wuth overridden configuration
            services.AddSingleton(_ => new CertificateStoreClientDefinition<IMyCustomMaskinportenSettings>(new MaskinportenSettings()
            {
                Environment = "prod",
                ClientId = "some-id",
                Scope = "somescope",
                CertificateStoreThumbprint = "somethumbprinthere"
            }));

            // Add a client definition with fully custom client definition implementation 
            services.AddSingleton<MyCustomClientDefinition>();

            // Add handlers for the various definitions
            services.AddTransient<MaskinportenTokenHandler<SettingsJwkClientDefinition>>();
            services.AddTransient<MaskinportenTokenHandler<Pkcs12ClientDefinition>>();
            services.AddTransient<MaskinportenTokenHandler<CertificateStoreClientDefinition>>();
            services.AddTransient<MaskinportenTokenHandler<Pkcs12ClientDefinition<IMyCustomMaskinportenSettings>>>();
            services.AddTransient<MaskinportenTokenHandler<MyCustomClientDefinition>>();
            
            // Add some named clients
            services.AddHttpClient("client1").AddHttpMessageHandler<MaskinportenTokenHandler<SettingsJwkClientDefinition>>();
            services.AddHttpClient("client2").AddHttpMessageHandler<MaskinportenTokenHandler<Pkcs12ClientDefinition>>();
            services.AddHttpClient("client3").AddHttpMessageHandler<MaskinportenTokenHandler<CertificateStoreClientDefinition>>();
            services.AddHttpClient("client4").AddHttpMessageHandler<MaskinportenTokenHandler<Pkcs12ClientDefinition<IMyCustomMaskinportenSettings>>>();
            services.AddHttpClient("client5").AddHttpMessageHandler<MaskinportenTokenHandler<CertificateStoreClientDefinition<IMyCustomMaskinportenSettings>>>();
            services.AddHttpClient("client6").AddHttpMessageHandler<MaskinportenTokenHandler<MyCustomClientDefinition>>();

            // Add a typed client
            services.AddHttpClient<MyHttpClient>().AddHttpMessageHandler<MaskinportenTokenHandler<CertificateStoreClientDefinition<IMyCustomMaskinportenSettings>>>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
