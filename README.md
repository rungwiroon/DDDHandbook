# DDD Restaurant Handbook

The executable companion for the DDD Restaurant Handbook: a .NET 10 API, Vue 3 client, SQLite persistence, Outbox reliability slice, and tests across domain, application, HTTP integration, and Vue UI states.

## Prerequisites

- .NET SDK 10.0.201 or later in the 10.0 feature band
- Node.js 24+ and npm 11+

## Run

Apply the SQLite migrations once before starting the API. The API deliberately does not change the database schema at startup:

```sh
dotnet ef database update \
  --project src/Restaurant.Infrastructure \
  --startup-project src/Restaurant.Api
```

Run the API (health check at `http://localhost:5000/health`):

```sh
dotnet run --project src/Restaurant.Api --urls http://localhost:5000
```

In another terminal, run the Vue application:

```sh
cd src/restaurant-web
npm install
npm run dev
```

If `POST /orders` returns `500` on a new local setup, stop the API and run the migration command above. An empty `restaurant.db` has no tables until migrations are applied.

## Test and build

```sh
dotnet test Restaurant.sln
cd src/restaurant-web && npm install && npm run build && npm test
```

## Project layout

- `Restaurant.Domain`: business model and Vogen value objects
- `Restaurant.Application`: use-case orchestration
- `Restaurant.Infrastructure`: EF Core/SQLite persistence and Outbox adapters
- `Restaurant.Api`: ASP.NET Core HTTP host
- `restaurant-web`: Vue 3 + TypeScript client
- `tests`: domain, application, and HTTP integration tests
