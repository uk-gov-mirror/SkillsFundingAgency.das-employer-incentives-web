using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using SFA.DAS.Authorization.Context;
using SFA.DAS.Authorization.DependencyResolution.Microsoft;
using SFA.DAS.Configuration.AzureTableStorage;
using SFA.DAS.EmployerIncentives.Web.Authorisation;
using SFA.DAS.EmployerIncentives.Web.Filters;
using SFA.DAS.EmployerIncentives.Web.Infrastructure;
using SFA.DAS.EmployerIncentives.Web.Infrastructure.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SFA.DAS.EmployerIncentives.Web
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _environment = environment;
            var configBuilder = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .SetBasePath(Directory.GetCurrentDirectory())
#if DEBUG
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.development.json", true)
#endif
                .AddEnvironmentVariables();

            if (!configuration["EnvironmentName"].Equals("LOCAL", StringComparison.CurrentCultureIgnoreCase))
            {
                configBuilder.AddAzureTableStorage(options =>
                        {
                            options.ConfigurationKeys = configuration["ConfigNames"].Split(",");
                            options.StorageConnectionString = configuration["ConfigurationStorageConnectionString"];
                            options.EnvironmentName = configuration["EnvironmentName"];
                            options.PreFixConfigurationKeys = false;
                        }
                    );

                _configuration = configBuilder.Build();
            }
            else
            {
                _configuration = configuration;
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddOptions();
            services.Configure<WebConfigurationOptions>(_configuration.GetSection(WebConfigurationOptions.EmployerIncentivesWebConfiguration));
            services.Configure<EmployerIncentivesApiOptions>(_configuration.GetSection(EmployerIncentivesApiOptions.EmployerIncentivesApi));
            services.Configure<CosmosDbConfigurationOptions>(_configuration.GetSection(CosmosDbConfigurationOptions.CosmosDbConfiguration));
            services.Configure<IdentityServerOptions>(_configuration.GetSection(IdentityServerOptions.IdentityServerConfiguration));
            services.Configure<ExternalLinksConfiguration>(_configuration.GetSection(ExternalLinksConfiguration.EmployerIncentivesExternalLinksConfiguration));

            services.AddAuthorizationPolicies();
            services.AddAuthorization<DefaultAuthorizationContextProvider>();
            services.AddEmployerAuthentication(_configuration);

            services.Configure<IISServerOptions>(options => { options.AutomaticAuthentication = false; });

            services.AddMvc(
                    options =>
                    {
                        options.Filters.Add(new AuthorizeFilter(PolicyNames.IsAuthenticated));
                        options.Filters.Add(new AuthorizeFilter(PolicyNames.HasEmployerAccount));
                        options.Filters.Add(new GoogleAnalyticsFilterAttribute());
                        options.EnableEndpointRouting = false;
                        options.SuppressOutputFormatterBuffering = true;
                    })
                .AddControllersAsServices();

            services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = _configuration["EnvironmentName"] == "LOCAL" ? 5001 : 443;
            });

            services.AddApplicationInsightsTelemetry(_configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);

            if (_configuration["EnvironmentName"] == "LOCAL" || _configuration["EnvironmentName"] == "DEV")
            {
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = _configuration.GetValue<string>("EmployerIncentivesWeb:RedisCacheConnectionString");
                });
            }

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
            });

            services.AddApplicationInsightsTelemetry();
            services.AddAntiforgery(options => options.Cookie = new CookieBuilder() { Name = ".EmployerIncentives.AntiForgery", HttpOnly = false });

            services
                .AddHashingService()
                .AddEmployerIncentivesService()
                .AddDataEncryptionService()
                .AddVerificationService()
                .AddDataProtection(_configuration);

            /* if (!_environment.IsDevelopment())
            {
                services.AddHealthChecks()
                    .AddCheck<ReservationsApiHealthCheck>(
                        "Reservation Api",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: new[] { "ready" })
                    .AddCheck<CommitmentsApiHealthCheck>(
                        "Commitments Api",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: new[] { "ready" })
                    .AddCheck<ProviderRelationshipsApiHealthCheck>(
                        "ProviderRelationships Api",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: new[] { "ready" })
                    .AddCheck<AccountApiHealthCheck>(
                        "Accounts Api",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: new[] { "ready" });
            } */

            if (!_environment.IsDevelopment())
            {
                services.AddHealthChecks();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error/500");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseDasHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.Use(async (context, next) =>
            {
                if (context.Response.Headers.ContainsKey("X-Frame-Options"))
                {
                    context.Response.Headers.Remove("X-Frame-Options");
                }

                context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");

                await next();

                if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
                {
                    //Re-execute the request so the user gets the error page
                    var originalPath = context.Request.Path.Value;
                    context.Items["originalPath"] = originalPath;
                    context.Request.Path = "/error/404";
                    await next();
                }
            });
            app.UseAuthentication();

            if (!_environment.IsDevelopment())
            {
                app.UseHealthChecks();
            }

            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
