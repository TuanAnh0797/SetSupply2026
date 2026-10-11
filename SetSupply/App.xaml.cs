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
            services.AddSingleton<ProductionDataService>();
            services.AddSingleton<ModelGroupService>();
            services.AddSingleton<ProductionPlanService>();
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
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""ModelGroups"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ModelGroups"" PRIMARY KEY AUTOINCREMENT,
                    ""Name"" TEXT NOT NULL,
                    ""Prefixes"" TEXT NOT NULL DEFAULT '',
                    ""PlanShift1"" INTEGER NOT NULL DEFAULT 0,
                    ""PlanShift2"" INTEGER NOT NULL DEFAULT 0,
                    ""PlanShift3"" INTEGER NOT NULL DEFAULT 0,
                    ""SortOrder"" INTEGER NOT NULL DEFAULT 0
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

                // Seed nhóm "Khác" (gom các model không thuộc nhóm nào)
                if (!dbContext.ModelGroups.Any())
                {
                    dbContext.ModelGroups.Add(new ModelGroup { Name = ModelGroupService.OtherGroupName, SortOrder = 999 });
                    await dbContext.SaveChangesAsync();
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