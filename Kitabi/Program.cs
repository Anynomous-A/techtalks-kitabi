using Kitabi.Data;
using Kitabi.Endpoints;
using Kitabi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Kitabi");
builder.Services.AddSqlServer<ApplicationDbContext>(connectionString);




// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();


app.MigrateDb();
app.MapBookEndpoints();
app.MapImageEndpoints();
app.MapListingEndpoints();


app.Run();
