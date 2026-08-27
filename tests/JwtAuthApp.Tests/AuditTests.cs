using JwtAuthApp.Data;
using JwtAuthApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JwtAuthApp.Tests
{
    public class AuditTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options, new HttpContextAccessor());
        }

        [Fact]
        public async Task SaveChanges_CreatesChangeAuditLogs_ForUser()
        {
            using var db = CreateDb();
            db.Users.Add(new User { UserName = "auditme", PasswordHash = "hash1", Salt = "salt1", Role = "User" });

            await db.SaveChangesAsync();

            var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.Type == AuditLogType.Change && a.EntityType == nameof(User));
            Assert.NotNull(log);
            Assert.Equal("Added", log.ChangeType);
        }

        [Fact]
        public async Task AuditLogSnapshot_DoesNotContain_PasswordHashAndSalt()
        {
            using var db = CreateDb();
            db.Users.Add(new User { UserName = "secretkeeper", PasswordHash = "super-secret-hash", Salt = "super-secret-salt", Role = "User" });

            await db.SaveChangesAsync();

            var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.Type == AuditLogType.Change && a.EntityType == nameof(User));
            Assert.NotNull(log);

            var snapshot = JsonSerializer.Deserialize<Dictionary<string, object?>>(log!.NewValues!);
            Assert.NotNull(snapshot);
            Assert.False(snapshot.ContainsKey(nameof(User.PasswordHash)));
            Assert.False(snapshot.ContainsKey(nameof(User.Salt)));

            var json = log.NewValues!;
            Assert.DoesNotContain("super-secret-hash", json);
            Assert.DoesNotContain("super-secret-salt", json);
        }

        [Fact]
        public async Task PasswordChange_ChangeLog_HidesOldAndNewHash()
        {
            using var db = CreateDb();
            var user = new User { UserName = "changer", PasswordHash = "old-hash", Salt = "old-salt", Role = "User" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            user.PasswordHash = "new-hash";
            user.Salt = "new-salt";
            await db.SaveChangesAsync();

            var logs = await db.AuditLogs
                .Where(a => a.Type == AuditLogType.Change && a.EntityType == nameof(User))
                .ToListAsync();

            // Первый лог — Added, второй — Modified
            var modified = logs.FirstOrDefault(l => l.ChangeType == "Modified");
            Assert.NotNull(modified);

            Assert.DoesNotContain("old-hash", modified!.OriginalValues ?? "");
            Assert.DoesNotContain("new-hash", modified.NewValues ?? "");
            Assert.DoesNotContain("PasswordHash", modified.ChangedProperties ?? "");
        }

        [Fact]
        public async Task AuditLogs_Themselves_AreNotLogged()
        {
            using var db = CreateDb();
            db.Users.Add(new User { UserName = "logtest", PasswordHash = "h", Salt = "s", Role = "User" });
            await db.SaveChangesAsync();

            // Сами записи аудита не порождают вложенных логов
            Assert.All(await db.AuditLogs.ToListAsync(), l => Assert.Equal("User", l.EntityType));
        }
    }
}