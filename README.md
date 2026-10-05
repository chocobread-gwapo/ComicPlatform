# Catkoom

A webcomic reading and publishing platform — readers browse, follow, and comment on series; creators publish chapters on a schedule and get paid their share of ad revenue; admins moderate reports and manage bans. Built entirely in C# with Blazor Web App (Interactive Server), styled as an ink-on-paper take on the Webtoon layout.

For the full feature list, data model, and design history, see [`SUMMARY.md`](./SUMMARY.md).

## Tech Stack

- .NET 10, ASP.NET Core / Blazor Web App (Interactive Server)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity (accounts, roles, banning via lockout)
- Stripe (`Stripe.net`) for creator payouts via Stripe Connect
- Hand-written CSS (no framework) for the custom pages; Identity's scaffolded Account pages still use Bootstrap, re-themed to match

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for development) or a connection string to any SQL Server instance
- Visual Studio 2026 (or another IDE that supports .NET 10 and Blazor)
- A free [Stripe](https://dashboard.stripe.com/register) account, for a test-mode secret key (only needed for the creator payout flow — the rest of the site runs fine without it)

## Getting Started

1. **Clone the repository and open the solution** in Visual Studio.

2. **Set the database connection string.** In `appsettings.json` (or `appsettings.Development.json`), set `ConnectionStrings:DefaultConnection` to your SQL Server instance.

3. **Set the Stripe secret key as a user secret** — never commit this to source control. Right-click the project in Solution Explorer → *Manage User Secrets*, and add:
   ```json
   {
     "Stripe": {
       "SecretKey": "sk_test_..."
     }
   }
   ```
   Get your test key from the [Stripe Dashboard → API keys](https://dashboard.stripe.com/test/apikeys).

4. **Apply the database migrations.** In the Package Manager Console, with the project (not the solution) as the working directory:
   ```
   Update-Database
   ```

5. **Run the project.** On first run, `Program.cs` seeds the genre list and creates the `Admin` role automatically.

6. **Grant yourself admin access.** The seed logic in `Program.cs` currently looks for one specific email address to promote to the `Admin` role (`var adminUser = await userManager.FindByEmailAsync("...")`). Register an account with that email first, or update the email in `Program.cs` to your own before running.

## Project Structure

See the **Project Structure** table in [`SUMMARY.md`](./SUMMARY.md) for what each key file does. In short:

- `Models.cs` / `Data/` — entities, `ApplicationUser`, `ApplicationDbContext`
- `Components/Pages/` — reader, creator, and admin pages
- `Components/Layout/` — the app shell and nav
- `Components/Shared/` — reusable components (the logo, the report button)
- `Components/Account/` — Identity's scaffolded account pages, plus the custom claims factory and username backfill gate

## Known Limitations

- Image uploads go to local disk (`wwwroot/uploads/`), which won't survive most cloud hosting as-is — moving to Azure Blob Storage is a planned step before deployment, not yet done
- Ad placement and the creator revenue-split ledger aren't built yet — the Stripe Connect payout foundation is in place, but nothing feeds it real numbers yet
- Scheduled chapter publishing compares timestamps in UTC directly, with no timezone conversion — the actual go-live moment will be offset from your local wall clock if your server isn't UTC-aligned

## Status

Actively developed, not yet deployed.
