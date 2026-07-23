using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public static class DatabaseSeeder {
    public static async Task SeedAsync(DatabaseContext db, IConfiguration configuration, CancellationToken token = default) {
        await SeedAdminAsync(db, configuration, token);
    }

    private static async Task SeedAdminAsync(DatabaseContext db, IConfiguration configuration, CancellationToken token) {
        var email = configuration["Seed:Admin:Email"];
        var password = configuration["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            await db.Users.AnyAsync(user => user.Email == email.ToLower(), token))
        {
            return;
        }

        db.Users.Add(new User
        {
            UserId = Guid.NewGuid(),
            FirstName = configuration["Seed:Admin:FirstName"] ?? "System",
            LastName = configuration["Seed:Admin:LastName"] ?? "Administrator",
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = AuthServices.HashPassword(password),
            Role = UserRole.Admin
        });

        await db.SaveChangesAsync(token);
    }
}
