# Todo App

A full-stack todo application with an Angular 21 frontend and ASP.NET Core (.NET 10) backend, orchestrated with .NET Aspire.

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (required for PostgreSQL and Redis containers)

## Architecture

```
Todo.slnx
├── apphost/          .NET Aspire AppHost (orchestration)
├── servicedefaults/  Aspire ServiceDefaults (health checks, OpenTelemetry)
├── api/              ASP.NET Core Web API (EF Core + PostgreSQL + Redis)
└── ui/todo-ui/       Angular 21 frontend
```

### Infrastructure (managed by Aspire via Docker)

- **PostgreSQL** on port **5488** with persistent volume `todo-postgres-data`
- **Redis** for output caching on the GET endpoint

## Running the app

### Backend (recommended — via Aspire)

```bash
dotnet run --project apphost
```

This starts the Aspire dashboard, PostgreSQL, Redis, and the API. The dashboard URL is printed in the console.

EF Core migrations are applied automatically on API startup.

### Frontend

```bash
cd ui/todo-ui
npm install
npm start
```

The UI runs at **http://localhost:4200** and proxies `/api` requests to the .NET backend.

## API Endpoints

| Method | URL                      | Description       |
|--------|--------------------------|-------------------|
| GET    | `/api/todos`             | List all todos    |
| POST   | `/api/todos`             | Create a todo     |
| PUT    | `/api/todos/{id}/toggle` | Toggle completion |
| DELETE | `/api/todos/{id}`        | Delete a todo     |

## Key Implementation Details

- **Angular 21 is zoneless** — UI state uses signals (`signal<Todo[]>`) for change detection
- **EF Core + Npgsql** — `TodoDbContext` with `DbSet<TodoItem>`, Aspire-managed connection via `AddNpgsqlDbContext`
- **Redis output caching** — GET `/api/todos` is cached with tag `"todos"`; mutations evict the cache via `IOutputCacheStore.EvictByTagAsync`
- **Aspire ServiceDefaults** — provides health checks (`/health`, `/alive`), OpenTelemetry metrics and tracing
- **Migration retry** — startup retries `Database.Migrate()` up to 5 times (2s delay) to handle container readiness

## EF Core Migrations

```bash
# Ensure dotnet-ef tool is installed
dotnet tool install --global dotnet-ef

# Add a new migration
dotnet ef migrations add <MigrationName> --project api

# Migrations are auto-applied on startup, but can also be run manually:
dotnet ef database update --project api
```
