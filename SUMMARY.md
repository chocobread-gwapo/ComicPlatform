# Catkoom — Project Summary

*(Originally named ComicPlatform / CattoKom.)*

A webcomic reading and publishing platform, built to compete directly with Webtoon. Readers browse, follow, read, like, comment, and reply on series; creators write, upload, schedule, and manage their own (or co-authored) series, and keep a public profile with a feed of updates their readers can like and reply to; admins moderate the platform and handle reports and bans. Visually, the site follows a Webtoon-style layout (top nav, home rails, ranked lists) rendered in an ink-on-paper design language — sharp corners, bold borders, no drop shadows, Fraunces + Public Sans.

## Tech Stack

- **Language / Framework:** C# / ASP.NET Core, targeting .NET 10 (LTS)
- **IDE:** Visual Studio 2026
- **UI framework:** Blazor Web App, Interactive Server render mode — no separate JS frontend framework
- **Database:** SQL Server, via Entity Framework Core. Both `AddDbContext` and `AddDbContextFactory` (Scoped) are registered, each with `EnableRetryOnFailure()`. Components that run alongside others on the same render pass (the nav, Home, and every page written since) take an `IDbContextFactory` and open their own short-lived context instead of sharing the circuit's scoped one — see Notable Bugs Fixed
- **Auth:** ASP.NET Core Identity (Individual Accounts), role-based authorization for the Admin area, lockout columns repurposed for user banning
- **Social sign-in:** Google and Facebook, via `Microsoft.AspNetCore.Authentication.Google` / `.Facebook`. Credentials come from configuration (User Secrets in development): `Authentication:Google:ClientId` / `ClientSecret` and `Authentication:Facebook:AppId` / `AppSecret`. A provider is registered only when both of its values are present, so an unconfigured one never breaks the app. Callback paths are `/signin-google` and `/signin-facebook`
- **Payments:** Stripe (`Stripe.net` NuGet package) — Stripe Connect (Express accounts) for creator payouts
- **Image storage:** Local disk (`wwwroot/uploads/`) — **not yet production-ready**; the plan is Azure Blob Storage before any real deployment. Avatars go to `wwwroot/uploads/avatars/`
- **Styling:** Hand-written CSS using a shared custom property (token) system for theming (`--ink`, `--paper`, `--accent`, `--panel-bg`, `--muted-text`, `--border-soft`, `--danger`, `--hatch`), no CSS framework for the custom pages. Identity's scaffolded Account pages are still Bootstrap underneath, but `app.css` now overrides the Bootstrap defaults that clashed with the theme (primary buttons, links, form fields, disabled fields, the native file-picker button, input-group boxes), and the Account area has its own layout and sidebar in the same style. Fraunces (serif, headings) + Public Sans (body) from Google Fonts
- **Theme toggle:** Entirely CSS-driven — the toggle's icons and knob react directly to the `data-theme` attribute on `<html>` via `html[data-theme="dark"]` selectors. No C# state is involved; clicking it is plain JavaScript (`window.toggleTheme()` in `App.razor`), which is what lets it work even on statically-rendered pages (see Notable Bugs Fixed)
- **Upload limits:** 30 MB per page/cover image, enforced both at the SignalR transport layer (`MaximumReceiveMessageSize`) and per-file in upload handlers. Avatars are capped at 2 MB, JPG/PNG/WebP only, and the file's real signature is checked rather than trusting its name
- **Form validation:** two small custom attributes, `OptionalUrl` and `OptionalPhone`, that treat an empty value as valid (see Notable Bugs Fixed)

## Data Model

