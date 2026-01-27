# Todo App

A full-stack todo application with an Angular frontend and ASP.NET Core Web API backend.

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

## Run the backend

```bash
cd api
dotnet run
```

The API runs at **http://localhost:5181**.
Swagger UI is available at http://localhost:5181/swagger.

## Run the frontend

```bash
cd ui/todo-ui
npm install
npm start
```

The UI runs at **http://localhost:4200**.

The Angular dev server proxies `/api` requests to the .NET backend, so both must be running.

## API Endpoints

| Method | URL                      | Description       |
|--------|--------------------------|-------------------|
| GET    | `/api/todos`             | List all todos    |
| POST   | `/api/todos`             | Create a todo     |
| PUT    | `/api/todos/{id}/toggle` | Toggle completion |
| DELETE | `/api/todos/{id}`        | Delete a todo     |
