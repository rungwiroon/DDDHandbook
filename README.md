# DDD Restaurant Handbook

The executable companion for the DDD Restaurant Handbook. It is intentionally at M0: a clean .NET 10 API, a Vue 3 client, PostgreSQL for future persistence, and test projects. Restaurant behavior is introduced chapter by chapter.

## Prerequisites

- .NET SDK 10.0.201 or later in the 10.0 feature band
- Node.js 24+ and npm 11+
- Docker Desktop (only required for PostgreSQL)

## Run

Start PostgreSQL for later persistence chapters:

```sh
docker compose up -d postgres
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

## Test and build

```sh
dotnet test Restaurant.sln
cd src/restaurant-web && npm install && npm run build && npm test
```

## Project layout

- `Restaurant.Domain`: business model and Vogen value objects (introduced in later chapters)
- `Restaurant.Application`: use-case orchestration
- `Restaurant.Infrastructure`: adapters such as EF Core persistence (introduced later)
- `Restaurant.Api`: ASP.NET Core HTTP host
- `restaurant-web`: Vue 3 + TypeScript client
- `tests`: domain, application, and HTTP integration tests