| Entity | Purpose |
|---|---|
| `ApplicationUser` | Extends Identity's `IdentityUser` with `DisplayName`, `Bio`, `AvatarUrl`, `InstagramUrl`, `TwitterUrl`, `PatreonUrl`, `WebsiteUrl`, `CreatedAt`, `StripeConnectAccountId`, `StripeOnboardingComplete` |
| `Series` | A comic/webtoon title — title, synopsis, cover image, status (Ongoing/Completed/Hiatus), `CreatorId`, an optional `MainGenreId` |
| `Genre` | Fixed, curated category like "Fantasy" or "Romance" — many-to-many with `Series`. Seeded with 16 genres at startup |
| `Tag` | Free-typed keyword a creator attaches to a series, up to 50 characters, unique on `Name` — many-to-many with `Series`, separate from `Genre` |
| `Chapter` | An episode within a `Series` — number, title, draft/published state, `PublishedAt`, `ViewCount` |
| `Page` | One image within a `Chapter`, in reading order |
| `Comment` | A reader comment on a `Chapter`. Threaded one level deep via `ParentCommentId`/`Replies` — a reply can't itself be replied to |
| `Follow` | Join entity: which readers follow which series; also tracks `LastViewedAt` for the "new chapter" badge |
| `CreatorFollow` | Join entity: which readers follow which creator. Composite key (`UserId`, `CreatorId`), both pointing at `ApplicationUser`, so both are `Restrict` |
| `CreatorPost` | An update a creator posts to their own profile feed — text (up to 2000 characters), an optional `ImageUrl` (no upload UI yet), timestamp |
| `CreatorPostComment` | A reply on a `CreatorPost`. Self-referencing through `ParentCommentId` with no depth limit — replies can nest as deep as the conversation goes |
| `CreatorPostLike` | Join entity: which readers liked which creator post. Simple on/off, composite key (`UserId`, `CreatorPostId`) |
| `Like` | Join entity: which readers liked which chapter |
| `SeriesAuthor` | Join entity: co-authors with edit rights on a series, alongside its original `CreatorId` |
| `Report` | A user-filed report against a `Comment` or a `Series`, with `TargetType`, `Reason` (enum: Spam/HarassmentOrHate/Spoilers/Copyright/Inappropriate/Other), `Status` (Pending/Resolved/Dismissed), and the reporter. `CommentId`/`SeriesId` use `ON DELETE SET NULL`/`RESTRICT` respectively — see Notable Bugs Fixed for why they're different |

User banning uses Identity's existing `LockoutEnd`/`LockoutEnabled` columns directly — no new entity needed.

Seeded genres: Action, Romance, Comedy, Fantasy, Drama, Horror, Sci-Fi, Slice of Life, Thriller, Mystery, Romance Fantasy, Action Fantasy, BL, GL, LGBTQ+, Gaming. The seed checks each name individually, so adding a name to the list in `Program.cs` and restarting is enough to bring it into an existing database.

## Features

### Reader-facing
- **Home (`/`)** — Webtoon-style rails: Trending & Popular (Trending/Popular tabs), Popular Series by Category, Newly Released, Daily (by weekday + Completed), More Stories from Creators; live search-as-you-type that matches a series' title or any of its tags
- **Categories (`/categories`)** — full genre browsing with **multi-select** filtering and an Any/All match-mode toggle (OR vs AND across selected genres), plus Popular/Newest sort. Arriving with `?tag=name` (from a tag on a series page) switches it into a tag view instead: the genre picker is replaced by a single clearable chip, and the same results grid shows every series with that tag
- **Rankings (`/rankings`)** — full numbered Trending/All-Time Popular list (top 50), with the top 3 ranks visually emphasized
- **Series page (`/series/{id}`)** — synopsis, genre tags, creator byline (avatar and name link to the creator's profile, with a Follow creator toggle on anyone's series but your own), tags as `#tag` links into the Categories tag view, stats strip (views/followers/likes), full chapter list, Follow/Unfollow, Start Reading, Report
- **Reader (`/read/{seriesId}/{chapterNumber}`)** — vertical-scroll page images, in-reader episode list panel (jump to any chapter without leaving the reader), previous/next chapter cards, Like, threaded comments (post, reply one level deep, delete, report)
- **Creator profile (`/creator/{id}`)** — a centered header (avatar, name, bio, whichever social links are set, series and follower counts, Follow button — or an Edit profile link on your own page), a horizontal row of the creator's series, and their feed. Creators post text updates from the composer at the top of their own feed and can delete their own posts. Readers like posts and reply with Reddit-style threads: reply to the post or to any reply at any depth, each level indented under its parent, with a collapse/expand toggle that folds a sub-thread to a reply count. Deleting a reply removes its whole sub-thread
- **Account area tabs** — Subscriptions / Following / Comments / Account, one shared tab bar rendered across all four and across every page under Account/Manage
- **Subscriptions (`/subscriptions`)** — the series a reader follows, as Webtoon-style rows (cover thumbnail, title, creator, status, last updated), Updates/Recently-followed sort, "new chapter" badges, one-click Unfollow
- **Following (`/following`)** — the creators a reader follows: avatar, name, series count, follower count, Unfollow; each row links to the creator's profile
- **My Comments (`/my-comments`)** — a reader's own comments across every series, newest first, linking back to context (capped at the most recent 100)
- **Dark/light theme toggle** — see Tech Stack; correct immediately on load and reacts instantly to clicks, static pages included

