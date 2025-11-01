using System.Reflection;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using MailKitSimplified.Receiver;
using MailKitSimplified.Sender;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
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

            // API versioning default
            var apiVersionDefault = new ApiVersion(1, 0);

            // Add API versioning including 'api-supported-versions' response header
            services
                .AddApiVersioning(options =>
                {
                    options.ReportApiVersions = true;
                    options.DefaultApiVersion = apiVersionDefault;
                    options.AssumeDefaultVersionWhenUnspecified = true;

                    // Combine media type "ver" and URL segment readers
                    options.ApiVersionReader = ApiVersionReader.Combine(
                        new MediaTypeApiVersionReader("ver"),
                        new UrlSegmentApiVersionReader());
                })
                // Add the versioned API explorer which is needed by Swagger generation
                .AddApiExplorer(options =>
                {
                    // Format: "v1" or "v1.0"
                    options.GroupNameFormat = "'v'VVV"; // e.g. v1, v1.0
                    options.SubstituteApiVersionInUrl = true;
                    // Optional: set default API version if not set here; we already set it on AddApiVersioning
                });

            // Add swagger generation (we will register one doc per discovered API version)
            var apiTitle = "MailKitSimplified API";
            var apiDescription = "##### An ASP.NET Core Web API for MailKitSimplified IMAP queries";
            var minorVersion = apiVersionDefault.MinorVersion > 0 ? $".{apiVersionDefault.MinorVersion}" : string.Empty;
            var apiVersion = $"v{apiVersionDefault.MajorVersion}{minorVersion}";
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                //options.SwaggerDoc(apiVersion, new OpenApiInfo
                //{
                //    Title = apiTitle,
                //    Description = apiDescription,
                //    Version = apiVersion
                //});
                // Common OpenAPI metadata (if you want to add more per-version info, see below)
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter JWT bearer token"
                });

                // Include XML comments if present
                var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            // Add HTTP logging
            services.AddHttpLogging(options =>
            {
                options.LoggingFields = HttpLoggingFields.Request;
            });

            // Add general services
            services.AddLogging();
            services.AddMemoryCache();

            // Add custom services
            services.AddMailKitSimplifiedEmailSender(configuration);
            services.AddMailKitSimplifiedEmailReceiver(configuration);
            services.AddHttpClient("MyApiService")
                .AddPolicyHandler(HttpPolicies.ExponentialRetry)
                .AddPolicyHandler(HttpPolicies.CircuitBreaker);

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

            // Configure the HTTP request pipeline for Swagger with versioning
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwagger(options =>
            {
                // Optionally set a custom route template for swagger JSON
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                // Create a Swagger endpoint for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    var name = description.GroupName; // e.g. "v1"
                    options.SwaggerEndpoint($"/swagger/{name}/swagger.json", $"{name.ToUpperInvariant()}");
                }

                // Optionally set the Swagger UI at the app root ("/")
                //options.RoutePrefix = string.Empty;
            });

            //app.UseAuthentication();
            //app.UseAuthorization();

            app.UseRouting();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}