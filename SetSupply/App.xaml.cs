using APP.Database;
using APP.Interface.language;
using APP.Models.Database;
using APP.Service;
using APP.UserControls;

using APP.UserControls.Setting.Sub;
using APP.ViewModels;
using APP.ViewModels.FormViewModels;
using APP.ViewModels.UserControlViewModels.Home;

using APP.ViewModels.UserControlViewModels.Setting;
using APP.ViewModels.UserControlViewModels.Setting.Sub;
using APP.Views;
using LiveChartsCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SetSupply.Models.Database;
using SetSupply.Service;
using SetSupply.UserControls.DashBoard;
using SetSupply.UserControls.History;
using SetSupply.UserControls.Setting.Sub;
using SetSupply.ViewModels.UserControlViewModels.DashBoard;
using SetSupply.ViewModels.UserControlViewModels.History;
using SetSupply.ViewModels.UserControlViewModels.Setting.Sub;
using SQLitePCL;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace APP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        private static Mutex mutex;


        protected override void OnExit(ExitEventArgs e)
        {
            var historyService = App.ServiceProvider.GetRequiredService<ShiftHistoryBackgroundService>();
            var plcService = App.ServiceProvider.GetRequiredService<PLCService>();
            historyService.Stop();
            plcService.Stop();
            App.ServiceProvider.GetRequiredService<PlanImportService>().Stop();
            base.OnExit(e);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            const string appName = "APP";
            bool createdNew;
            mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                ActivateExistingWindow();
                Shutdown();
                return;
            }

            Batteries.Init();
            var services = new ServiceCollection();

            //Database
            string pathdb = Directory.GetCurrentDirectory() + "\\Resources\\Database\\SetSupply.db";

            // Tạo thư mục nếu chưa có
            string? dbDirectory = Path.GetDirectoryName(pathdb);
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            services.AddDbContext<AppDbContext>(option =>
            {
                option.UseSqlite($"Data Source={pathdb}");
            });

            //Authorization
            services.AddSingleton<UserSession>();
            services.AddSingleton<AuthorizationService>();

            // Services
            services.AddSingleton<PLCService>();
            services.AddSingleton<ShiftSettingService>(); // THÊM DÒNG NÀY
            services.AddSingleton<ShiftScheduleService>();
            services.AddSingleton<ProductionDataService>();
            services.AddSingleton<ModelGroupService>();
            services.AddSingleton<ProductionPlanService>();
            services.AddSingleton<AppSettingService>();
            services.AddSingleton<PlanImportService>();
            services.AddSingleton<TestDataService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();

            // ViewModels & UserControls
            services.AddSingleton<UCControlBarViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoginWindowViewModel>();
            services.AddSingleton<UCHomeViewModel>();
            services.AddSingleton<UCSettingViewModel>();
            services.AddSingleton<UCPLCSettingViewModel>();



            services.AddSingleton<UCHistoryViewModel>();


            services.AddSingleton<UCDashboardViewModel>();



            services.AddSingleton<UCHome>();
            services.AddSingleton<UCSetting>();
            services.AddSingleton<UCPLCSetting>();
            services.AddSingleton<UCHelp>();
            services.AddSingleton<UCHistory>();
            services.AddSingleton<UCShiftSettingViewModel>();
            services.AddSingleton<UCShiftSetting>();
            services.AddSingleton<UCDashboard>();
            services.AddSingleton<UCHomeHeader>();
            services.AddSingleton<UCModelGroupSettingViewModel>();
            services.AddSingleton<UCModelGroupSetting>();
            services.AddSingleton<UCDailyPlanSettingViewModel>();
            services.AddSingleton<UCDailyPlanSetting>();
            services.AddSingleton<UCGeneralSettingViewModel>();
            services.AddSingleton<UCGeneralSetting>();
            services.AddSingleton<UCTestDataSettingViewModel>();
            services.AddSingleton<UCTestDataSetting>();

            //ManagementUser
            services.AddSingleton<UserManagementViewModel>();
            services.AddSingleton<UserManagement>();

            services.AddSingleton<ShiftHistoryBackgroundService>();

            //Init
            ServiceProvider = services.BuildServiceProvider();

            // ===== KHỞI TẠO DATABASE =====
            await InitializeDatabaseAsync();

            //Remember
            var db = ServiceProvider.GetRequiredService<AppDbContext>();
            var userremember = db.RememberUser.FirstOrDefault();
            if (userremember != null)
            {
                var au = ServiceProvider.GetRequiredService<AuthorizationService>();
                await au.LoginAsync(userremember.UserID, userremember.PassWord, true);
            }

            //Start Monitor
            var PLC = App.ServiceProvider.GetRequiredService<PLCService>();
            PLC.Start();



           
            // Trong OnStartup hoặc sau khi build ServiceProvider
            var historyService = App.ServiceProvider.GetRequiredService<ShiftHistoryBackgroundService>();
            historyService.Start();

            // Tự động import file kế hoạch mới trong thư mục (Cài đặt > Daily Plan)
            App.ServiceProvider.GetRequiredService<PlanImportService>().Start();


            base.OnStartup(e);
        }

        /// <summary>
        /// Tự động tạo database
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    Debug.WriteLine("========== BẮT ĐẦU KHỞI TẠO DATABASE ==========");

                    // Tạo database và tất cả bảng
                    bool created = await dbContext.Database.EnsureCreatedAsync();

                    if (created)
                    {
                        Debug.WriteLine("✓ Database đã được tạo mới!");
                    }
                    else
                    {
                        Debug.WriteLine("✓ Database đã tồn tại.");
                    }

                    // DB cũ: EnsureCreated không tự thêm bảng/index mới => bổ sung thủ công
                    await EnsureSchemaUpdatesAsync(dbContext);

                    // Seed data mặc định
                    await SeedDefaultDataAsync(dbContext);

                    Debug.WriteLine("========== KHỞI TẠO DATABASE HOÀN TẤT ==========");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"========== LỖI KHỞI TẠO DATABASE ==========");
                Debug.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Lỗi khởi tạo database:\n\n{ex.Message}\n\nChi tiết:\n{ex.InnerException?.Message}",
                    "Lỗi Database",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // Có thể shutdown app nếu database critical
                // Shutdown();
            }
        }

        /// <summary>
        /// Bổ sung bảng / index / sửa dữ liệu cho database đã tồn tại từ phiên bản trước.
        /// Tất cả câu lệnh đều chạy lại được nhiều lần (idempotent).
        /// </summary>
        private static async Task EnsureSchemaUpdatesAsync(AppDbContext dbContext)
        {
            // Nhóm model: gán trực tiếp model -> nhóm (thay cho bảng ModelGroups theo tiền tố của bản trước)
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""ModelMappings"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ModelMappings"" PRIMARY KEY AUTOINCREMENT,
                    ""GroupName"" TEXT NOT NULL,
                    ""ModelName"" TEXT NOT NULL,
                    ""ImagePath"" TEXT NOT NULL DEFAULT '',
                    ""SortOrder"" INTEGER NOT NULL DEFAULT 0
                );");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ModelMappings_ModelName"" ON ""ModelMappings"" (""ModelName"");");

            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""PlanImportLogs"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PlanImportLogs"" PRIMARY KEY AUTOINCREMENT,
                    ""FilePath"" TEXT NOT NULL,
                    ""FileName"" TEXT NOT NULL,
                    ""FileSize"" INTEGER NOT NULL,
                    ""FileLastWrite"" TEXT NOT NULL,
                    ""ImportedAt"" TEXT NOT NULL,
                    ""Source"" TEXT NOT NULL,
                    ""Success"" INTEGER NOT NULL,
                    ""DaysImported"" INTEGER NOT NULL,
                    ""Message"" TEXT NOT NULL
                );");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS ""IX_PlanImportLogs_FilePath"" ON ""PlanImportLogs"" (""FilePath"");");

            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AppSettings"" (
                    ""Key"" TEXT NOT NULL CONSTRAINT ""PK_AppSettings"" PRIMARY KEY,
                    ""Value"" TEXT NOT NULL
                );");

            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""ProductionPlans"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ProductionPlans"" PRIMARY KEY AUTOINCREMENT,
                    ""PlanDate"" TEXT NOT NULL,
                    ""GroupName"" TEXT NOT NULL,
                    ""ShiftNumber"" INTEGER NOT NULL,
                    ""Quantity"" INTEGER NOT NULL
                );");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS ""IX_ProductionPlans_Date_Group_Shift""
                ON ""ProductionPlans"" (""PlanDate"", ""GroupName"", ""ShiftNumber"");");

            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS ""IX_ProductionData_Model_Serial""
                ON ""ProductionData"" (""Model"", ""Serial"");");

            // Phiên bản cũ lưu cả chuỗi mã (model 12 ký tự + serial 7 ký tự) vào cột Model
            // => tách lại model để gom nhóm được. Bản ghi đúng có Model khác Codeback nên không bị ảnh hưởng.
            int fixedRows = await dbContext.Database.ExecuteSqlRawAsync(@"
                UPDATE ""ProductionData""
                SET ""Model"" = TRIM(SUBSTR(""Model"", 1, 12))
                WHERE ""Model"" = ""Codeback"" AND LENGTH(""Model"") >= 19;");
            if (fixedRows > 0)
                Debug.WriteLine($"✓ Đã sửa cột Model cho {fixedRows} bản ghi ProductionData cũ.");

            await MigratePrefixGroupsAsync(dbContext);

            // Giờ nghỉ trong ca + giờ ca / giờ nghỉ của từng ngày sản xuất (xem lại ngày cũ đúng cài đặt)
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""ShiftBreaks"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ShiftBreaks"" PRIMARY KEY AUTOINCREMENT,
                    ""ShiftNumber"" INTEGER NOT NULL,
                    ""StartTime"" TEXT NOT NULL,
                    ""EndTime"" TEXT NOT NULL,
                    ""Name"" TEXT NOT NULL DEFAULT '',
                    ""SortOrder"" INTEGER NOT NULL DEFAULT 0
                );");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""ShiftDaySnapshots"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ShiftDaySnapshots"" PRIMARY KEY AUTOINCREMENT,
                    ""ProductionDate"" TEXT NOT NULL,
                    ""ShiftsJson"" TEXT NOT NULL,
                    ""UpdatedAt"" TEXT NOT NULL
                );");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS ""IX_ShiftDaySnapshots_ProductionDate"" ON ""ShiftDaySnapshots"" (""ProductionDate"");");

            // Cột đánh dấu dữ liệu test (Cài đặt > Test Data)
            var productionColumns = (await QueryAsync(dbContext, "PRAGMA table_info(\"ProductionData\");"))
                .Select(r => Convert.ToString(r[1])).ToList();
            if (!productionColumns.Contains("IsTest", StringComparer.OrdinalIgnoreCase))
                await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""ProductionData"" ADD COLUMN ""IsTest"" INTEGER NOT NULL DEFAULT 0;");
        }

        /// <summary>
        /// Chuyển 1 lần từ nhóm theo tiền tố (bảng ModelGroups cũ) sang gán từng model (ModelMappings):
        /// các model đã từng sản xuất được gán vào nhóm có tiền tố khớp dài nhất. Bảng ModelGroups cũ giữ nguyên, không dùng nữa.
        /// </summary>
        private static async Task MigratePrefixGroupsAsync(AppDbContext dbContext)
        {
            if (await dbContext.ModelMappings.AnyAsync()) return;
            if ((await QueryAsync(dbContext, "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'ModelGroups'")).Count == 0) return;

            var groups = (await QueryAsync(dbContext, "SELECT \"Name\", \"Prefixes\", \"SortOrder\" FROM \"ModelGroups\""))
                .Select(r => new
                {
                    Name = Convert.ToString(r[0])?.Trim() ?? string.Empty,
                    Prefixes = (Convert.ToString(r[1]) ?? string.Empty)
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim()).Where(p => p.Length > 0).ToList(),
                    Order = Convert.ToInt32(r[2])
                })
                .Where(g => g.Name.Length > 0 && g.Prefixes.Count > 0)
                .ToList();
            if (groups.Count == 0) return;

            var models = await dbContext.ProductionData.AsNoTracking().Select(p => p.Model).Distinct().ToListAsync();
            var mappings = new List<ModelMapping>();
            foreach (var model in models.OrderBy(m => m))
            {
                var best = groups
                    .SelectMany(g => g.Prefixes.Select(p => new { Group = g, Prefix = p }))
                    .Where(x => model.StartsWith(x.Prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Prefix.Length)
                    .FirstOrDefault();
                if (best == null) continue;
                mappings.Add(new ModelMapping { GroupName = best.Group.Name, ModelName = model, SortOrder = best.Group.Order * 1000 + mappings.Count });
            }

            dbContext.ModelMappings.AddRange(mappings);
            await dbContext.SaveChangesAsync();
            Debug.WriteLine($"✓ Đã chuyển {mappings.Count} model từ nhóm theo tiền tố sang ModelMappings.");
        }

        private static async Task<List<object?[]>> QueryAsync(AppDbContext dbContext, string sql)
        {
            var rows = new List<object?[]>();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var values = new object?[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                        values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(values);
                }
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
            return rows;
        }

        /// <summary>
        /// Seed dữ liệu mặc định
        /// </summary>
        private async Task SeedDefaultDataAsync(AppDbContext dbContext)
        {
            try
            {
                // Seed ShiftSettings
                if (!dbContext.ShiftSettings.Any())
                {
                    Debug.WriteLine("→ Đang thêm dữ liệu mặc định cho ShiftSettings...");

                    dbContext.ShiftSettings.AddRange(
                        new ShiftSetting
                        {
                            ShiftNumber = 1,
                            StartTime = new TimeSpan(6, 0, 0),
                            EndTime = new TimeSpan(14, 0, 0),
                            IsUse = true, // THÊM DÒNG NÀY
                            InsertTime = DateTime.Now
                        },
                        new ShiftSetting
                        {
                            ShiftNumber = 2,
                            StartTime = new TimeSpan(14, 0, 0),
                            EndTime = new TimeSpan(22, 0, 0),
                            IsUse = true, // THÊM DÒNG NÀY
                            InsertTime = DateTime.Now
                        },
                        new ShiftSetting
                        {
                            ShiftNumber = 3,
                            StartTime = new TimeSpan(22, 0, 0),
                            EndTime = new TimeSpan(6, 0, 0),
                            IsUse = true, // THÊM DÒNG NÀY
                            InsertTime = DateTime.Now
                        }
                    );

                    await dbContext.SaveChangesAsync();
                    Debug.WriteLine("✓ Dữ liệu ShiftSettings đã được thêm!");
                }
                //if (!dbContext.ProductionData.Any())
                //{
                //    Debug.WriteLine("→ Đang thêm dữ liệu mẫu cho ProductionData...");

                //    var testData = new List<SetSupply.Models.Database.ProductionData>();
                //    var random = new Random();
                //    var models = new[] { "Model-A", "Model-B", "Model-C", "Model-D" };

                //    int serialCounter = 1;

                //    // TẠO DỮ LIỆU CHO HÔM NAY
                //    DateTime today = DateTime.Today;

                //    // CA 1: 6:00 - 14:00 (8 giờ)
                //    for (int hour = 6; hour < 14; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            // Mỗi model mỗi giờ tạo từ 40-80 sản phẩm
                //            int productsPerHour = random.Next(40, 81);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{today:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = today.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    // CA 2: 14:00 - 22:00 (8 giờ)
                //    for (int hour = 14; hour < 22; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            // Mỗi model mỗi giờ tạo từ 45-85 sản phẩm
                //            int productsPerHour = random.Next(45, 86);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{today:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = today.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    // CA 3: 22:00 - 6:00 (8 giờ) - CA QUA ĐÊM
                //    // 22:00 - 24:00 (hôm nay)
                //    for (int hour = 22; hour < 24; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            int productsPerHour = random.Next(35, 71);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{today:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = today.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    // 0:00 - 6:00 (ngày mai - nhưng vẫn thuộc ca 3 của hôm nay)
                //    DateTime tomorrow = today.AddDays(1);
                //    for (int hour = 0; hour < 6; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            int productsPerHour = random.Next(35, 71);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{tomorrow:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = tomorrow.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    // TẠO THÊM DỮ LIỆU CHO NGÀY HÔM QUA (để test xem lại)
                //    DateTime yesterday = today.AddDays(-1);

                //    // CA 1 - Hôm qua
                //    for (int hour = 6; hour < 14; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            int productsPerHour = random.Next(50, 91);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{yesterday:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = yesterday.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    // CA 2 - Hôm qua
                //    for (int hour = 14; hour < 22; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            int productsPerHour = random.Next(55, 96);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{yesterday:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = yesterday.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    // CA 3 - Hôm qua (22:00 - 6:00 hôm nay)
                //    for (int hour = 22; hour < 24; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            int productsPerHour = random.Next(40, 76);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{yesterday:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = yesterday.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    for (int hour = 0; hour < 6; hour++)
                //    {
                //        foreach (var model in models)
                //        {
                //            int productsPerHour = random.Next(40, 76);

                //            for (int i = 0; i < productsPerHour; i++)
                //            {
                //                var randomMinute = random.Next(0, 60);
                //                var randomSecond = random.Next(0, 60);

                //                testData.Add(new SetSupply.Models.Database.ProductionData
                //                {
                //                    Codeback = $"CB{serialCounter:D6}",
                //                    Model = model,
                //                    Serial = $"SN{today:yyyyMMdd}{serialCounter:D6}",
                //                    DateTimeInsert = today.AddHours(hour).AddMinutes(randomMinute).AddSeconds(randomSecond)
                //                });

                //                serialCounter++;
                //            }
                //        }
                //    }

                //    // Thêm tất cả vào database
                //    dbContext.ProductionData.AddRange(testData);
                //    await dbContext.SaveChangesAsync();

                //    Debug.WriteLine($"✓ Đã thêm {testData.Count} dữ liệu mẫu cho ProductionData!");
                //    Debug.WriteLine($"  - Hôm qua: {testData.Count(d => d.DateTimeInsert.Date == yesterday)} records");
                //    Debug.WriteLine($"  - Hôm nay: {testData.Count(d => d.DateTimeInsert.Date == today || d.DateTimeInsert.Date == tomorrow)} records");
                //    Debug.WriteLine($"  - Models: {string.Join(", ", models)}");
                //}
                else
                {
                    Debug.WriteLine("→ ProductionData đã có dữ liệu.");
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Lỗi seed data: {ex.Message}");
            }
        }

        private void ActivateExistingWindow()
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        // đưa cửa sổ đang mở ra trước màn hình
                        NativeMethods.ShowWindow(process.MainWindowHandle, NativeMethods.SW_RESTORE);
                        NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
            }
            catch { }
        }

        internal class NativeMethods
        {
            public const int SW_RESTORE = 9;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }

       
    }
}