### Account & Identity
- **Registration** requires a unique **username** (stored as `DisplayName`), checked for uniqueness on submit, shown everywhere instead of email/UserName
- **Username backfill gate** — any account with a blank `DisplayName` (including a brand-new account created through social sign-in) is redirected to `/Account/ChooseUsername` on next visit and can't proceed until they pick one; a `CustomUserClaimsPrincipalFactory` bakes `DisplayName` into the auth cookie as a claim so most page loads never need a database check for this at all. That factory inherits the two-type-parameter `UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>`, so role claims are in the cookie too
- **Social sign-in** — Google and Facebook buttons appear on the Login page for whichever providers are configured. When a Google sign-in isn't linked to anyone yet, the callback looks for an existing account with the same email; if Google itself reports that email as verified, it links the Google login to that account and signs straight in, and only otherwise falls through to the usual "create an account" form. Facebook doesn't expose a verified-email claim in this setup, so it never auto-links and always falls through
- **Profile settings (`/Account/Manage`)** — profile picture upload/remove, bio (up to 500 characters), Instagram/Twitter/Patreon/Website links, phone number. Everything except the picture is optional and labeled as such; the username is shown read-only. Everything under Account/Manage renders statically (no Blazor circuit), so the picture upload is a plain multipart form post
- **Account area layout** — Identity's Manage pages share one restyled layout and sidebar (Profile, Email, Password, Two-factor, Passkeys, Personal data), under the shared tab bar
- **Account dropdown** — hovering (or keyboard-focusing) the username in the nav, which shows the user's avatar beside it, reveals Subscriptions, Following, Comments, Admin (admins only), Account, Logout; pure CSS (`:hover`/`:focus-within`), no JavaScript. The top nav itself is just Home, Categories, Rankings

### Creator tools
- **Create/Edit series** — title, synopsis, status, cover image upload, genres and tags. Genres use a pill picker capped at 3, with an optional **Main Genre** dropdown limited to whichever genres are selected (deselecting the main one clears it). Tags use a type-and-press-Enter chip input: up to 10 tags, 50 characters each, duplicates ignored, and an existing tag's name is reused case-insensitively rather than creating a near-duplicate
- **Create/Edit chapter** — unified create/edit component; auto-computed chapter numbers; pages uploaded/previewed/reordered/removed; a dashed drop-zone that accepts drag-and-drop natively; Draft / Publish Now / Schedule as explicit pills instead of a checkbox, with a note that scheduling uses the server's UTC clock
- **My Series (`/creator/my-series`)** — portfolio stats strip (series/views/likes/followers), Recently-updated/Most-views sort, per-series thumbnail + stats row, Earnings link
- **Earnings (`/creator/earnings`)** — Stripe Connect Express onboarding; a creator connects a payout account through Stripe's own hosted flow (their bank/identity details never touch our database)

### Admin
- **Admin dashboard (`/admin`)** — platform-wide series list with search, status filter, thumbnails, per-series pending-report badges, permanent delete (inline-confirmed). Reached from the account dropdown, for accounts in the `Admin` role
- **Reports (`/admin/reports`)** — Pending/Resolved/Dismissed tabs; Dismiss or Remove-content-and-resolve (which also resolves every other pending report on the same target)
- **Users (`/admin/users`)** — search, ban (1/7/30 days or permanent) built on Identity's lockout columns, Unban; Admin accounts and your own account can't be banned through this screen

