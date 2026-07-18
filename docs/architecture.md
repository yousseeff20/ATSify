# Architecture Overview

This project adheres to Clean Architecture principles.

The solution is divided into the following layers:
- **Domain**: Contains aggregates, entities, value objects, and domain events.
- **Application**: Contains MediatR commands, queries, validators, DTOs, interfaces. Organized by Features.
- **Infrastructure**: Contains persistence (EF Core context, repositories, migrations), identity, email, file storage, external services.
- **API**: ASP.NET Core REST API exposing the endpoints.
