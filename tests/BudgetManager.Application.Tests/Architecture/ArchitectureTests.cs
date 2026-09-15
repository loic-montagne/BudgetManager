using BudgetManager.Application.Abstractions.Authentication;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ArchitectureTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(DependencyInjection).Assembly;

    [Fact]
    public void EveryConcreteMediatRRequest_HasAHandler()
    {
        // Arrange

        var requestTypes = ApplicationAssembly
            .GetTypes()
            .Where(IsConcreteRequest)
            .ToArray();

        var handledRequestTypes = ApplicationAssembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface)
            .SelectMany(type => type.GetInterfaces())
            .Where(IsRequestHandlerContract)
            .Select(handlerContract =>
                handlerContract.GetGenericArguments()[0])
            .Distinct()
            .ToHashSet();

        // Act

        var missingHandlers = requestTypes
            .Where(requestType =>
                !handledRequestTypes.Contains(requestType))
            .Select(requestType =>
                requestType.FullName ?? requestType.Name)
            .ToArray();

        // Assert

        Assert.Empty(missingHandlers);
    }

    [Fact]
    public void EveryAuthorizedRequest_DeclaresAtLeastOneNonEmptyRole()
    {
        // Arrange

        var requestTypes = ApplicationAssembly
            .GetTypes()
            .Where(type =>
                typeof(IAuthorizedRequest).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                !type.IsInterface)
            .ToArray();

        // Act and Assert

        foreach (var requestType in requestTypes)
        {
            var constructor = requestType
                .GetConstructors()
                .OrderBy(item => item.GetParameters().Length)
                .First();

            var arguments = constructor
                .GetParameters()
                .Select(parameter =>
                    parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null)
                .ToArray();

            var request = (IAuthorizedRequest)constructor.Invoke(arguments);

            Assert.NotEmpty(request.RequiredRoles);

            Assert.DoesNotContain(
                request.RequiredRoles,
                string.IsNullOrWhiteSpace);
        }
    }

    [Fact]
    public void AddApplication_RegistersContextsValidatorsAndMediatRHandlers()
    {
        // Arrange

        var services = new ServiceCollection();

        // Act

        services.AddApplication();

        // Assert

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType.Name == "IBudgetContext" &&
                descriptor.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType.IsGenericType &&
                descriptor.ServiceType.GetGenericTypeDefinition() ==
                    typeof(IValidator<>));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType.IsGenericType &&
                descriptor.ServiceType.GetGenericTypeDefinition() ==
                    typeof(IRequestHandler<,>));
    }

    [Fact]
    public void EveryValidator_IsPublicAndSealed()
    {
        // Arrange

        var validatorTypes = ApplicationAssembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                IsValidator(type))
            .ToArray();

        // Act and Assert

        foreach (var validatorType in validatorTypes)
        {
            Assert.True(
                validatorType.IsPublic,
                $"{validatorType.FullName} must be public.");

            Assert.True(
                validatorType.IsSealed,
                $"{validatorType.FullName} must be sealed.");
        }
    }


    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        // Arrange

        var referencedAssemblies = ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        // Act

        var referencesInfrastructure = referencedAssemblies
            .Any(name => string.Equals(
                name,
                "BudgetManager.Infrastructure",
                StringComparison.Ordinal));

        // Assert

        Assert.False(referencesInfrastructure);
    }

    [Fact]
    public void Domain_DoesNotReferenceApplicationOrInfrastructure()
    {
        // Arrange

        var domainAssembly = typeof(BudgetManager.Domain.Entities.Budget).Assembly;

        var referencedAssemblies = domainAssembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        // Act

        var forbiddenReferences = referencedAssemblies
            .Where(name =>
                string.Equals(
                    name,
                    "BudgetManager.Application",
                    StringComparison.Ordinal) ||
                string.Equals(
                    name,
                    "BudgetManager.Infrastructure",
                    StringComparison.Ordinal))
            .ToArray();

        // Assert

        Assert.Empty(forbiddenReferences);
    }

    private static bool IsConcreteRequest(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return false;

        return type
            .GetInterfaces()
            .Any(@interface =>
                (@interface.IsGenericType &&
                 @interface.GetGenericTypeDefinition() == typeof(IRequest<>)) ||
                @interface == typeof(IRequest));
    }

    private static bool IsRequestHandlerContract(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericTypeDefinition =
            type.GetGenericTypeDefinition();

        return genericTypeDefinition == typeof(IRequestHandler<>) ||
               genericTypeDefinition == typeof(IRequestHandler<,>);
    }

    private static bool IsValidator(Type type)
    {
        return type
            .GetInterfaces()
            .Any(@interface =>
                @interface.IsGenericType &&
                @interface.GetGenericTypeDefinition() == typeof(IValidator<>));
    }
}
