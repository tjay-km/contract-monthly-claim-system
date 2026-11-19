# CMCS - Contract Monthly Claim System

An ASP.NET Core 8 MVC web app that lets contract lecturers submit monthly claims for the hours
they've worked. Programme coordinators and academic managers then review, approve or reject them.
An HR area adds statistics, lecturer management, an audit trail and PDF reports.

> Built for the PROG6212 Portfolio of Evidence at IIE MSA (2024). Individual project.

## Features

**Lecturers**
- Submit a claim: module code, hours worked × hourly rate (total calculated automatically), notes and
  a supporting document upload.
- Claims up to R5,000 are auto-approved on submission; larger ones wait for review.
- See their claims and each claim's status on a dashboard.

**Programme coordinators and academic managers**
- Review pending claims, download supporting documents, and approve or reject claims with a
  reason.
- HR dashboard: claim statistics, recent claims, per-lecturer totals and recent audit entries.
- Manage lecturers: edit details, activate or deactivate, delete.
- Configure the auto-approval rule (on/off and maximum amount).
- Generate summary, detailed or per-department reports for a date range as PDFs (iText 7).
- Audit trail of approvals, configuration changes and lecturer management.

**Security**
- ASP.NET Core Identity with role-based authorization (Lecturer, ProgrammeCoordinator,
  AcademicManager) and permission claims per role.
- Password complexity rules, account lockout after 5 failed logins, HttpOnly cookies with sliding
  expiry.
- Anti-forgery tokens on form posts; server-side validation with data annotations.

## Tech stack

ASP.NET Core 8 MVC · C# · Entity Framework Core 8 (SQL Server / LocalDB) · ASP.NET Core Identity ·
Razor views + Tailwind/Bootstrap styling · iText 7 · NUnit + Moq

## Getting started

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and SQL Server
LocalDB (installed with Visual Studio). You can also point `ConnectionStrings:DefaultConnection` in
`appsettings.json` at any SQL Server instance.

```bash
git clone https://github.com/tjay-km/<repo-name>.git
cd <repo-name>
dotnet run --project CMCS.csproj
```

On first start the app applies the EF Core migrations and seeds the three roles plus one demo account
per role:

| Role | Email |
|---|---|
| Lecturer | `lecturer@test.com` |
| Programme Coordinator | `coordinator@test.com` |
| Academic Manager | `manager@test.com` |

The demo password is set in `Data/SeedData.cs`. You can also register a new account from the
login page.

Run the tests with:

```bash
dotnet test CMCS.Tests.New/CMCS.Tests.csproj
```

## Project structure

```
Controllers/   Account, Claim, Dashboard, HR, Home
Services/      claims, documents, validation, approval, audit, reports, auto-approval config
Models/        Claim, ClaimAudit, ClaimReport, ApplicationUser, view models
Data/          ApplicationDbContext and role/user seeding
Migrations/    EF Core migrations
Views/         Razor views per controller
CMCS.Tests.New/  NUnit tests for the claim model, services and controller
```

## Academic integrity

This was submitted as coursework for PROG6212 at IIE MSA. It's shared as a portfolio piece. Please
don't copy it for your own coursework.
