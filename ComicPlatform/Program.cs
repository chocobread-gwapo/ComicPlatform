using ComicPlatform.Components;
using ComicPlatform.Components.Account;
using ComicPlatform.Data;
using ComicPlatform.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

// Lets components create their own short-lived DbContext (avoids "second operation started" errors in Blazor Server)
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()), ServiceLifetime.Scoped);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomUserClaimsPrincipalFactory>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 35 * 1024 * 1024; // headroom above the 30 MB per-image limit
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Per-name check instead of "only if the table is empty" — this way adding a
    // new name to this list and restarting is enough to bring it into an existing
    // database too, not just a brand-new one.
    var genreNames = new[]
    {
        "Action", "Romance", "Comedy", "Fantasy", "Drama", "Horror", "Sci-Fi",
        "Slice of Life", "Thriller", "Mystery",
        "Romance Fantasy", "Action Fantasy", "BL", "GL", "LGBTQ+", "Gaming"
    };

    var existingGenreNames = db.Genres.Select(g => g.Name).ToHashSet();

    foreach (var name in genreNames)
    {
        if (!existingGenreNames.Contains(name))
        {
            db.Genres.Add(new Genre { Name = name });
        }
    }

    await db.SaveChangesAsync();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var firstUser = db.Users.FirstOrDefault();

    if (firstUser is not null && !db.Series.Any())
    {
        db.Series.Add(new Series
        {
            Title = "The Last Signal",
            Synopsis = "Sample series for testing the reader page.",
            CreatorId = firstUser.Id,
            Chapters = new List<Chapter>
            {
                new Chapter
                {
                    Number = 1,
                    Title = "Pilot",
                    IsDraft = false,
                    PublishedAt = DateTime.UtcNow,
                    Pages = new List<Page>
                    {
                        new Page { PageNumber = 1, ImageUrl = "https://placehold.co/800x1200" },
                        new Page { PageNumber = 2, ImageUrl = "https://placehold.co/800x1200" },
                        new Page { PageNumber = 3, ImageUrl = "https://placehold.co/800x1200" },
                    }
                }
            }
        });
        db.SaveChanges();
    }
}

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    var adminUser = await userManager.FindByEmailAsync("jozuajimenez612@gmail.com");
    if (adminUser is not null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();