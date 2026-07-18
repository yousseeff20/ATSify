# AGENTS.md

# AI Development Instructions

This file defines the mandatory development rules for this repository.

## Source of Truth

Before implementing any feature, always read:

- ATS_Software_Requirements_Architecture_Specification.md

This specification is the single source of truth.

If any conflict exists between this file and the specification, the specification takes precedence.

---

# Primary Objective

Build production-ready software.

Every implementation should prioritize:

- Correctness
- Maintainability
- Readability
- Performance
- Security
- Scalability
- Consistency

Never prioritize speed over quality.

---

# Think Before Coding

Before writing any code:

- Read the requirement carefully.
- Understand the business objective.
- Search the existing solution.
- Identify reusable components.
- Identify affected modules.
- Plan the implementation.
- Implement the smallest correct solution.

Never guess requirements.

If something is ambiguous, ask for clarification.

---

# Existing Code First

Always inspect the codebase before creating anything new.

Search for existing:

- Services
- Repositories
- DTOs
- Validators
- Handlers
- Entities
- Interfaces
- Extension Methods
- Middleware
- Utilities
- Specifications
- Mapping Profiles
- Constants
- Enums

If a similar implementation exists:

- Reuse it.
- Extend it.
- Refactor it if needed.

Never duplicate functionality.

---

# File Creation

Do not create new files unless necessary.

Before creating a file:

- Check whether an existing file can be extended.
- Check whether functionality belongs to an existing component.

Avoid unnecessary file creation.

---

# DRY

Never duplicate:

- Business Logic
- Validation
- Mapping
- Database Queries
- Helper Methods
- Constants
- Configuration

Extract reusable logic whenever duplication appears.

---

# KISS

Keep implementations simple.

Avoid unnecessary abstractions.

Avoid over engineering.

Choose the simplest production-ready solution.

---

# Framework First

Before writing custom code check whether the functionality already exists in:

- .NET
- ASP.NET Core
- EF Core
- MediatR
- FluentValidation
- Microsoft.Extensions
- Existing project code

Never reinvent the wheel.

---

# External Libraries

Before installing a NuGet package:

Verify whether:

- .NET already provides it.
- ASP.NET Core provides it.
- EF Core provides it.
- The project already includes a package that solves the problem.

Only introduce a dependency if it provides clear value.

Avoid dependency bloat.

---

# Modern Development

Always use:

- Latest stable .NET APIs
- Latest C# language features
- Microsoft's recommended practices

Avoid deprecated APIs.

Avoid obsolete design patterns.

---

# Clean Architecture

Respect the architecture.

API

↓

Application

↓

Domain

↓

Infrastructure

Rules:

- Domain never depends on Infrastructure.
- Application never depends directly on EF Core.
- Controllers never contain business logic.
- Infrastructure implements abstractions only.

---

# CQRS

Every write operation:

Command

↓

Validator

↓

Handler

Every read operation:

Query

↓

Handler

Never bypass MediatR.

Never place business logic inside Controllers.

---

# Business Logic

Business logic belongs only in:

- Application
- Domain

Never place business logic inside:

- Controllers
- Middleware
- Repositories
- Program.cs

---

# Repository Rules

Repositories are responsible only for persistence.

Never mix persistence with business rules.

Keep repositories focused.

---

# Validation

Use FluentValidation.

Validate every request.

Never duplicate validation rules.

Validation must not exist inside Controllers.

---

# Mapping

If AutoMapper or Mapster is available:

Use it.

Avoid manual mapping unless necessary.

---

# Extension Methods

When logic is:

- Stateless
- Reusable

Prefer Extension Methods over Helper classes.

---

# Dependency Injection

Always use Dependency Injection.

Prefer constructor injection.

Never instantiate services manually.

---

# Async

Use async/await everywhere appropriate.

Never use:

- .Result
- .Wait()

Always propagate CancellationToken.

---

# Database

Use EF Core.

Use Migrations.

Prefer LINQ.

Avoid raw SQL unless necessary.

Use:

- AsNoTracking() for read operations.
- Projection instead of loading full entities.
- Pagination for collections.

Prevent:

- N+1 Queries
- Over-fetching

---

# API

Follow REST conventions.

Return consistent response models.

Use proper HTTP status codes.

Never expose internal exceptions.

---

# Security

Validate all inputs.

Authorize protected endpoints.

Never trust client data.

Never hardcode:

- Passwords
- Tokens
- Secrets
- API Keys
- Connection Strings

---

# Logging

Use structured logging.

Log:

- Errors
- Warnings
- Important business events

Never log sensitive information.

---

# Error Handling

Use centralized exception handling.

Never swallow exceptions.

Return standardized API responses.

---

# Naming

Use meaningful names.

Avoid names like:

- Helper
- Manager
- Utils
- Data
- Temp
- Service1
- NewClass

Every name should clearly express its responsibility.

---

# Methods

Methods should:

- Be small.
- Have one responsibility.
- Be readable.
- Avoid deep nesting.

Extract private methods when appropriate.

---

# Classes

One responsibility per class.

Follow SOLID.

Avoid God Classes.

Prefer composition over inheritance.

---

# Performance

Before completing any feature verify:

- No duplicate queries
- No unnecessary allocations
- No unnecessary Includes
- Pagination exists where needed
- Queries are optimized

Do not optimize prematurely.

Optimize only when necessary.

---

# Refactoring

Refactor only when it improves:

- Readability
- Reusability
- Maintainability

Do not introduce breaking changes.

---

# Testing

Whenever business logic changes:

Consider adding or updating Unit Tests.

Prefer testing behavior over implementation.

---

# Documentation

Update documentation whenever:

- Public APIs change
- Business rules change
- Architecture changes
- Shared components are introduced

---

# Code Style

Follow Microsoft's C# Coding Conventions.

Use:

- File-scoped namespaces
- Nullable Reference Types
- Readonly fields where possible
- Records where appropriate
- Required members when appropriate
- Pattern Matching where appropriate

Avoid:

- Regions (unless necessary)
- Magic numbers
- Commented-out code
- Dead code

---

# Cross Feature Reuse

If another feature already solves part of the problem:

Reuse it.

Do not implement the same logic twice.

---

# Minimal Changes

Only modify files related to the requested task.

Never change unrelated files.

Avoid unnecessary refactoring.

---

# AI Behavior

Behave like a Senior Software Engineer.

Not like a code generator.

Always:

- Understand the codebase.
- Reuse existing code.
- Produce production-ready implementations.
- Prefer consistency over creativity.
- Keep solutions maintainable.

Never:

- Invent business rules.
- Generate placeholder implementations.
- Generate fake code.
- Leave TODO implementations.
- Duplicate functionality.
- Create unnecessary abstractions.
- Create unnecessary files.

---

# Self Review

Before considering a task complete verify:

✓ The specification has been followed.

✓ Existing code was reused whenever possible.

✓ No duplicate code exists.

✓ No duplicate validation exists.

✓ No duplicate mapping exists.

✓ No unnecessary files were created.

✓ No unnecessary NuGet packages were added.

✓ Clean Architecture is respected.

✓ CQRS is respected.

✓ SOLID is respected.

✓ DRY is respected.

✓ Naming is consistent.

✓ Logging is implemented where required.

✓ Error handling is correct.

✓ Code formatting is consistent.

✓ The implementation is production-ready.

---

# Final Principle

Every new line of code should improve the project.

If the existing code solves the problem:

Reuse it.

If the framework solves the problem:

Use it.

If the platform solves the problem:

Use it.

Write custom code only when it provides real value.

Always optimize for long-term maintainability.