using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using MailKitSimplified.Receiver.Extensions;
using WebApiExample.Helpers;

namespace WebApiExample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var services = builder.Services;
            var configuration = builder.Configuration;

            // Add security
            services.AddHttpsRedirection(options =>
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect);

            // Add API versioning including 'api-supported-versions' response header
            var apiVersionDefault = new ApiVersion(1, 0);
            services
                .AddApiVersioning(options =>
                {
                    options.ReportApiVersions = true;
                    options.DefaultApiVersion = apiVersionDefault;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ApiVersionReader = ApiVersionReader.Combine(
                        new MediaTypeApiVersionReader("ver"),
                        new UrlSegmentApiVersionReader());
                })
                .AddVersionedApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV"; // "'v'major[.minor][-status]"
                    options.SubstituteApiVersionInUrl = true;
                });

            // Add swagger
            var apiTitle = "MailKitSimplified API";
            var apiDescription = "##### An ASP.NET Core Web API for MailKitSimplified IMAP queries";
            var minorVersion = apiVersionDefault.MinorVersion > 0 ? $".{apiVersionDefault.MinorVersion}" : string.Empty;
            var apiVersion = $"v{apiVersionDefault.MajorVersion}{minorVersion}";
            services.AddEndpointsApiExplorer()
                .AddSwaggerGen(options =>
                {
                    options.SwaggerDoc(apiVersion, new OpenApiInfo
                    {
                        Title = apiTitle,
                        Description = apiDescription,
                        Version = apiVersion
                    });
                    // Set the comments path for the Swagger JSON and UI
                    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
                    options.IncludeXmlComments(xmlPath);
                });

            // Add general services
            services.AddLogging();
            services.AddMemoryCache();

            // Add custom services
            services.AddMailKitSimplifiedEmailReceiver(configuration);
            //services.AddHttpClient<IJasperApiClient, JasperApiClient>()
            //    .AddPolicyHandler(HttpPolicies.ExponentialRetry)
            //    .AddPolicyHandler(HttpPolicies.CircuitBreaker);
            //services.AddScoped<IJasperApiClient, JasperApiClient>();

            services.AddControllers()
                .AddJsonOptions(o =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        o.JsonSerializerOptions.WriteIndented = true;
                    }
                    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                });

            var app = builder.Build();

            // Use middleware
            app.UseHttpLogging();
            app.UseHttpsRedirection();
            app.UseExceptionHandler("/error");

            // Configure the HTTP request pipeline
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{apiTitle} {apiVersion}"));
            app.UsePathBase(new PathString($"/api/{apiVersion}"));

            //app.UseAuthentication();
            //app.UseAuthorization();

            app.UseRouting();
            app.MapControllers();

            //if (app.Environment.IsDevelopment())
            //{
            //    using var scope = app.Services.CreateScope();
            //    using var context = scope.ServiceProvider
            //        .GetRequiredService<MyDbContext>();
            //    context.Database.EnsureCreated();
            //    await context.SeedAsync();
            //}

            await app.RunAsync();
        }
    }
}