using Kitabi.Endpoints;
using Kitabi.Data;
using Kitabi.Extensions;
using Kitabi.Interfaces;
using Kitabi.Models;
using Microsoft.OpenApi.Models;
using Kitabi.Policies;
using Kitabi.Services;
using Serilog;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.File(
        "logs/kitabi-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}")
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Kitabi");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("https://techtalks-kitabi-3.onrender.com",   // Render domain
                           "http://localhost:5000",
                           "https://localhost:7004",
                           "http://localhost:5500",
                           "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

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
builder.Services.AddAppRateLimiting();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();
builder.Host.UseSerilog();


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
app.MigrateDb();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!db.Users.Any())
    {
        var user1 = new User
        {
            Name = "Nizar",
            Email = "nizar@example.com",
            PasswordHash = "dummyhash",
            Phone = "123456789",
            University = "AUB",
            Listings = new List<Listing>()
        };

        var user2 = new User
        {
            Name = "Sara",
            Email = "sara@example.com",
            PasswordHash = "dummyhash",
            Phone = "987654321",
            University = "LAU",
            Listings = new List<Listing>()
        };

        var user3 = new User
        {
            Name = "Ali",
            Email = "ali@example.com",
            PasswordHash = "dummyhash",
            Phone = "555555555",
            University = "USJ",
            Listings = new List<Listing>()
        };

        var user4 = new User
        {
            Name = "Maya",
            Email = "maya@example.com",
            PasswordHash = "dummyhash",
            Phone = "111222333",
            University = "BAU",
            Listings = new List<Listing>()
        };

        var user5 = new User
        {
            Name = "Omar",
            Email = "omar@example.com",
            PasswordHash = "dummyhash",
            Phone = "444555666",
            University = "LIU",
            Listings = new List<Listing>()
        };

        // Now add listings and set Seller explicitly
        user1.Listings.Add(new Listing
        {
            Price = 20,
            Condition = "Good",
            City = "Beirut",
            Status = "Available",
            CreatedAt = DateTime.UtcNow,
            Book = new Book
            {
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Category = "Programming",
                ISBN = "9780132350884",
                Description = "Classic programming practices",
                Listings = new List<Listing>()
            },
            Images = new List<ListingImage>(),
            Seller = user1
        });

        user2.Listings.Add(new Listing
        {
            Price = 25,
            Condition = "Like New",
            City = "Tripoli",
            Status = "Available",
            CreatedAt = DateTime.UtcNow,
            Book = new Book
            {
                Title = "Database Systems",
                Author = "Elmasri & Navathe",
                Category = "Databases",
                ISBN = "9780133970777",
                Description = "Intro to DBMS",
                Listings = new List<Listing>()
            },
            Images = new List<ListingImage>(),
            Seller = user2
        });

        user3.Listings.Add(new Listing
        {
            Price = 40,
            Condition = "Fair",
            City = "Saida",
            Status = "Available",
            CreatedAt = DateTime.UtcNow,
            Book = new Book
            {
                Title = "Operating Systems",
                Author = "Silberschatz",
                Category = "Systems",
                ISBN = "9781119456339",
                Description = "Concepts and design",
                Listings = new List<Listing>()
            },
            Images = new List<ListingImage>(),
            Seller = user3
        });

        user5.Listings.Add(new Listing
        {
            Price = 35,
            Condition = "Excellent",
            City = "Tyre",
            Status = "Available",
            CreatedAt = DateTime.UtcNow,
            Book = new Book
            {
                Title = "Computer Networks",
                Author = "Tanenbaum",
                Category = "Networking",
                ISBN = "9780132126952",
                Description = "Networking fundamentals",
                Listings = new List<Listing>()
            },
            Images = new List<ListingImage>(),
            Seller = user5
        });

        db.Users.AddRange(user1, user2, user3, user4, user5);
        db.SaveChanges();
    }
}


app.UseHttpsRedirection();
//app.UseStaticFiles();
app.UseDefaultFiles();
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
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();



app.MapBookEndpoints();
app.MapImageEndpoints();
app.MapListingEndpoints();
app.MapCommunicationEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
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

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
