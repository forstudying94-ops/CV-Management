using System.Security.Cryptography;
using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface IAuthServices {
    Task<User?> AuthenticateAsync(string email, string password, CancellationToken token = default);

    Task<User> RegisterCandidateAsync(string firstName, string lastName, string email, string password, CancellationToken token = default);

    Task<User> RegisterRecruiterAsync(string firstName, string lastName, string email, string password, string company, CancellationToken token = default);

    Task<User> FindOrCreateExternalAsync(string provider, string subject, string email, string displayName, CancellationToken token = default);
}

public sealed class AuthServices : IAuthServices
{
    private const int PasswordIterations = 100_000;
    private readonly DatabaseContext _db;

    public AuthServices(DatabaseContext db) {
        _db = db;
    }

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken token = default) {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _db.Users.FirstOrDefaultAsync(savedUser => savedUser.Email == normalizedEmail, token);
        if (user is null || user.IsBlocked || !VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }
        return user;
    }

    public Task<User> RegisterCandidateAsync(string firstName, string lastName, string email, string password, CancellationToken token = default) {
        return RegisterAsync(firstName, lastName, email, password, UserRole.Candidate, company: null, createCandidateProfile: true, token);
    }

    public Task<User> RegisterRecruiterAsync(string firstName, string lastName, string email, string password, string company, CancellationToken token = default) {
        return RegisterAsync(firstName, lastName, email, password, UserRole.Recruiter, company.Trim(), createCandidateProfile: false, token);
    }

    public async Task<User> FindOrCreateExternalAsync(string provider, string subject, string email, string displayName, CancellationToken token = default) {
        var normalizedEmail = NormalizeEmail(email);
        var externalLogin = await _db.UserExternalLogins
            .Include(login => login.UserForExternalLogin)
            .FirstOrDefaultAsync(login => login.Provider == provider && login.Subject == subject, token);
        if (externalLogin is not null)
        {
            if (externalLogin.UserForExternalLogin.IsBlocked)
            {
                throw new UnauthorizedAccessException("Account is blocked.");
            }

            return externalLogin.UserForExternalLogin;
        }

        var user = await _db.Users
            .Include(savedUser => savedUser.ExternalLoginForUser)
            .FirstOrDefaultAsync(savedUser => savedUser.Email == normalizedEmail, token);

        if (user is null)
        {
            var nameParts = displayName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var userId = Guid.NewGuid();

            user = new User
            {
                UserId = userId,
                FirstName = nameParts.FirstOrDefault() ?? "Candidate",
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                Email = normalizedEmail,
                PasswordHash = HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))),
                Role = UserRole.Candidate,
                ProfileForUser = new ProfileCandidate
                {
                    ProfileCandidateId = Guid.NewGuid(),
                    UserId = userId
                }
            };

            _db.Users.Add(user);
        }
        else if (user.IsBlocked)
        {
            throw new UnauthorizedAccessException("Account is blocked.");
        }

        user.ExternalLoginForUser.Add(new UserExternalLogin
        {
            Provider = provider,
            Subject = subject,
            UserId = user.UserId
        });

        await _db.SaveChangesAsync(token);
        return user;
    }

    private async Task<User> RegisterAsync(string firstName, string lastName, string email, string password, UserRole role, string? company, bool createCandidateProfile, CancellationToken token)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (await _db.Users.AnyAsync(user => user.Email == normalizedEmail, token))
        {
            throw new InvalidOperationException("A user with this email already exists, you may login at login menu");
        }
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, FirstName = firstName.Trim(), LastName = lastName.Trim(),
            Email = normalizedEmail, PasswordHash = HashPassword(password), Role = role, Company = company};

        if (createCandidateProfile)
        {
            user.ProfileForUser = new ProfileCandidate { ProfileCandidateId = Guid.NewGuid(), UserId = userId };
        }
        _db.Users.Add(user);
        await _db.SaveChangesAsync(token);
        return user;
    }

    private static string NormalizeEmail(string email) {
        return email.Trim().ToLowerInvariant();
    }

    internal static string HashPassword(string password) {
        const int saltSize = 16;
        const int hashSize = 32;

        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            hashSize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string passwordHash) {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