## Notable Design Decisions & Trade-offs

- **Likes, not star ratings** — and the same simple on/off like on creator posts, not a multi-emoji reaction picker
- **Single-level comment threading on chapters** — a reply can't itself be replied to, even though the schema (`ParentCommentId`) technically allows deeper nesting. Matches how YouTube/Instagram do it
- **Reddit-style nesting on creator posts, deliberately different** — the creator feed is a conversation space rather than a comment list under a chapter, so replies nest to any depth with a collapse toggle. The two systems use separate entities and stay separate
- **Creator posts are their own entities, not an extension of `Comment`** — `Comment` is Chapter-specific and already holds live data, so the feed was added as new tables only, leaving the existing one untouched. The trade-off is that creator-post replies aren't wired into the Report system yet
- **Genres and tags are different things** — genres are a small fixed list chosen from a picker and used for browsing; tags are free-typed and unbounded, so they're reached by clicking one on a series page rather than by a list of every tag in existence
- **Main Genre is its own optional relationship** — a single reference from `Series` to `Genre`, separate from the many-to-many collection, set to null (not blocked) if that genre is ever deleted
- **Webtoon's naming for the account tabs** — "Subscriptions" is followed series and "Following" is followed creators, matching Webtoon, which meant renaming the old Following page
- **Creator profile merges two references** — Webtoon's centered header and series row, Tapas' post feed
- **Sharp-cornered pills, not capsules** — a reference design used fully rounded pills for genre picking; the behavior was copied, the shape wasn't, so the picker matches every other pill on the site
- **New entities keep `Restrict` on every direct reference to `ApplicationUser`** — `CreatorFollow` points at users twice, and SQL Server won't allow multiple cascade paths into one table, so none of the user references cascade
- **Account pages are restyled by overriding Bootstrap in `app.css`**, not by rewriting Identity's scaffolded pages
- **Social providers are optional, and account linking is conservative** — a provider is registered only when its credentials exist, and a login is linked to an existing account by email only when the provider vouches the email is verified
- **Ad-revenue-share monetization, not reader subscriptions or pay-per-chapter** — explicitly decided against a reader-paid model; revenue is meant to come from passive, non-popup ads and be split with creators proportional to view share. Ad placement itself can't go live until the site is deployed (ad networks review live content before approving an account), so only the creator-payout foundation (Stripe Connect) is built so far — the revenue ledger and actual ad units are not
- **Usernames are unique, display names are not a separate concept** — rather than adding a distinct `@handle` alongside `DisplayName`, `DisplayName` itself became the unique, required username, shown everywhere. Under the hood Identity's own `UserName` is still the email
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
- **Identity's Account/Manage pages are static-only** — they write auth cookies directly (email/password changes), which doesn't work reliably over a persistent Blazor circuit, so Microsoft's own scaffolding renders them without one. Any `@onclick`-based interactivity (like the old theme toggle) silently did nothing there; fixed by moving to plain-JS `onclick` instead. Their Bootstrap defaults were later overridden in `app.css` and the area restyled
- **`IdentityRevalidatingAuthenticationStateProvider` only checked the security stamp** — which `SetLockoutEndDateAsync` (what banning calls) never touches, so a banned user's already-open session wasn't cut off. Added an `IsLockedOutAsync` check; it's re-evaluated on the existing 30-minute `RevalidationInterval`, so banning is not instant, just bounded now instead of unbounded
- **`Report`'s cascade paths** — a first attempt at `SET NULL` on both `Report.CommentId` and `Report.SeriesId` failed to migrate: SQL Server treats `SET NULL` as a cascading action just like `CASCADE`, and `Series` could already reach `Report` two ways (directly, and via `Series → Chapters → Comments`), which SQL Server won't allow. Fixed by making `Report.SeriesId` use `Restrict` instead, with the two places that delete a series now explicitly clearing their direct reports first
- **"A second operation was started on this context instance"** — every component in a Blazor Server circuit shares one scoped `DbContext`, and the Home rail load collided with other work running at the same moment. Fixed by registering `IDbContextFactory` and giving those components their own short-lived context. The nav does the same for its avatar lookup, since it wraps every page and initializes alongside whatever page it's wrapping
- **The Admin link never appeared, even for a real admin** — the custom claims factory inherited the one-type-parameter `UserClaimsPrincipalFactory`, which never writes role claims into the cookie, so `AuthorizeView Roles="Admin"` had nothing to match even though the database was correct. Fixed by inheriting the two-parameter version (and signing in again, since the claim is only written at sign-in)
- **A string component parameter silently received literal text** — `CurrentUserId="_currentUserId"` without an `@` passes the characters `_currentUserId`, not the field's value, because string-typed parameters take their text literally. It surfaced as a foreign-key failure when a like tried to insert that text as a user id. Non-string parameters happen to infer an expression, which is why other parameters had worked; component attributes that reference code now always carry the `@`
- **A background color on Bootstrap's floating label made stray white boxes** — the `.form-floating` label spans the whole input and is scaled down when floated, so coloring it left an oversized fragment behind. Removed; Bootstrap's default label rendering was fine
- **`[Url]` and `[Phone]` reject an empty string** — they only skip validation for `null`, and a static form post submits `""` for an untouched field, so optional fields failed with "not a valid URL". Replaced with `OptionalUrl`/`OptionalPhone`, which treat empty as valid and still catch malformed input (`OptionalPhone` delegates the real format check to the built-in `PhoneAttribute`)
- **Migrations generated before the model was final** — the first `AddTags` migration was created before a later unique index was added to the model, so `Update-Database` raised `PendingModelChangesWarning`, and `Remove-Migration` refused because it had already been applied. Fixed with a small follow-up migration rather than redoing the first. The same pattern applies any time a migration is applied and the model then changes
- **Deleting a reply that had replies** — `ParentCommentId` is `Restrict`, so the database refuses to delete a comment while others point at it. Deleting a creator-post reply now collects its whole sub-thread and removes it in one save
- **Social sign-in wiring** — `.AddIdentityCookies()` returns a narrower builder than the provider methods extend, so they couldn't be chained after it (the builder is now kept in a variable). An unconfigured Facebook provider threw on every request because its options validate `AppId` at startup, so providers now register only when credentials exist. Google's response includes `email_verified`, but the handler doesn't map it to a claim by default, so the account-linking check found nothing until it was mapped explicitly

