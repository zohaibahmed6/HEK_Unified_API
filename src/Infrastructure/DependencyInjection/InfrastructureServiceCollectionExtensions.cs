using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HekCoreApi.Infrastructure.DependencyInjection;

/// <summary>
/// Convention-based registration for Block 2's domain repositories: every concrete, non-abstract
/// class in this assembly that implements exactly one interface declared in the Application
/// assembly is registered Scoped, repository-interface -> repository-implementation. This keeps
/// Program.cs from growing one manual registration line per domain group (18 groups) while still
/// satisfying "register dependencies only inside Program.cs" (the call to this method IS that
/// registration, just batched by convention).
///
/// Uses TryAddScoped, so it never overrides an explicit registration already made in Program.cs
/// (e.g. ISecretProvider's Singleton lifetime, or TenantRegistryDbContext's AddDbContext-managed
/// registration) - call this AFTER any manual registrations that must win.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
    {
        var applicationAssemblyName = typeof(Application.Common.Interfaces.ISecretProvider).Assembly.GetName().Name;
        var infrastructureAssembly = typeof(InfrastructureServiceCollectionExtensions).Assembly;

        var candidates = infrastructureAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsPublic);

        foreach (var implementationType in candidates)
        {
            var applicationInterfaces = implementationType.GetInterfaces()
                .Where(i => i.Assembly.GetName().Name == applicationAssemblyName)
                .ToList();

            if (applicationInterfaces.Count == 1)
            {
                services.TryAddScoped(applicationInterfaces[0], implementationType);
            }
        }

        return services;
    }
}
