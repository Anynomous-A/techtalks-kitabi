using Kitabi.Endpoints;
using Kitabi.Data;
using Kitabi.Extensions;
using Kitabi.Interfaces;
using Kitabi.Models;
using Microsoft.OpenApi.Models;
using Kitabi.Policies;
using Kitabi.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Kitabi");
builder.Services.AddSqlServer<ApplicationDbContext>(connectionString);




// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddAppPolicies();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskManagerApp",
        Version = "v1"
    });

    // Add JWT Bearer auth
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token.\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.Configure<ImageUploadOptions>(builder.Configuration.GetSection("ImageUpload"));
builder.Services.AddSingleton<IImageStorage, ImageStorage>();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 60 * 1024 * 1024; // 60 MB
});


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.Users.Any(u => u.UserId == 1))
    {
        db.Users.Add(new User
        {
            Name = "Nizar",
            Email = "nizar@example.com",
            PasswordHash = "dummyhash",   // any string works for testing
            Phone = "123456789",
            University = "AUB",
            Listings = new List<Listing>() // initialize empty list
        });
        db.SaveChanges();

    }
}

app.UseHttpsRedirection();
//app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
        {
            ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Context.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
        }
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();


app.MigrateDb();
app.MapBookEndpoints();
app.MapImageEndpoints();
app.MapListingEndpoints();
app.MapCommunicationEndpoints();
app.MapAuthEndpoints();
app.Use(async (context, next) =>
{
    // Only sanitize JSON requests
    if (context.Request.ContentType != null &&
        context.Request.ContentType.Contains("application/json"))
    {
        // Enable buffering so we can read the body
        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (!string.IsNullOrWhiteSpace(body))
        {
            // Example: strip HTML globally
            body = System.Text.RegularExpressions.Regex.Replace(body, "<.*?>", string.Empty);

            // You could also trim strings here if needed
        }
    }

    await next();
});

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 400 &&
        context.Items.ContainsKey("errors"))
    {
        var errors = context.Items["errors"];
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Validation failed",
            errors
        });
    }
});

app.Run();
