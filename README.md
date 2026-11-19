# CMCS - Contract Monthly Claim System

An ASP.NET Core 8 MVC web app that lets contract lecturers submit monthly claims for the hours
they've worked. Programme coordinators and academic managers then review, approve or reject them.
An HR area adds statistics, lecturer management, an audit trail and PDF reports.

> Built for the PROG6212 Portfolio of Evidence at IIE MSA (2024). Individual project, revisited in
> 2026 to fix bugs, close security gaps and add tests.

## Features

**Lecturers**
- Submit a claim: module code, hours worked Ã— hourly rate, notes and a supporting document
  (PDF, DOCX or XLSX, up to 5 MB). The total is always calculated on the server.
- Claims within the auto-approval limit (R5,000 by default, configurable by HR) are approved
  instantly; larger ones wait for review.
- Submission rules: at most 4 claims a month, 24 hours between claims, and capped hours and rate.
- See their own claims, statuses and rejection reasons, and download their own documents.

**Programme coordinators and academic managers**
- Review pending claims, download supporting documents, approve claims, or reject them with a
  reason.
- Coordinators can approve claims up to R5,000; larger claims need an academic manager.
- HR dashboard: claim statistics, recent claims, per-lecturer totals and recent audit entries.
- Manage lecturers: edit details, activate or deactivate accounts. Deactivated lecturers can't sign in.
- Configure auto-approval (on/off and maximum amount). Every change is kept as history.
- Generate summary, detailed or per-department PDF reports for a date range (iText 7).
- Audit trail for approvals, rejections, configuration changes, lecturer changes and reports.

## Security

- ASP.NET Core Identity with role-based authorization (Lecturer, ProgrammeCoordinator,
  AcademicManager). Self-registration always creates a lecturer account.
- Password rules (8+ characters, mixed case, digit, symbol), lockout after 5 failed sign-ins, and
  HttpOnly, Secure cookies with sliding expiry.
- Anti-forgery tokens on every state-changing request, including the AJAX ones.
- Uploads are type- and size-checked, renamed to random file names and stored in `App_Data`
  outside the web root. They can only be downloaded through an authorised action: lecturers get
  their own documents only.
- Claim ownership and amounts are set on the server, never taken from the form.
- Errors are logged server-side; users only see friendly messages.

## Tech stack

ASP.NET Core 8 MVC Â· C# Â· Entity Framework Core 8 (SQL Server / LocalDB) Â· ASP.NET Core Identity Â·
Razor views with Tailwind/Bootstrap Â· iText 7 Â· NUnit, Moq and `WebApplicationFactory`

## Getting started

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and SQL Server
LocalDB (installed with Visual Studio). You can also point `ConnectionStrings:DefaultConnection` in
`appsettings.json` at any SQL Server instance.

```bash
git clone https://github.com/tjay-km/contract-monthly-claim-system.git
cd contract-monthly-claim-system
dotnet run --project CMCS.csproj
```

On first start the app applies the EF Core migrations and creates the three roles. In the
Development environment it also creates one demo account per role, using the password in
`appsettings.Development.json` (`DemoUsers:Password`):

| Role | Email |
|---|---|
| Lecturer | `lecturer@test.com` |
| Programme Coordinator | `coordinator@test.com` |
| Academic Manager | `manager@test.com` |

You can also register a new lecturer account from the login page.

## Tests

```bash
dotnet test CMCS.Tests.New/CMCS.Tests.csproj
```

The test suite has 53 tests:
- **Unit tests** for the approval rules, upload validation, the claim and account controllers, and
  the claim model.
- **Database tests** for the submission rules and auto-approval settings, run against in-memory
  SQLite.
- **End-to-end tests** that start the whole app in memory: pages render, protected pages redirect,
  uploads aren't publicly reachable, and a lecturer registers, logs in, submits a claim, sees it
  approved and downloads the document.

## Project structure

```
Controllers/     Account, Claim, Dashboard, HR, Home
Services/        claims, documents, validation, approval, auto-approval config, audit, reports
Models/          Claim, ClaimAudit, ClaimReport, ApplicationUser, view models, upload settings
Data/            ApplicationDbContext and role/demo-user seeding
Migrations/      EF Core migrations
Views/           Razor views per controller
CMCS.Tests.New/  NUnit unit, database and end-to-end tests
```

## Academic integrity

This was submitted as coursework for PROG6212 at IIE MSA. It's shared as a portfolio piece. Please
don't copy it for your own coursework.
