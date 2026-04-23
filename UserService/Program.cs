using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add PostgreSQL DbContext
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// GET: /api/users - Get all active users
app.MapGet("/api/users", async (UserDbContext db) =>
{
    var users = await db.Users.Where(u => u.IsActive).ToListAsync();
    return Results.Ok(users);
})
.WithName("GetUsers")
.Produces<List<User>>(StatusCodes.Status200OK);

// GET: /api/users/{id} - Get user by ID
app.MapGet("/api/users/{id:guid}", async (Guid id, UserDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    return user is not null && user.IsActive
        ? Results.Ok(user)
        : Results.NotFound(new { message = "User not found" });
})
.WithName("GetUser")
.Produces<User>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// POST: /api/users - Create new user
app.MapPost("/api/users", async (User user, UserDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Email == user.Email && u.IsActive))
    {
        return Results.BadRequest(new { message = "Email already exists" });
    }

    user.Id = Guid.NewGuid();
    user.CreatedAt = DateTime.UtcNow;
    user.IsActive = true;

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", user);
})
.WithName("CreateUser")
.Produces<User>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

// PUT: /api/users/{id} - Update user
app.MapPut("/api/users/{id:guid}", async (Guid id, User inputUser, UserDbContext db) =>
{
    var user = await db.Users.FindAsync(id);

    if (user is null || !user.IsActive)
        return Results.NotFound(new { message = "User not found" });

    if (await db.Users.AnyAsync(u => u.Email == inputUser.Email && u.Id != id && u.IsActive))
    {
        return Results.BadRequest(new { message = "Email already exists" });
    }

    user.Name = inputUser.Name;
    user.Email = inputUser.Email;
    user.PhoneNumber = inputUser.PhoneNumber;
    user.Address = inputUser.Address;
    user.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("UpdateUser")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status400BadRequest);

// DELETE: /api/users/{id} - Soft delete user
app.MapDelete("/api/users/{id:guid}", async (Guid id, UserDbContext db) =>
{
    var user = await db.Users.FindAsync(id);

    if (user is null)
        return Results.NotFound(new { message = "User not found" });

    user.IsActive = false;
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteUser")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// Health check endpoint
app.MapGet("/health", async (UserDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "Healthy", database = "Connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Database connection failed");
    }
})
.WithName("HealthCheck");

app.Run();