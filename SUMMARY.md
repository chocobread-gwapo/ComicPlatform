# Catkoom — Project Summary

*(Originally named ComicPlatform / CattoKom.)*

A webcomic reading and publishing platform, built to compete directly with Webtoon. Readers browse, follow, read, like, comment, and reply on series; creators write, upload, schedule, and manage their own (or co-authored) series; admins moderate the platform and handle reports and bans. Visually, the site follows a Webtoon-style layout (top nav, home rails, ranked lists) rendered in an ink-on-paper design language — sharp corners, bold borders, no drop shadows, Fraunces + Public Sans.

## Tech Stack

- **Language / Framework:** C# / ASP.NET Core, targeting .NET 10 (LTS)
- **IDE:** Visual Studio 2026
- **UI framework:** Blazor Web App, Interactive Server render mode — no separate JS frontend framework
- **Database:** SQL Server, via Entity Framework Core
- **Auth:** ASP.NET Core Identity (Individual Accounts), role-based authorization for the Admin area, lockout columns repurposed for user banning
- **Payments:** Stripe (`Stripe.net` NuGet package) — Stripe Connect (Express accounts) for creator payouts
- **Image storage:** Local disk (`wwwroot/uploads/`) — **not yet production-ready**; the plan is Azure Blob Storage before any real deployment
- **Styling:** Hand-written CSS using a shared custom property (token) system for theming (`--ink`, `--paper`, `--accent`, `--panel-bg`, `--muted-text`, `--border-soft`, `--danger`, `--hatch`), no CSS framework for the custom pages (Identity's scaffolded Account pages still lean on Bootstrap, now re-themed to use the same tokens). Fraunces (serif, headings) + Public Sans (body) from Google Fonts.
- **Theme toggle:** Entirely CSS-driven — the toggle's icons and knob react directly to the `data-theme` attribute on `<html>` via `html[data-theme="dark"]` selectors. No C# state is involved; clicking it is plain JavaScript (`window.toggleTheme()` in `App.razor`), which is what lets it work even on statically-rendered pages (see Notable Bugs Fixed).
- **Upload limit:** 30 MB per image, enforced both at the SignalR transport layer (`MaximumReceiveMessageSize`) and per-file in upload handlers

## Data Model

| Entity | Purpose |
|---|---|
| `ApplicationUser` | Extends Identity's `IdentityUser` with `DisplayName`, `Bio`, `AvatarUrl`, `CreatedAt`, `StripeConnectAccountId`, `StripeOnboardingComplete` |
| `Series` | A comic/webtoon title — title, synopsis, cover image, status (Ongoing/Completed/Hiatus), `CreatorId` |
| `Genre` | Tag like "Fantasy" or "Romance" — many-to-many with `Series` |
| `Chapter` | An episode within a `Series` — number, title, draft/published state, `PublishedAt`, `ViewCount` |
| `Page` | One image within a `Chapter`, in reading order |
| `Comment` | A reader comment on a `Chapter`. Threaded one level deep via `ParentCommentId`/`Replies` — a reply can't itself be replied to |
| `Follow` | Join entity: which readers follow which series; also tracks `LastViewedAt` for the "new chapter" badge |
| `Like` | Join entity: which readers liked which chapter |
| `SeriesAuthor` | Join entity: co-authors with edit rights on a series, alongside its original `CreatorId` |
| `Report` | A user-filed report against a `Comment` or a `Series`, with `TargetType`, `Reason` (enum: Spam/HarassmentOrHate/Spoilers/Copyright/Inappropriate/Other), `Status` (Pending/Resolved/Dismissed), and the reporter. `CommentId`/`SeriesId` use `ON DELETE SET NULL`/`RESTRICT` respectively — see Notable Bugs Fixed for why they're different |

User banning uses Identity's existing `LockoutEnd`/`LockoutEnabled` columns directly — no new entity needed.

## Features

### Reader-facing
- **Home (`/`)** — Webtoon-style rails: Trending & Popular (Trending/Popular tabs), Popular Series by Category, Newly Released, Daily (by weekday + Completed), More Stories from Creators; live search-as-you-type by title
- **Categories (`/categories`)** — full genre browsing with **multi-select** filtering and an Any/All match-mode toggle (OR vs AND across selected genres), plus Popular/Newest sort
- **Rankings (`/rankings`)** — full numbered Trending/All-Time Popular list (top 50), with the top 3 ranks visually emphasized
- **Series page (`/series/{id}`)** — synopsis, genre tags, creator byline, stats strip (views/followers/likes), full chapter list, Follow/Unfollow, Start Reading, Report
- **Reader (`/read/{seriesId}/{chapterNumber}`)** — vertical-scroll page images, in-reader episode list panel (jump to any chapter without leaving the reader), previous/next chapter cards, Like, threaded comments (post, reply one level deep, delete, report)
- **Following (`/following`)** — Updates/Recently-followed sort, "new chapter" cover badges, one-click Unfollow from the grid
- **My Comments (`/my-comments`)** — a reader's own comments across every series, newest first, linking back to context (capped at the most recent 100)
- **Dark/light theme toggle** — see Tech Stack; correct immediately on load and reacts instantly to clicks, static pages included

### Account & Identity
- **Registration** now requires a unique **username** (stored as `DisplayName`), checked for uniqueness on submit, shown everywhere instead of email/UserName
- **Username backfill gate** — any account that predates this feature (blank `DisplayName`) is redirected to `/Account/ChooseUsername` on next visit and can't proceed until they pick one; a `CustomUserClaimsPrincipalFactory` bakes `DisplayName` into the auth cookie as a claim so most page loads never need a database check for this at all
- **Account dropdown** — hovering (or keyboard-focusing) the username in the nav reveals Following, Comments, Account, Logout, pure CSS (`:hover`/`:focus-within`), no JavaScript

### Creator tools
- **Create/Edit series** — title, synopsis, status, genre selection (now also at creation time, not just Edit), cover image upload (now also at creation time)
- **Create/Edit chapter** — unified create/edit component; auto-computed chapter numbers; pages uploaded/previewed/reordered/removed; a dashed drop-zone that accepts drag-and-drop natively; Draft / Publish Now / Schedule as explicit pills instead of a checkbox, with a note that scheduling uses the server's UTC clock
- **My Series (`/creator/my-series`)** — portfolio stats strip (series/views/likes/followers), Recently-updated/Most-views sort, per-series thumbnail + stats row, Earnings link
- **Earnings (`/creator/earnings`)** — Stripe Connect Express onboarding; a creator connects a payout account through Stripe's own hosted flow (their bank/identity details never touch our database)

### Admin
- **Admin dashboard (`/admin`)** — platform-wide series list with search, status filter, thumbnails, per-series pending-report badges, permanent delete (inline-confirmed)
- **Reports (`/admin/reports`)** — Pending/Resolved/Dismissed tabs; Dismiss or Remove-content-and-resolve (which also resolves every other pending report on the same target)
- **Users (`/admin/users`)** — search, ban (1/7/30 days or permanent) built on Identity's lockout columns, Unban; Admin accounts and your own account can't be banned through this screen

## Notable Design Decisions & Trade-offs

- **Likes, not star ratings**
- **Single-level comment threading** — a reply can't itself be replied to, even though the schema (`ParentCommentId`) technically allows deeper nesting. Matches how YouTube/Instagram do it rather than full Reddit-style nesting
- **Ad-revenue-share monetization, not reader subscriptions or pay-per-chapter** — explicitly decided against a reader-paid model; revenue is meant to come from passive, non-popup ads and be split with creators proportional to view share. Ad placement itself can't go live until the site is deployed (ad networks review live content before approving an account), so only the creator-payout foundation (Stripe Connect) is built so far — the revenue ledger and actual ad units are not
- **Usernames are unique, display names are not a separate concept** — rather than adding a distinct `@handle` alongside `DisplayName`, `DisplayName` itself became the unique, required username, shown everywhere
- **Raw view counter, not deduplicated/unique visitors**
- **Simple up/down page reordering, not drag-and-drop** (for page order within a chapter — the upload drop-zone itself does accept drag-and-drop for adding files, which is a different thing)
- **Co-authors as an addition, not a replacement** — `Series.CreatorId` remains the credited "original creator"; `SeriesAuthor` is additive
- **No timezone conversion on scheduled publishing** — stored and compared as UTC directly; now called out explicitly in the Schedule UI itself, not just in this doc

## Notable Bugs Fixed Along the Way

- **Namespace mismatches** after moving files between folders in Solution Explorer
- **Cascade-delete cycle** on `Follow` — `Series → Creator` needed `DeleteBehavior.Restrict`
- **`@page` directive collision** with a loop variable named `page`
- **`AuthorizeView`/`EditForm` collision** on the implicit `context` parameter
- **Missing `RoleManager` registration** — needed `.AddRoles<IdentityRole>()` explicitly
- **Theme toggle, round 1** — the toggle's knob showed the wrong state on load because `MainLayout` read the theme cookie via the `HttpContext` cascading parameter, which Blazor nulls out once the interactive circuit connects (it only exists during the initial static render)
- **Theme toggle, round 2** — replacing that with a `CascadingValue` from `App.razor` seemed right but turned out not to reliably survive Blazor's static-to-interactive handoff either
- **Theme toggle, round 3 (actual fix)** — stopped using any C# state for the toggle's appearance at all. The page's `data-theme` attribute was always being set correctly; the toggle's CSS now reacts to that same attribute directly, so there's only one source of truth instead of two that could disagree. Clicking moved to plain JS for the same reason — it also fixed the toggle not working on Identity's Account/Manage pages, which render without an interactive circuit at all (see below)
- **Identity's Account/Manage pages are static-only** — they write auth cookies directly (email/password changes), which doesn't work reliably over a persistent Blazor circuit, so Microsoft's own scaffolding renders them without one. Any `@onclick`-based interactivity (like the old theme toggle) silently did nothing there; fixed by moving to plain-JS `onclick` instead, and their Bootstrap-default colors were never connected to the site's theme tokens at all (separate fix, in `app.css`)
- **`IdentityRevalidatingAuthenticationStateProvider` only checked the security stamp** — which `SetLockoutEndDateAsync` (what banning calls) never touches, so a banned user's already-open session wasn't cut off. Added an `IsLockedOutAsync` check; it's re-evaluated on the existing 30-minute `RevalidationInterval`, so banning is not instant, just bounded now instead of unbounded
- **`Report`'s cascade paths** — a first attempt at `SET NULL` on both `Report.CommentId` and `Report.SeriesId` failed to migrate: SQL Server treats `SET NULL` as a cascading action just like `CASCADE`, and `Series` could already reach `Report` two ways (directly, and via `Series → Chapters → Comments`), which SQL Server won't allow. Fixed by making `Report.SeriesId` use `Restrict` instead, with the two places that delete a series now explicitly clearing their direct reports first

## Project Structure (key files)

| File | Role |
|---|---|
| `Models.cs` | Core entity classes (`Series`, `Genre`, `Chapter`, `Page`, `Comment`, `Follow`, `Like`, `SeriesAuthor`, `Report` + its three enums) |
| `Data/ApplicationUser.cs` | Identity user extension, including Stripe Connect fields |
| `Data/ApplicationDbContext.cs` | EF Core context, `DbSet`s, relationship configuration |
| `Program.cs` | Service registration, SignalR message size config, Stripe API key config, custom claims factory registration, seed data |
| `Components/App.razor` | Root HTML document; server-side theme cookie read; `window.setTheme`/`window.toggleTheme` |
| `Components/Layout/MainLayout.razor` | App shell; theme toggle; username backfill gate check |
| `Components/Layout/NavMenu.razor` | Top nav; logo; account hover dropdown |
| `Components/Shared/Logo.razor` | Inline SVG logo, colors swap with theme via CSS custom properties |
| `Components/Shared/ReportButton.razor` | Reusable report-this-comment/report-this-series control |
| `Components/Account/CustomUserClaimsPrincipalFactory.cs` | Adds `DisplayName` as an auth claim at sign-in |
| `Components/Account/Pages/Register.razor` | Registration, now with required unique username |
| `Components/Account/Pages/ChooseUsername.razor` | Backfill prompt for pre-existing blank-username accounts |
| `Components/Pages/Home.razor` | Trending/Popular/Category/Newly-Released/Daily rails, search |
| `Components/Pages/Categories.razor` | Multi-select genre browsing |
| `Components/Pages/Rankings.razor` | Full ranked list |
| `Components/Pages/SeriesDetail.razor` | Series page |
| `Components/Pages/Read.razor` | Reader, likes, threaded comments |
| `Components/Pages/MyComments.razor` | A reader's own comment history |
| `Components/Pages/ChapterForm.razor` | Unified chapter create/edit |
| `Components/Pages/NewSeries.razor` / `EditSeries.razor` | Series create / edit + co-author management |
| `Components/Pages/MySeries.razor` | Creator dashboard |
| `Components/Pages/Earnings.razor` | Stripe Connect payout onboarding |
| `Components/Pages/Following.razor` | Followed series list |
| `Components/Pages/Admin.razor` | Platform-wide series moderation |
| `Components/Pages/AdminReports.razor` | Report review queue |
| `Components/Pages/AdminUsers.razor` | User search and banning |
| `Components/Account/IdentityRevalidatingAuthenticationStateProvider.cs` | Periodic session revalidation, now lockout-aware |
| `wwwroot/app.css` | Shared light/dark theme tokens, including `--logo-a`/`--logo-b` |

## Not Yet Started

- **Deployment** — moving off local disk storage to Azure Blob Storage, plus actual hosting; still deferred by choice
- **Ad placement** — can't go live until the site is deployed and approved by an ad network; the creator-payout foundation (Stripe Connect) is built, but there's nothing yet to actually earn
- **Revenue-split ledger** — the piece that takes a period's total ad revenue and divides it across creators by their share of total views. Unlike ad placement, this doesn't strictly need deployment to build and test
- **Avatar upload** — `ApplicationUser.AvatarUrl` exists and is used (comment avatars, etc.) but there's no UI yet for a user to actually set one
- **Full Account/Manage restyle** — currently just re-themed to use the site's color tokens; still plain Bootstrap layout, not the Fraunces/ink-on-paper treatment the rest of the site has
- **Broader reporting** — chapters themselves still aren't a reportable target, only comments and series
- **Multi-select genre filtering** is only on the Categories page — Home's category rail is still single-select, by choice, since it's a 12-item preview rather than the dedicated browsing tool
