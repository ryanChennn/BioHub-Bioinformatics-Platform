using BIOHub.Data;
using BIOHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BIOHub.Services;

/// <summary>
/// 僅供 Docker Compose 本機展示環境執行一次性 Migration 與 Demo 帳號初始化。
/// 使用 BIOHUB_INIT_DB=true；一般網站啟動不執行此流程。
/// </summary>
public static class BioHubDemoInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        string password = configuration["BIOHUB_DEMO_PASSWORD"]
            ?? throw new InvalidOperationException(
                "缺少 BIOHUB_DEMO_PASSWORD：請在部署環境提供 Demo 密碼");

        if (password.Length < 12)
        {
            throw new InvalidOperationException(
                "BIOHUB_DEMO_PASSWORD 至少需要 12 個字元");
        }

        string username = configuration["BIOHUB_DEMO_USERNAME"] ?? "biohub_demo";
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100)
            throw new InvalidOperationException("Demo 帳號名稱不正確");

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BIOHubDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Users>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("BioHubDemoInitializer");

        // 等待新建立的 SQL Server 接受連線：最多嘗試 30 次，每次間隔 3 秒。
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("EF Core Migrations 已成功套用");
                break;
            }
            catch (SqlException ex) when (attempt < 30)
            {
                logger.LogWarning("等待 SQL Server ({Attempt}/30)：{Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        // 只允許初始化器建立或更新固定的 Demo 帳號，避免誤改真實使用者。
        // 重跑 db-init 時同步更新 Demo 密碼，以修正第一次初始化使用錯誤密碼的情況。
        const string demoEmail = "biohub-demo@example.invalid";
        var existingDemo = await db.Users.SingleOrDefaultAsync(u => u.UserName == username);
        if (existingDemo != null)
        {
            if (existingDemo.Email != demoEmail || existingDemo.Organization != "BioHub Demo")
            {
                throw new InvalidOperationException(
                    "同名帳號不是本初始化器建立的 Demo 使用者，已拒絕覆寫。請先確認帳號設定。");
            }

            existingDemo.PasswordHash = hasher.HashPassword(existingDemo, password);
            existingDemo.IsActive = true;
            await db.SaveChangesAsync();
            logger.LogInformation("Demo 帳號已存在，密碼雜湊已安全更新：{UserName}", username);
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == demoEmail))
        {
            throw new InvalidOperationException(
                "預設 Demo Email 已被其他帳號使用，請檢查資料庫");
        }

        var user = new Users
        {
            UserName = username,
            FullName = "BioHub Demo User",
            Phone = "0000000000",
            Email = demoEmail,
            Organization = "BioHub Demo",
            CreateAt = DateTime.UtcNow,
            LastAt = null,
            IsActive = true
        };

        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        logger.LogInformation("Demo 帳號已建立：{UserName}（不記錄密碼）", username);
    }
}
