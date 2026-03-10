using APP.Models.Database;
using Microsoft.EntityFrameworkCore;
using SetSupply.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Database;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RememberUser> RememberUser => Set<RememberUser>();
    public DbSet<PLCSetting> PLCSetting => Set<PLCSetting>();
    //public DbSet<History> History => Set<History>();
    //public DbSet<Person> Person => Set<Person>();

    // Shift Settings
    public DbSet<ShiftSetting> ShiftSettings => Set<ShiftSetting>();
    public DbSet<ShiftSettingHistory> ShiftSettingHistory => Set<ShiftSettingHistory>();

    // Production Data
    public DbSet<ProductionData> ProductionData => Set<ProductionData>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(e => e.UserID);
        modelBuilder.Entity<RememberUser>().HasKey(e => e.Id);
        modelBuilder.Entity<PLCSetting>().HasKey(e => e.Name);
        modelBuilder.Entity<History>().HasKey(e => e.Id);
        modelBuilder.Entity<Person>().HasKey(e => e.ID);

        // ===== CẤU HÌNH CHO SHIFTSETTING =====
        modelBuilder.Entity<ShiftSetting>().HasKey(e => e.Id);
        modelBuilder.Entity<ShiftSetting>()
            .HasIndex(e => e.ShiftNumber)
            .IsUnique();

        // ===== CẤU HÌNH CHO SHIFTSETTINGHISTORY =====
        modelBuilder.Entity<ShiftSettingHistory>().HasKey(e => e.Id);
        modelBuilder.Entity<ShiftSettingHistory>()
            .HasIndex(e => new { e.DateUsing, e.ShiftNumber })
            .HasDatabaseName("IX_ShiftSettingHistory_DateUsing_ShiftNumber");

        // ===== CẤU HÌNH CHO PRODUCTIONDATA =====
        modelBuilder.Entity<ProductionData>().HasKey(e => e.Id);

        // Index cho tìm kiếm theo DateTime và Model (tăng performance)
        modelBuilder.Entity<ProductionData>()
            .HasIndex(e => new { e.DateTimeInsert, e.Model })
            .HasDatabaseName("IX_ProductionData_DateTime_Model");


        // Index cho Codeback
        modelBuilder.Entity<ProductionData>()
            .HasIndex(e => e.Codeback)
            .HasDatabaseName("IX_ProductionData_Codeback");

        // Index cho Model
        modelBuilder.Entity<ProductionData>()
            .HasIndex(e => e.Model)
            .HasDatabaseName("IX_ProductionData_Model");

        // Index cho Date (để query nhanh theo ngày)
        modelBuilder.Entity<ProductionData>()
            .HasIndex(e => e.DateTimeInsert)
            .HasDatabaseName("IX_ProductionData_DateTimeInsert");

        // ===== SEED DATA CHO USER =====
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserID = "pe",
                userName = "pe",
                passWord = "1",
                role = "Admin"
            }
        );

        // ===== SEED DATA MẶC ĐỊNH CHO 3 CA =====
        modelBuilder.Entity<ShiftSetting>().HasData(
            new ShiftSetting
            {
                Id = 1,
                ShiftNumber = 1,
                StartTime = new TimeSpan(6, 0, 0),
                EndTime = new TimeSpan(14, 0, 0),
                IsUse = true,
                InsertTime = DateTime.Now
            },
            new ShiftSetting
            {
                Id = 2,
                ShiftNumber = 2,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(22, 0, 0),
                IsUse = true,
                InsertTime = DateTime.Now
            },
            new ShiftSetting
            {
                Id = 3,
                ShiftNumber = 3,
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                IsUse = true,
                InsertTime = DateTime.Now
            }
        );
    }
}