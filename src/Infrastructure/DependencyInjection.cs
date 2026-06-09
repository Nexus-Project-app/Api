using System.Text;
using Amazon.S3;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Storage;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Storage;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddStorage(configuration)
            .AddHealthChecks(configuration)
            .AddAuthorizationInternal();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var secretPath = "/opt/nexus/secrets/db_connection_string";
    
        var connectionString = File.Exists(secretPath)
            ? File.ReadAllText(secretPath).Trim()
            : configuration.GetConnectionString("Database")
              ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);

        return services;
    }

    private static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"] ?? "FileSystem";

        if (provider == "Garage")
        {
            var accessKey = GarageAttachmentStorage.ReadSecret(
                "/opt/nexus/secrets/garage_access_key",
                configuration["Storage:AccessKey"] ?? string.Empty);

            var secretKey = GarageAttachmentStorage.ReadSecret(
                "/opt/nexus/secrets/garage_secret_key",
                configuration["Storage:SecretKey"] ?? string.Empty);

            var s3Config = new AmazonS3Config()
            {
                ServiceURL = configuration["Storage:Endpoint"],
                ForcePathStyle = true,
            };

            services.AddSingleton<IAmazonS3>(_ =>
                new AmazonS3Client(accessKey, secretKey, s3Config));

            services.AddScoped<IAttachmentStorage, GarageAttachmentStorage>();
        }
        else
        {
            services.AddScoped<IAttachmentStorage, FileSystemAttachmentStorage>();
        }

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}
