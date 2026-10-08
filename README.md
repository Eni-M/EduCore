# EduCore

[![CI](https://github.com/Eni-M/EduCore/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Eni-M/EduCore/actions/workflows/ci.yml?query=branch%3Amain)

A school management platform for a national education system — from the ministry down to a
single classroom. REST API in .NET 10, React front end, PostgreSQL.

**Live demo:** [edu-core-front.vercel.app](https://edu-core-front.vercel.app)

## What it does

- **Six roles, one hierarchy.** State admin → regional admin → school directorate →
  teachers and students, with guardians alongside. Each tier creates and manages the one below
  it, and sees only its own state, city or school.
- **School life.** Classes, subjects, curriculum, timetables and shifts, lessons, attendance,
  marks, homework with submissions, a school calendar and notices.
- **Student records.** Schooling history, documents, medical notes and academic history.
- **Communication.** Teacher–guardian messaging and in-app notifications.
- **Statistics.** Enrollment, attendance, academic results, demographics, class capacity and
  at-risk students, filterable by city, zone, school and academic year.
- **Role-aware dashboards** behind a single endpoint.

## Security

- JWT authentication, role-based policies, double-submit CSRF protection.
- School isolation enforced twice: EF query filters on reads, a save interceptor on writes.
- National identifiers encrypted at rest (AES-256), still searchable by exact match.
- Rate limiting per user, stricter on login and password reset.
- Immediate account revocation, with no wait for token expiry.

## Stack

| | |
| --- | --- |
| API | .NET 10 minimal APIs, EF Core, FluentValidation, AutoMapper |
| Database | PostgreSQL |
| Front end | React, TypeScript, Vite |
| Files & email | S3-compatible storage, MailKit |
| Observability | OpenTelemetry, Prometheus |
| Delivery | Docker Compose, nginx (HTTPS), GitHub Actions CI |

435 automated tests cover the business rules, validation and statistics queries.

## Documentation

- [Architecture](docs/ARCHITECTURE.md): how the system is put together
- [Developer setup](docs/DEV-SETUP.md): running it locally
- [Migrations](docs/EXPLICIT_MIGRATIONS.md): the database migration workflow

## License

Copyright (c) 2026 Eni Mirena. All rights reserved. The source is published for viewing only.
No permission is granted to copy, modify, distribute or use it. See [LICENSE](LICENSE).
