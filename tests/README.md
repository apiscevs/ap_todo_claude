# Todo API Tests

Comprehensive test suite for the Todo API project, including unit tests and integration tests with real database using Testcontainers.

## Test Projects

### 1. Todo.Api.UnitTests (11 tests)
Unit tests for models and basic functionality:
- **TodoItemTests**: Tests for the TodoItem model
  - Property initialization and defaults
  - Property setters
  - Toggle completion status
  - Various title values
- **CreateTodoRequestTests**: Tests for the CreateTodoRequest record
  - Record initialization
  - Value equality (record semantics)
  - Various title inputs

### 2. Todo.Api.IntegrationTests (32 tests)
Integration tests using **Testcontainers** to spin up a real PostgreSQL database for testing.

#### RestApiTests (13 tests)
Tests for REST API minimal endpoints:
- GET `/api/todos` - List all todos
- POST `/api/todos` - Create new todo
- PUT `/api/todos/{id}/toggle` - Toggle completion status
- DELETE `/api/todos/{id}` - Delete todo
- Complete workflow test covering all operations

#### GraphQLApiTests (11 tests)
Tests for GraphQL queries and mutations:
- **Queries**:
  - `todos` - Get all todos with filtering, sorting, projections
  - `todoById(id)` - Get single todo by ID
- **Mutations**:
  - `createTodo(input)` - Create new todo
  - `toggleTodo(id)` - Toggle completion status
  - `deleteTodo(id)` - Delete todo
- Error handling for non-existent todos
- Complete workflow test

#### DatabaseTests (11 tests)
Tests for database operations and EF Core functionality:
- Database creation and migration
- CRUD operations
- Auto-increment ID generation
- Querying and filtering
- Ordering
- Concurrent operations
- Schema validation (NOT NULL constraints)
- Default values

## Technology Stack

### Testing Frameworks
- **xUnit** - Test framework
- **FluentAssertions** - Fluent assertion library for readable tests
- **Moq** - Mocking library (for unit tests)

### Integration Testing
- **Testcontainers.PostgreSql** (4.10.0) - Spins up real PostgreSQL container for integration tests
- **Microsoft.AspNetCore.Mvc.Testing** (10.0.2) - WebApplicationFactory for in-memory API testing
- **PostgreSQL 17 Alpine** - Lightweight PostgreSQL container image

## Running the Tests

## UI E2E (Playwright)

Prerequisites:
- Backend and UI running (see root README)
- `BASE_URL` set (e.g. `http://localhost:4200`)

Run the Playwright todo smoke test:
```bash
BASE_URL=http://localhost:4200 npx playwright test tests/todo.spec.ts
```

Run all Playwright tests:
```bash
BASE_URL=http://localhost:4200 npx playwright test
```

### Run All Tests
```bash
dotnet test
```

### Run Only Unit Tests
```bash
dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj
```

### Run Only Integration Tests
```bash
dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~TestName"
```

## Prerequisites

### Docker
Integration tests require Docker to be running, as Testcontainers will automatically:
1. Pull the `postgres:17-alpine` image (if not already present)
2. Start a PostgreSQL container
3. Run migrations
4. Execute tests
5. Clean up the container

Ensure Docker Desktop or Docker Engine is running before executing integration tests.

## Test Configuration

### TodoApiFactory
The `TodoApiFactory` class extends `WebApplicationFactory<Program>` and:
- Starts a PostgreSQL container using Testcontainers
- Configures the test application to use the containerized database
- Replaces Redis output caching with in-memory caching for tests
- Runs database migrations automatically
- Cleans up resources after tests complete

### Database Isolation
Each test class receives its own instance of the factory, but tests within a class share the database. Tests use a `ClearDatabase()` helper method to ensure test isolation.

## Test Coverage

### API Endpoints Covered
- ✅ All REST API endpoints (GET, POST, PUT, DELETE)
- ✅ All GraphQL queries and mutations
- ✅ Error cases (404, validation errors, GraphQL exceptions)
- ✅ Complete workflows (create → read → update → delete)

### Database Features Covered
- ✅ EF Core migrations
- ✅ CRUD operations
- ✅ Transactions
- ✅ Concurrent operations
- ✅ Query filtering and ordering
- ✅ Schema constraints (NOT NULL, auto-increment)

### Business Logic Covered
- ✅ Todo creation with defaults
- ✅ Toggle completion status
- ✅ Deletion
- ✅ Ordering by ID
- ✅ Cache invalidation (via in-memory cache in tests)

## Test Results Summary

```
✅ Total: 43 tests
✅ Passed: 43
❌ Failed: 0
⏭️  Skipped: 0

Unit Tests:        11/11 passing
Integration Tests: 32/32 passing
```

## Continuous Integration

These tests are designed to run in CI/CD pipelines. Ensure your CI environment has:
- .NET 10 SDK
- Docker (for integration tests with Testcontainers)

## Notes

- **Entity Framework Version Warnings**: You may see warnings about EF Core version conflicts (10.0.1 vs 10.0.2). These are harmless and don't affect test execution.
- **Test Duration**: Unit tests complete in ~40ms. Integration tests take ~1-2 seconds due to database container setup.
- **Parallel Execution**: xUnit runs test classes in parallel by default. Tests within a class run sequentially.
