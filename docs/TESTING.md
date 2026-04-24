# Testing Guide

This document covers the testing strategy, test projects, and code coverage requirements for IssueTrackerApp.

## Test Projects

| Project | Type | Description |
|---------|------|-------------|
| `Domain.Tests` | Unit | MediatR handlers, validators, models, DTOs, Result<T> utilities |
| `Architecture.Tests` | Rules | Layer dependencies, naming conventions, code structure rules |
| `Web.Tests` | Unit | Web service tests, security tests |
| `Web.Tests.Bunit` | Component | Blazor UI component tests with bUnit |
| `Web.Tests.Integration` | Integration | Full HTTP request pipelines with real MongoDB |

## Running Tests

### Run All Tests

```bash
dotnet test IssueTrackerApp.slnx
```

### Run Specific Test Project

```bash
dotnet test tests/Domain.Tests
dotnet test tests/Web.Tests
dotnet test tests/Architecture.Tests
dotnet test tests/Web.Tests.Bunit
dotnet test tests/Web.Tests.Integration
```

### Run with Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage reports are generated in `TestResults/<guid>/coverage.cobertura.xml`.

## Test Naming Convention

```
{MethodUnderTest}_{Scenario}_{ExpectedBehavior}
```

Examples:
- `CreateIssue_WithValidData_ReturnsSuccessResult`
- `GetIssueById_WhenNotFound_ReturnsFailure`
- `ValidateIssue_WithEmptyTitle_HasValidationError`

## Code Coverage Requirements

| Metric | Threshold |
|--------|-----------|
| Line Coverage | ≥ 80% |
| Branch Coverage | ≥ 60% |

Coverage is enforced in CI via the `squad-test.yml` workflow.

## Test Categories

### Unit Tests (Domain.Tests, Web.Tests)

- Fast execution (~1ms per test)
- No external dependencies
- Mock all interfaces using NSubstitute
- Test single units of functionality

### Architecture Tests (Architecture.Tests)

- Verify layer dependencies using NetArchTest.Rules
- Ensure naming conventions are followed
- Validate code structure patterns

### Component Tests (Web.Tests.Bunit)

- Test Blazor components in isolation
- Mock services (IMediator, IIssueService, etc.)
- Verify component rendering and interactions

### Integration Tests (Web.Tests.Integration)

- Full HTTP pipeline testing
- Real MongoDB via Testcontainers
- Test complete feature workflows

## Test Dependencies

| Package | Purpose |
|---------|---------|
| `xunit` | Test framework |
| `FluentAssertions` | Assertion library |
| `NSubstitute` | Mocking framework |
| `bUnit` | Blazor component testing |
| `Testcontainers.MongoDb` | MongoDB integration testing |
| `NetArchTest.Rules` | Architecture testing |
| `coverlet.collector` | Code coverage |

## CI/CD Integration

Tests run automatically on:
- Push to `main` branch
- Pull requests to any branch
- Manual workflow dispatch

The `squad-test.yml` workflow:
1. Builds the solution
2. Runs all test projects in parallel
3. Collects and merges coverage reports
4. Publishes results to Codecov
5. Fails if coverage drops below 80%

## Writing Tests

### Unit Test Example

```csharp
public class CreateIssueCommandHandlerTests
{
    private readonly IRepository<Issue> _repository = Substitute.For<IRepository<Issue>>();
    private readonly CreateIssueCommandHandler _sut;

    public CreateIssueCommandHandlerTests()
    {
        _sut = new CreateIssueCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateIssueCommand("Title", "Description", userId: "user-1");
        _repository.AddAsync(Arg.Any<Issue>())
            .Returns(Task.FromResult(Result.Ok()));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Issue>());
    }
}
```

### bUnit Test Example

```csharp
public class StatusBadgeTests : BunitTestBase
{
    [Fact]
    public void Render_WithStatus_ShowsCorrectBadge()
    {
        // Arrange
        var status = CreateTestStatus("open", "Open", "#22c55e");

        // Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        cut.Markup.Should().Contain("Open");
        cut.Find("span").GetAttribute("style").Should().Contain("#22c55e");
    }
}
```

### Integration Test Example

```csharp
public class IssueEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateIssue_WithValidData_Returns201()
    {
        // Arrange
        var issue = new CreateIssueRequest("Test Issue", "Description");

        // Act
        var response = await Client.PostAsJsonAsync("/api/issues", issue);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<IssueDto>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("Test Issue");
    }
}
```

## Coverage Report

Current test coverage (as of last run):

| Project | Line Coverage | Tests |
|---------|---------------|-------|
| Domain | 82% | 255 |
| Web | ~75% | 155 |
| Architecture | 100% | 38 |
| bUnit | ~60% | 328 |

**Total: 776+ tests**

## Troubleshooting

### Integration Tests Fail Locally

Ensure Docker is running for Testcontainers:

```bash
docker info
```

### bUnit Tests Have Async Issues

Use `WaitForState()` for async component updates:

```csharp
cut.WaitForState(() => cut.FindAll("li").Count > 0);
```

### Coverage Not Collected

Ensure `coverlet.collector` package is referenced:

```xml
<PackageReference Include="coverlet.collector" />
```
