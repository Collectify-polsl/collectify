using Collectify.Api.Data;
using Collectify.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add SQLite database context
builder.Services.AddDbContext<CollectifyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Ensure database is created and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CollectifyDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Add SQLite database endpoints
app.MapGet("/api/items", async (CollectifyDbContext db) =>
{
    return await db.CollectionItems.ToListAsync();
})
.WithName("GetAllItems");

app.MapGet("/api/items/{id}", async (int id, CollectifyDbContext db) =>
{
    var item = await db.CollectionItems.FindAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
})
.WithName("GetItemById");

app.MapPost("/api/items", async (CollectionItem item, CollectifyDbContext db) =>
{
    db.CollectionItems.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/items/{item.Id}", item);
})
.WithName("CreateItem");

app.MapPut("/api/items/{id}", async (int id, CollectionItem inputItem, CollectifyDbContext db) =>
{
    var item = await db.CollectionItems.FindAsync(id);
    if (item is null) return Results.NotFound();

    item.Name = inputItem.Name;
    item.Description = inputItem.Description;
    item.Category = inputItem.Category;
    item.Value = inputItem.Value;

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UpdateItem");

app.MapDelete("/api/items/{id}", async (int id, CollectifyDbContext db) =>
{
    var item = await db.CollectionItems.FindAsync(id);
    if (item is null) return Results.NotFound();

    db.CollectionItems.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteItem");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
