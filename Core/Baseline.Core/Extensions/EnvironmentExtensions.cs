using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Fluent environment-based extension methods for conditional service and middleware registration.
/// </summary>
public static class EnvironmentExtensions
{
    /// <summary>
    /// Conditionally configures services only in Development environment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="env">The web host environment.</param>
    /// <param name="configure">The configuration action to execute in Development.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.IfDevelopment(env, s => s.AddDatabaseDeveloperPageExceptionFilter());
    /// </code>
    /// </example>
    public static IServiceCollection IfDevelopment(
        this IServiceCollection services,
        IHostEnvironment env,
        Action<IServiceCollection> configure)
    {
        if (env.IsDevelopment())
        {
            configure(services);
        }

        return services;
    }

    /// <summary>
    /// Conditionally configures services only in non-Development environments (Staging, Production).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="env">The web host environment.</param>
    /// <param name="configure">The configuration action to execute in non-Development.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.IfNotDevelopment(env, s => s.AddResponseCompression());
    /// </code>
    /// </example>
    public static IServiceCollection IfNotDevelopment(
        this IServiceCollection services,
        IHostEnvironment env,
        Action<IServiceCollection> configure)
    {
        if (!env.IsDevelopment())
        {
            configure(services);
        }

        return services;
    }

    /// <summary>
    /// Conditionally configures services for a specific environment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="env">The web host environment.</param>
    /// <param name="environmentName">The target environment name.</param>
    /// <param name="configure">The configuration action to execute.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection IfEnvironment(
        this IServiceCollection services,
        IHostEnvironment env,
        string environmentName,
        Action<IServiceCollection> configure)
    {
        if (env.IsEnvironment(environmentName))
        {
            configure(services);
        }

        return services;
    }

    /// <summary>
    /// Conditionally configures middleware only in Development environment.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="env">The web host environment.</param>
    /// <param name="configure">The configuration action to execute in Development.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.IfDevelopment(env, a => a.UseDeveloperExceptionPage());
    /// </code>
    /// </example>
    public static IApplicationBuilder IfDevelopment(
        this IApplicationBuilder app,
        IHostEnvironment env,
        Action<IApplicationBuilder> configure)
    {
        if (env.IsDevelopment())
        {
            configure(app);
        }

        return app;
    }

    /// <summary>
    /// Conditionally configures middleware only in non-Development environments.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="env">The web host environment.</param>
    /// <param name="configure">The configuration action to execute in non-Development.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.IfNotDevelopment(env, a => a.UseHsts());
    /// </code>
    /// </example>
    public static IApplicationBuilder IfNotDevelopment(
        this IApplicationBuilder app,
        IHostEnvironment env,
        Action<IApplicationBuilder> configure)
    {
        if (!env.IsDevelopment())
        {
            configure(app);
        }

        return app;
    }

    /// <summary>
    /// Conditionally configures WebApplication only in Development environment.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="env">The web host environment.</param>
    /// <param name="configure">The configuration action to execute in Development.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication IfDevelopment(
        this WebApplication app,
        IHostEnvironment env,
        Action<WebApplication> configure)
    {
        if (env.IsDevelopment())
        {
            configure(app);
        }

        return app;
    }

    /// <summary>
    /// Conditionally configures WebApplication only in non-Development environments.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="env">The web host environment.</param>
    /// <param name="configure">The configuration action to execute in non-Development.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication IfNotDevelopment(
        this WebApplication app,
        IHostEnvironment env,
        Action<WebApplication> configure)
    {
        if (!env.IsDevelopment())
        {
            configure(app);
        }

        return app;
    }
}
