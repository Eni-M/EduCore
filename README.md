# EduCore

[![CI](https://github.com/Eni-M/EduCore/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Eni-M/EduCore/actions/workflows/ci.yml?query=branch%3Amain)

A school management platform for a national education system — from the ministry down to a
single classroom. REST API in .NET 10, React front end, PostgreSQL.

**Live demo:** [educore.website](https://educore.website)

## What it does

- **Six roles, one hierarchy.** State admin → regional admin → school directorate →
  teachers and students, with guardians alongside. Each tier creates and manages the one below
  it, and sees only its own state, city or school.
- **School life.** Classes, subjects, curriculum, timetables and shifts, lessons, attendance,
  marks with grading and graduation rules, homework with submissions, a school calendar and
  notices.
- **Student records.** Schooling history, documents, medical notes and an academic history that
  follows the student when they change schools.
- **Communication.** Teacher–guardian messaging and a notification bell whose unread count is
  pushed live to the browser.
- **Email.** HTML emails in the product's own colours with a plain-text part alongside: a welcome
  on account creation, a notice on every password change, and a weekly digest for guardians of
  what changed for each child.
- **Statistics.** Enrollment, attendance, academic results, demographics, class capacity and
  at-risk students, filterable by city, zone, school and academic year. Figures from groups too
  small to stay anonymous are withheld.
- **Three languages.** Answers in Albanian, Serbian or English, chosen per request.
- **Role-aware dashboards** behind a single endpoint.

## Security

- **Two-step sign-in.** Passkeys (WebAuthn / FIDO2), an authenticator app (TOTP), or a one-time
  code sent by email as a fallback. Code attempts are charged atomically and capped per user.
- JWT authentication, role-based policies, CSRF tokens bound to the session, `__Host-` cookies.
- School isolation enforced twice: EF query filters on reads, a save interceptor on writes.
- **National identifiers** encrypted at rest with AES-256-GCM, and found by exact match through a
  keyed HMAC-SHA256 index, so a search never decrypts a stored value.
- Self-service password reset; signing out or changing a password ends the session.
- Immediate account revocation, with no wait for token expiry.
- Rate limiting per user, stricter on login and password reset.
- The API runs as a data-only database role; only migrations run as the schema owner.
- Audit trail, scheduled data retention, and a [DPIA draft](docs/DPIA-draft.md).

## Stack

| | |
| --- | --- |
| API | .NET 10 minimal APIs, EF Core, FluentValidation, AutoMapper |
| Database | PostgreSQL |
| Front end | React, TypeScript, Vite |
| Files & email | S3-compatible storage, MailKit |
| Observability | OpenTelemetry, Prometheus |
| Delivery | Docker Compose, nginx (HTTPS), GitHub Actions CI with pinned actions and Dependabot |

Over 300 automated tests cover the business rules, validation and statistics queries, and a
[k6 load test](loadtests/README.md) simulates a full school day. The committed
[OpenAPI document](openapi.json) is the contract with the front end.

## Documentation

- [Architecture](docs/ARCHITECTURE.md): how the system is put together, with a scale-out checklist
- [Developer setup](docs/DEV-SETUP.md): running it locally
- [Migrations](docs/EXPLICIT_MIGRATIONS.md): the database migration workflow
- [Optimisation plan](docs/OPTIMISATION-PLAN.md): review findings and what each would take
- [DPIA draft](docs/DPIA-draft.md): data protection impact assessment

## License

Copyright (c) 2026 Eni Mirena. All rights reserved. The source is published for viewing only.
No permission is granted to copy, modify, distribute or use it. See [LICENSE](LICENSE).
