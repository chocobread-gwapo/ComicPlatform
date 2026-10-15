# Catkoom

A webcomic reading and publishing platform — readers browse, follow, and comment on series; creators publish chapters on a schedule, keep a profile with a feed of updates, and get paid their share of ad revenue; admins moderate reports and manage bans. Built entirely in C# with Blazor Web App (Interactive Server), styled as an ink-on-paper take on the Webtoon layout.

For the full feature list, data model, and design history, see [`SUMMARY.md`](./SUMMARY.md).

## Tech Stack

- .NET 10, ASP.NET Core / Blazor Web App (Interactive Server)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity (accounts, roles, banning via lockout), with optional Google and Facebook sign-in
- Stripe (`Stripe.net`) for creator payouts via Stripe Connect
- Hand-written CSS (no framework) for the custom pages; Identity's scaffolded Account pages are still Bootstrap underneath, with `app.css` overriding the defaults and a custom layout and sidebar to match the rest of the site

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for development) or a connection string to any SQL Server instance
- Visual Studio 2026 (or another IDE that supports .NET 10 and Blazor)
- A free [Stripe](https://dashboard.stripe.com/register) account, for a test-mode secret key (only needed for the creator payout flow — the rest of the site runs fine without it)
- Optional: a [Google Cloud](https://console.cloud.google.com) project and/or a [Meta for Developers](https://developers.facebook.com) app, if you want social sign-in. Each provider is enabled only when its credentials are present, so you can set up one, both, or neither

## Getting Started

1. **Clone the repository and open the solution** in Visual Studio. NuGet packages, including the Google and Facebook authentication packages, restore automatically.

2. **Set the database connection string.** In `appsettings.json` (or `appsettings.Development.json`), set `ConnectionStrings:DefaultConnection` to your SQL Server instance.

3. **Set your secrets as user secrets** — never commit these to source control. Right-click the project in Solution Explorer → *Manage User Secrets*, and add whichever of these you need:
   ```json
   {
     "Stripe": {
       "SecretKey": "sk_test_..."
     },
     "Authentication": {
       "Google": {
         "ClientId": "...",
         "ClientSecret": "..."
       },
       "Facebook": {
         "AppId": "...",
         "AppSecret": "..."
       }
     }
   }
   ```
   - Get the Stripe test key from the [Stripe Dashboard → API keys](https://dashboard.stripe.com/test/apikeys).
   - For **Google**, create OAuth credentials of type *Web application* in the Google Cloud Console and add `https://localhost:<port>/signin-google` as an authorized redirect URI. While the consent screen is unpublished, add your own account as a test user.
   - For **Facebook**, create an app with the Facebook Login product and add `https://localhost:<port>/signin-facebook` as a valid OAuth redirect URI. A new app starts in Development mode, where only people with a role on the app can sign in.
   - `<port>` is the HTTPS port in `Properties/launchSettings.json` (for example `7240`).
   - A provider is registered only when **both** of its values are present. Leave one out and it's simply skipped; nothing breaks.

4. **Apply the database migrations.** In the Package Manager Console, with the project (not the solution) as the working directory:
   ```
   Update-Database
   ```
   If you change the model, create a migration with `Add-Migration <Name>` before running `Update-Database` again. If `Update-Database` reports pending model changes, the model has changed since the last migration was created.

5. **Run the project.** On startup, `Program.cs` seeds the genre list and creates the `Admin` role automatically. The genre seed checks each name individually, so adding a genre to the list in `Program.cs` and restarting is enough to bring it into an existing database.

6. **Grant yourself admin access.** The seed logic in `Program.cs` looks for one specific email address to promote to the `Admin` role (`var adminUser = await userManager.FindByEmailAsync("...")`). Either update that email to your own, or register an account with it. The check only runs when the app **starts**, so if you register after the app is already running, restart it. Then sign out and back in, because role claims are written into your sign-in cookie at sign-in. The Admin link then appears in the account dropdown.

## Project Structure

See the **Project Structure** table in [`SUMMARY.md`](./SUMMARY.md) for what each key file does. In short:

- `Models.cs` / `Data/` — entities, `ApplicationUser`, `ApplicationDbContext`
- `Components/Pages/` — reader, creator profile, subscriptions/following, and admin pages
- `Components/Layout/` — the app shell and nav
- `Components/Shared/` — reusable components: the logo, the report button, the account tab bar, the tag input, the genre picker, and the creator feed's post and reply components
- `Components/Account/` — Identity's scaffolded account pages (restyled), the Manage layout and sidebar, the social sign-in callback, the custom claims factory, the username backfill gate, and the optional-field validation attributes

## Known Limitations

- Image uploads go to local disk (`wwwroot/uploads/`), which won't survive most cloud hosting as-is — moving to Azure Blob Storage is a planned step before deployment, not yet done
- Ad placement and the creator revenue-split ledger aren't built yet — the Stripe Connect payout foundation is in place, but nothing feeds it real numbers yet
- Scheduled chapter publishing compares timestamps in UTC directly, with no timezone conversion — the actual go-live moment will be offset from your local wall clock if your server isn't UTC-aligned
- Creator profile posts are text-only (the data model has room for an image, but there's no upload yet)
- Chapters, creator posts, and creator-post replies can't be reported — only comments and series can
- A Google sign-in whose verified email matches an existing account links to it automatically; a Facebook sign-in never does, and falls through to the registration form instead
- User Secrets only exist on your development machine. A deployed environment will need the Stripe, Google, and Facebook credentials supplied another way (environment variables or a key vault)

## Troubleshooting

- **The Admin link doesn't show up for your admin account.** Restart the app (the role is only assigned at startup, and only if the account already existed then), then sign out and back in.
- **`Update-Database` says the model has pending changes.** The model changed after the last migration was created. Run `Add-Migration <Name>`. If a migration has already been applied to the database, `Remove-Migration` will refuse; add a new follow-up migration instead.
- **"A second operation was started on this context instance."** Two things used the same `DbContext` at once. Components that load data alongside others should take an `IDbContextFactory<ApplicationDbContext>` and open their own context, as the existing ones do.
- **The app crashes at startup or on every request after adding social login.** Check that each provider has both of its values in User Secrets, or neither. Redirect URIs must match exactly, including the port.
- **LocalDB won't start, or the app says it can't open the database.** First check what SQL Server thinks exists: connect to the server (not the app database) and run `SELECT name, state_desc FROM sys.databases`. If the database is missing from the list but its `.mdf` and `.ldf` files are still on disk, the instance has lost its registration of them, not the data. Copy both files somewhere safe, then re-register them:
  ```sql
  CREATE DATABASE [YourDatabaseName]
      ON (FILENAME = 'C:\path\to\YourDatabaseName.mdf'),
         (FILENAME = 'C:\path\to\YourDatabaseName_log.ldf')
      FOR ATTACH;
  ```
  Then run `Update-Database` to catch up on any migrations. If the LocalDB instance itself won't start and there's nothing in it you need, `sqllocaldb delete` followed by `sqllocaldb create` rebuilds it.

## Status

Actively developed, not yet deployed.
