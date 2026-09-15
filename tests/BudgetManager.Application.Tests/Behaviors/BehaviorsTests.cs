using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Behaviors;
using BudgetManager.Application.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class BehaviorsTests
{
    private sealed record PlainRequest;

    private sealed record SecuredRequest : IAuthorizedRequest
    {
        public IReadOnlyCollection<string> RequiredRoles => ["User"];
    }

    [Fact]
    public async Task AuthorizationBehavior_WhenRequestIsNotSecured_CallsNextHandler()
    {
        // Arrange

        var currentUser = new TestCurrentUser(
            authenticated: false);

        var behavior = new AuthorizationBehavior<PlainRequest, int>(
            currentUser);

        var request = new PlainRequest();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult(42),
            cancellationToken);

        // Assert

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task AuthorizationBehavior_WhenUserIsNotAuthenticated_ThrowsUnauthenticatedException()
    {
        // Arrange

        var currentUser = new TestCurrentUser(
            authenticated: false);

        var behavior = new AuthorizationBehavior<SecuredRequest, int>(
            currentUser);

        var request = new SecuredRequest();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var action = () => behavior.Handle(
            request,
            _ => Task.FromResult(1),
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<UnauthenticatedException>(action);
    }

    [Fact]
    public async Task AuthorizationBehavior_WhenUserDoesNotHaveRequiredRole_ThrowsForbiddenAccessException()
    {
        // Arrange

        var currentUser = new TestCurrentUser(
            authenticated: true,
            roles: ["Guest"]);

        var behavior = new AuthorizationBehavior<SecuredRequest, int>(
            currentUser);

        var request = new SecuredRequest();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var action = () => behavior.Handle(
            request,
            _ => Task.FromResult(1),
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<ForbiddenAccessException>(action);
    }

    [Fact]
    public async Task AuthorizationBehavior_WhenUserIsAuthorized_CallsNextHandler()
    {
        // Arrange

        var currentUser = new TestCurrentUser(
            authenticated: true,
            roles: ["User"]);

        var behavior = new AuthorizationBehavior<SecuredRequest, int>(
            currentUser);

        var request = new SecuredRequest();
        var nextWasCalled = false;
        var cancellationToken = TestContext.Current.CancellationToken;

        Task<int> Next(CancellationToken _)
        {
            nextWasCalled = true;
            return Task.FromResult(7);
        }

        // Act

        var result = await behavior.Handle(
            request,
            Next,
            cancellationToken);

        // Assert

        Assert.True(nextWasCalled);
        Assert.Equal(7, result);
    }

    [Fact]
    public async Task AuthorizationBehavior_WhenCallingNext_ForwardsCancellationToken()
    {
        // Arrange

        var currentUser = new TestCurrentUser(
            authenticated: true);

        var behavior =
            new AuthorizationBehavior<PlainRequest, int>(
                currentUser);

        var request = new PlainRequest();

        var expectedCancellationToken =
            TestContext.Current.CancellationToken;

        CancellationToken receivedCancellationToken = default;

        Task<int> Next(CancellationToken cancellationToken)
        {
            receivedCancellationToken = cancellationToken;

            return Task.FromResult(42);
        }

        // Act

        var result = await behavior.Handle(
            request,
            Next,
            expectedCancellationToken);

        // Assert

        Assert.Equal(42, result);

        Assert.Equal(
            expectedCancellationToken,
            receivedCancellationToken);
    }

    [Fact]
    public async Task ValidationBehavior_WhenNoValidatorIsRegistered_CallsNextHandler()
    {
        // Arrange

        var validators = Array.Empty<IValidator<PlainRequest>>();

        var behavior = new ValidationBehavior<PlainRequest, int>(
            validators);

        var request = new PlainRequest();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult(3),
            cancellationToken);

        // Assert

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidationFails_AggregatesFailuresAndDoesNotCallNextHandler()
    {
        // Arrange

        var firstValidator = new InlineValidator<PlainRequest>();

        firstValidator
            .RuleFor(x => x)
            .Custom((_, context) =>
            {
                context.AddFailure(new ValidationFailure(
                    "FirstProperty",
                    "First validation error.")
                {
                    ErrorCode = "E1"
                });
            });

        var secondValidator = new InlineValidator<PlainRequest>();

        secondValidator
            .RuleFor(x => x)
            .Custom((_, context) =>
            {
                context.AddFailure(new ValidationFailure(
                    "SecondProperty",
                    "Second validation error.")
                {
                    ErrorCode = "E2"
                });
            });

        IValidator<PlainRequest>[] validators =
        [
            firstValidator,
        secondValidator
        ];

        var behavior = new ValidationBehavior<PlainRequest, int>(
            validators);

        var request = new PlainRequest();

        var nextWasCalled = false;

        Task<int> Next(CancellationToken _)
        {
            nextWasCalled = true;

            return Task.FromResult(0);
        }

        // Act

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            behavior.Handle(
                request,
                Next,
                TestContext.Current.CancellationToken));

        // Assert

        Assert.False(nextWasCalled);

        Assert.Collection(
            exception.ValidationErrors.OrderBy(x => x.ErrorCode),
            first =>
            {
                Assert.Equal("E1", first.ErrorCode);
                Assert.Equal("FirstProperty", first.PropertyName);
                Assert.Equal("First validation error.", first.ErrorMessage);
            },
            second =>
            {
                Assert.Equal("E2", second.ErrorCode);
                Assert.Equal("SecondProperty", second.PropertyName);
                Assert.Equal("Second validation error.", second.ErrorMessage);
            });
    }

    [Fact]
    public async Task ValidationBehavior_WhenMultipleValidatorsAreRegistered_ExecutesThemSequentially()
    {
        // Arrange

        var executionOrder = new List<int>();

        var firstValidator = new InlineValidator<PlainRequest>();

        firstValidator
            .RuleFor(request => request)
            .CustomAsync(async (_, _, _) =>
            {
                executionOrder.Add(1);

                await Task.Yield();

                executionOrder.Add(2);
            });

        var secondValidator = new InlineValidator<PlainRequest>();

        secondValidator
            .RuleFor(request => request)
            .CustomAsync(async (_, _, _) =>
            {
                executionOrder.Add(3);

                await Task.Yield();

                executionOrder.Add(4);
            });

        IValidator<PlainRequest>[] validators =
        [
            firstValidator,
            secondValidator
        ];

        var behavior = new ValidationBehavior<PlainRequest, int>(
            validators);

        var request = new PlainRequest();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        await behavior.Handle(
            request,
            _ => Task.FromResult(0),
            cancellationToken);

        // Assert

        Assert.Equal(
            [1, 2, 3, 4],
            executionOrder);
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidationSucceeds_CallsNextHandler()
    {
        // Arrange

        var validator = new InlineValidator<PlainRequest>();

        validator
            .RuleFor(request => request)
            .Custom((_, _) =>
            {
            });

        IValidator<PlainRequest>[] validators =
        [
            validator
        ];

        var behavior =
            new ValidationBehavior<PlainRequest, int>(
                validators);

        var request = new PlainRequest();

        var nextWasCalled = false;

        var expectedCancellationToken =
            TestContext.Current.CancellationToken;

        CancellationToken receivedCancellationToken = default;

        Task<int> Next(CancellationToken cancellationToken)
        {
            nextWasCalled = true;
            receivedCancellationToken = cancellationToken;

            return Task.FromResult(42);
        }

        // Act

        var result = await behavior.Handle(
            request,
            Next,
            expectedCancellationToken);

        // Assert

        Assert.True(nextWasCalled);
        Assert.Equal(42, result);

        Assert.Equal(
            expectedCancellationToken,
            receivedCancellationToken);
    }
}
