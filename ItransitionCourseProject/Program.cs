using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Filters;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ExceptionFilter>();
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(
        databaseConnectionString,
        postgres => postgres.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserCookieEvents>();
builder.Services.AddScoped<IUserSignInService, UserSignInService>();
builder.Services.AddScoped<IAuthServices, AuthServices>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IImageService, CloudinaryImageService>();
builder.Services.AddScoped<IAttributeProfileService, AttributeProfileService>();
builder.Services.AddScoped<IAttributeLibraryService, AttributeLibraryService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ICvService, CvService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IHomeService, HomeService>();

var authentication = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.EventsType = typeof(UserCookieEvents);
    })
    .AddCookie("External", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(options =>
    {
        options.SignInScheme = "External";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SaveTokens = true;
    });
}

var facebookClientId = builder.Configuration["Authentication:Facebook:ClientId"];
var facebookClientSecret = builder.Configuration["Authentication:Facebook:ClientSecret"];

if (!string.IsNullOrWhiteSpace(facebookClientId) &&
    !string.IsNullOrWhiteSpace(facebookClientSecret))
{
    authentication.AddFacebook(options =>
    {
        options.SignInScheme = "External";
        options.AppId = facebookClientId;
        options.AppSecret = facebookClientSecret;
        options.Scope.Add("email");
        options.Fields.Add("email");
        options.SaveTokens = true;
    });
}

builder.Services.AddAuthorization();
var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/status", () => Results.Ok(new
{
    application = "CV Management System",
    backend = "ready"
}));
app.MapGet("/health", async (DatabaseContext db, CancellationToken token) =>
    await db.Database.CanConnectAsync(token)
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapGet("/error", () => Results.Problem(
    title: "An unexpected server error occurred.",
    statusCode: StatusCodes.Status500InternalServerError));
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