## Project Structure (key files)

| File | Role |
|---|---|
| `Models.cs` | Core entity classes (`Series`, `Genre`, `Tag`, `Chapter`, `Page`, `Comment`, `Follow`, `CreatorFollow`, `CreatorPost`, `CreatorPostComment`, `CreatorPostLike`, `Like`, `SeriesAuthor`, `Report` + its three enums) |
| `Data/ApplicationUser.cs` | Identity user extension: display name, bio, avatar, social links, Stripe Connect fields |
| `Data/ApplicationDbContext.cs` | EF Core context, `DbSet`s, relationship configuration |
| `Program.cs` | Service registration (including both `DbContext` registrations and the social providers), SignalR message size config, Stripe API key config, custom claims factory registration, seed data (genres, `Admin` role) |
| `Components/App.razor` | Root HTML document; server-side theme cookie read; `window.setTheme`/`window.toggleTheme` |
| `Components/Layout/MainLayout.razor` | App shell; theme toggle; username backfill gate check |
| `Components/Layout/NavMenu.razor` | Top nav; logo; account hover dropdown with the user's avatar |
| `Components/Shared/Logo.razor` | Inline SVG logo, colors swap with theme via CSS custom properties |
| `Components/Shared/ReportButton.razor` | Reusable report-this-comment/report-this-series control |
| `Components/Shared/AccountTabs.razor` | The Subscriptions / Following / Comments / Account tab bar |
| `Components/Shared/TagInput.razor` | Chip-style tag entry (Enter to add, × to remove, caps on count and length) |
| `Components/Shared/GenrePicker.razor` | Capped pill genre picker plus the Main Genre dropdown |
| `Components/Shared/CreatorFeedModels.cs` | View models passed between the creator profile and its post components |
| `Components/Shared/CreatorPostCard.razor` | One feed post: content, like, delete, reply composer |
| `Components/Shared/CreatorReplyThread.razor` | One reply that renders its own children by rendering itself — the recursive reply tree, collapse toggle, and subtree delete |
| `Components/Account/CustomUserClaimsPrincipalFactory.cs` | Adds `DisplayName` as an auth claim at sign-in; inherits the role-aware factory |
| `Components/Account/OptionalUrlAttribute.cs` / `OptionalPhoneAttribute.cs` | Validation that treats an empty value as valid |
| `Components/Account/Shared/ManageLayout.razor` / `ManageNavMenu.razor` | Layout and sidebar shared by every Account/Manage page; also loads the fonts for them |
| `Components/Account/Pages/Register.razor` | Registration, with required unique username |
| `Components/Account/Pages/ChooseUsername.razor` | Backfill prompt for accounts with a blank username |
| `Components/Account/Pages/ExternalLogin.razor` | Social sign-in callback, including the link-by-verified-email step |
| `Components/Account/Pages/Manage/Index.razor` | Profile settings: avatar, bio, social links, phone |
| `Components/Pages/Home.razor` | Trending/Popular/Category/Newly-Released/Daily rails, search by title or tag |
| `Components/Pages/Categories.razor` | Multi-select genre browsing, plus the tag view |
| `Components/Pages/Rankings.razor` | Full ranked list |
| `Components/Pages/SeriesDetail.razor` | Series page |
| `Components/Pages/Read.razor` | Reader, likes, threaded comments |
| `Components/Pages/CreatorProfile.razor` | Creator profile: header, series row, feed |
| `Components/Pages/Subscriptions.razor` | Followed series list |
| `Components/Pages/Following.razor` | Followed creators list |
| `Components/Pages/MyComments.razor` | A reader's own comment history |
| `Components/Pages/ChapterForm.razor` | Unified chapter create/edit |
| `Components/Pages/NewSeries.razor` / `EditSeries.razor` | Series create / edit + co-author management, genres, tags |
| `Components/Pages/MySeries.razor` | Creator dashboard |
| `Components/Pages/Earnings.razor` | Stripe Connect payout onboarding |
| `Components/Pages/Admin.razor` | Platform-wide series moderation |
| `Components/Pages/AdminReports.razor` | Report review queue |
| `Components/Pages/AdminUsers.razor` | User search and banning |
| `Components/Account/IdentityRevalidatingAuthenticationStateProvider.cs` | Periodic session revalidation, lockout-aware |
| `wwwroot/app.css` | Shared light/dark theme tokens (including `--logo-a`/`--logo-b`) and the overrides for Bootstrap's defaults |

## Not Yet Started

- **Deployment** — moving off local disk storage to Azure Blob Storage, plus actual hosting; still deferred by choice. User Secrets are a development tool only, so the Stripe, Google, and Facebook credentials will need a production home (environment variables or a key vault) at that point
- **Ad placement** — can't go live until the site is deployed and approved by an ad network; the creator-payout foundation (Stripe Connect) is built, but there's nothing yet to actually earn
- **Revenue-split ledger** — the piece that takes a period's total ad revenue and divides it across creators by their share of total views. Unlike ad placement, this doesn't strictly need deployment to build and test
- **Broader reporting** — chapters aren't a reportable target, and neither are creator posts or their replies; only comments and series are
- **Images on creator posts** — `CreatorPost.ImageUrl` exists and renders if set, but the composer is text-only and has no upload
- **Main Genre isn't shown anywhere yet** — it's saved and editable, but the series page lists genres without singling it out
- **Facebook account linking** — a Facebook sign-in never links to an existing account by email, since no verified-email claim comes through. Whether to trust Facebook's email is an open decision
- **Multi-select genre filtering** is only on the Categories page — Home's category rail is still single-select, by choice, since it's a 12-item preview rather than the dedicated browsing tool
