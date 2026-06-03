using CoreMs.UserMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreMs.UserMs.Infrastructure.Data;

/// <summary>
/// Seeds development/staging test data. Run via CLI: dotnet run -- --seed
/// Password for all users: Password123!
/// </summary>
public class SeedDataService
{
    private readonly UserMsDbContext _context;
    private readonly ILogger<SeedDataService> _logger;

    // BCrypt hash of "Password123!"
    private const string DefaultPasswordHash = "$2a$10$qdt5KNdDULqFsZi30vj38ePzMkUi1t2NtHnL3jgpTTk0p3ElLyOoq";

    public SeedDataService(UserMsDbContext context, ILogger<SeedDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Applying migrations...");
        await _context.Database.MigrateAsync();

        if (await _context.Set<UserEntity>().AnyAsync(u => u.Email == "super@corems.local"))
        {
            _logger.LogInformation("Seed data already exists — skipping");
            return;
        }

        _logger.LogInformation("Seeding test data...");
        _context.Set<UserEntity>().AddRange(CreateUsers());
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seed data complete — 30 users created");
    }

    private static List<UserEntity> CreateUsers()
    {
        var users = new List<UserEntity>();

        users.Add(CreateUser("10000000-0000-0000-0000-000000000001", "super@corems.local", "Super", "Admin", ["SUPER_ADMIN"]));
        users.Add(CreateUser("20000000-0000-0000-0000-000000000001", "admin@corems.local", "Admin", "User",
            ["USER_MS_ADMIN", "USER_MS_USER", "COMMUNICATION_MS_ADMIN", "DOCUMENT_MS_ADMIN", "TRANSLATION_MS_ADMIN"]));
        users.Add(CreateUser("20000000-0000-0000-0000-000000000002", "john.admin@corems.local", "John", "Admin", ["USER_MS_ADMIN", "USER_MS_USER"]));
        users.Add(CreateUser("20000000-0000-0000-0000-000000000003", "sarah.admin@corems.local", "Sarah", "Admin", ["COMMUNICATION_MS_ADMIN", "COMMUNICATION_MS_USER", "USER_MS_USER"]));
        users.Add(CreateUser("20000000-0000-0000-0000-000000000004", "mike.docadmin@corems.local", "Mike", "DocAdmin", ["DOCUMENT_MS_ADMIN", "DOCUMENT_MS_USER", "USER_MS_USER"]));
        users.Add(CreateUser("20000000-0000-0000-0000-000000000005", "lisa.transadmin@corems.local", "Lisa", "TransAdmin", ["TRANSLATION_MS_ADMIN", "USER_MS_USER"]));

        var regularUsers = new (string Num, string Email, string First, string Last)[]
        {
            ("06", "alice.johnson@corems.local", "Alice", "Johnson"),
            ("07", "bob.wilson@corems.local", "Bob", "Wilson"),
            ("08", "charlie.brown@corems.local", "Charlie", "Brown"),
            ("09", "diana.prince@corems.local", "Diana", "Prince"),
            ("10", "edward.stark@corems.local", "Edward", "Stark"),
            ("11", "fiona.green@corems.local", "Fiona", "Green"),
            ("12", "george.miller@corems.local", "George", "Miller"),
            ("13", "hannah.davis@corems.local", "Hannah", "Davis"),
            ("14", "ivan.petrov@corems.local", "Ivan", "Petrov"),
            ("15", "julia.roberts@corems.local", "Julia", "Roberts"),
            ("16", "kevin.hart@corems.local", "Kevin", "Hart"),
            ("17", "laura.smith@corems.local", "Laura", "Smith"),
            ("18", "marcus.lee@corems.local", "Marcus", "Lee"),
            ("19", "nina.williams@corems.local", "Nina", "Williams"),
            ("20", "oscar.martinez@corems.local", "Oscar", "Martinez"),
            ("21", "peter.parker@corems.local", "Peter", "Parker"),
            ("22", "quinn.taylor@corems.local", "Quinn", "Taylor"),
            ("23", "rachel.adams@corems.local", "Rachel", "Adams"),
            ("24", "steve.rogers@corems.local", "Steve", "Rogers"),
            ("25", "tina.turner@corems.local", "Tina", "Turner"),
            ("26", "uma.watson@corems.local", "Uma", "Watson"),
            ("27", "victor.hugo@corems.local", "Victor", "Hugo"),
            ("28", "wendy.clark@corems.local", "Wendy", "Clark"),
            ("29", "xavier.jones@corems.local", "Xavier", "Jones"),
            ("30", "yara.silva@corems.local", "Yara", "Silva"),
        };

        foreach (var (num, email, first, last) in regularUsers)
            users.Add(CreateUser($"20000000-0000-0000-0000-0000000000{num}", email, first, last, ["USER_MS_USER"]));

        return users;
    }

    private static UserEntity CreateUser(string uuid, string email, string firstName, string lastName, string[] roles)
    {
        var user = new UserEntity
        {
            Uuid = Guid.Parse(uuid),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Provider = "local",
            Password = DefaultPasswordHash,
            EmailVerified = true
        };

        foreach (var role in roles)
            user.Roles.Add(new UserRoleEntity { Name = role });

        return user;
    }
}
