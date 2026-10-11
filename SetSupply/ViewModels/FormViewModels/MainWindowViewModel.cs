using APP.Database;
using APP.Interface.language;
using APP.Models.Database;
using APP.Service;
using APP.UserControls;
using APP.Views;
using SetSupply.Service;
using APP.ViewModels.UserControlViewModels.Setting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.UserControls.DashBoard;
using SetSupply.UserControls.History;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace APP.ViewModels.FormViewModels
{
    public partial class MainWindowViewModel: ObservableObject
    {


        // Tên ứng dụng: đổi được ở Cài đặt > General (bảng AppSettings)
        [ObservableProperty]
        private string applicationTitle = AppSettingService.DefaultApplicationTitle;

        [ObservableProperty]
        private UserControl currentView;
        [ObservableProperty]
        private string selectedpage = "Home";
        [ObservableProperty]
        private string pageIcon = "Home";
        [ObservableProperty]
        private string currentUser = "Guest";
        [ObservableProperty]
        private Visibility isLoginVisibility =  Visibility.Visible;
        [ObservableProperty]
        private Visibility isLogoutVisibility = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility isAdmin = Visibility.Collapsed;
        [ObservableProperty]
        private string avatar = "G";


        [ObservableProperty]
        private string statusConnectionPLC = "Disconnected";

        // Thanh tiêu đề: phụ đề + điều khiển riêng của trang (Home: ngày / ca / cài đặt)
        [ObservableProperty]
        private string pageSubtitle = HomeSubtitle;
        [ObservableProperty]
        private object? headerToolbar;
        [ObservableProperty]
        private Visibility pageHeaderVisibility = Visibility.Collapsed;

        private const string HomeSubtitle = "Sản lượng theo nhóm model / model";



        private ILocalizationService _localizationService;
        private UserSession _userSession;
        private AuthorizationService _authorizationService;
        private readonly AppDbContext _db;

        private readonly PLCService _plcService;

        public MainWindowViewModel(ILocalizationService localizationService, UserSession userSession, AuthorizationService authorizationService, AppDbContext db, PLCService pLCService, AppSettingService appSettingService)
        {
            ApplicationTitle = appSettingService.ApplicationTitle;
            appSettingService.SettingChanged += (key, value) =>
            {
                if (key == AppSettingService.ApplicationTitleKey)
                    Application.Current?.Dispatcher.Invoke(() => ApplicationTitle = value);
            };

            _db = db;
            _localizationService = localizationService;


            CurrentView = App.ServiceProvider.GetRequiredService<UCHome>();
            Selectedpage = _localizationService.GetString("Home");
            _authorizationService = authorizationService;
            _userSession = userSession;
            _userSession.PropertyChanged += _userSession_PropertyChanged;


            _plcService = pLCService;

            // Đăng ký trước các lệnh return bên dưới (bản cũ không đăng ký khi chưa đăng nhập => luôn hiện "Connected")
            _plcService.ConnectionChanged += _plcService_ConnectionChanged;
            _plcService_ConnectionChanged(_plcService.IsConnected);

            if (currentUser != null)
            {
                if (_userSession.Role == "Admin")
                {
                    IsAdmin = Visibility.Visible;
                }
                else
                {
                    IsAdmin = Visibility.Collapsed;
                }
                if (_userSession.CurrentUser == null)
                {
                    Avatar = "G";
                    return;
                }
                Avatar = _userSession.CurrentUser.UserName.Split(' ').Last().Substring(0, 1).ToUpper();


            }
            else
            {
                IsAdmin = Visibility.Collapsed;
            }
        }

        private void _plcService_ConnectionChanged(bool obj)
        {
           if (obj)
            {
                StatusConnectionPLC = "Connected";
            }
           else
            {
                StatusConnectionPLC = "Disconnected";
            }
        }

        private void _userSession_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
              ShowSettingCommand.NotifyCanExecuteChanged();
              OpenModelGroupSettingCommand.NotifyCanExecuteChanged();

            if (sender != null)
            {
               UserSession userSession = sender as UserSession;
                if (userSession.Role == "Admin" )
                {
                    IsAdmin = Visibility.Visible;
                }
                else
                {
                    IsAdmin = Visibility.Collapsed;
                }
                if (_userSession.CurrentUser == null)
                {
                    Avatar = "G";
                    return;
                }
                Avatar = _userSession.CurrentUser.UserName.Split(' ').Last().Substring(0, 1).ToUpper();
            }
            else
            {
                IsAdmin = Visibility.Collapsed;
            }
        }

        private bool CanTool() => _authorizationService.HasRole( "Admin","Operater");
        private bool CanSetting() => _authorizationService.HasRole("Admin");


        [RelayCommand]
        private void ShowHome()
        {
            CurrentView = App.ServiceProvider.GetRequiredService<UCHome>();
            Selectedpage = _localizationService.GetString("Home");
            PageIcon = "Home";
            

        }
        [RelayCommand]
        private void ShowDashBoard()
        {
            CurrentView = App.ServiceProvider.GetRequiredService<UCDashboard>();
            Selectedpage = _localizationService.GetString("DashBoard");
            PageIcon = "MonitorDashboard";

        }
        [RelayCommand(CanExecute = nameof(CanSetting))]
        private void ShowSetting()
        {
            CurrentView = App.ServiceProvider.GetRequiredService<UCSetting>();
            Selectedpage = _localizationService.GetString("Setting");
            PageIcon = "CogOutline";
        }
        /// <summary>
        /// Nút "Cài đặt" trên header Home: mở Cài đặt > Nhóm model.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSetting))]
        private void OpenModelGroupSetting()
        {
            ShowSetting();
            App.ServiceProvider.GetRequiredService<UCSettingViewModel>().ShowModelGroupSettingCommand.Execute(null);
        }

        partial void OnCurrentViewChanged(UserControl value)
        {
            bool isHome = value is UCHome;
            HeaderToolbar = isHome ? App.ServiceProvider.GetRequiredService<UCHomeHeader>() : null;
            PageHeaderVisibility = isHome ? Visibility.Collapsed : Visibility.Visible;
            PageSubtitle = isHome ? HomeSubtitle : string.Empty;
        }

        [RelayCommand]
        private void ShowHistory()
        {
            CurrentView = App.ServiceProvider.GetRequiredService<UCHistory>();
            Selectedpage = _localizationService.GetString("History");
            PageIcon = "History";
        }


        [RelayCommand]
        private void ShowHelp()
        {
            CurrentView = App.ServiceProvider.GetRequiredService<UCHelp>();
            Selectedpage = _localizationService.GetString("Help");
            PageIcon = "Help";
          
        }
        [RelayCommand]
        private void ChangeLanguage(string language = "vi")
        {
            language = "vi";
            string dictPath = $"Resources/Language/StringResources.{language}.xaml";
            var dicts = Application.Current.Resources.MergedDictionaries;
            var oldDict = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("StringResources."));
            if (oldDict != null)
            {
                int index = dicts.IndexOf(oldDict);
                dicts[index] = new ResourceDictionary() { Source = new Uri(dictPath, UriKind.Relative) };
                _localizationService.ChangeLanguage(language);
            }
            else
            {
                dicts.Add(new ResourceDictionary() { Source = new Uri(dictPath, UriKind.Relative) });
                _localizationService.ChangeLanguage(language);
            }
        }
        [RelayCommand]
        private void Login()
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
        }

        [RelayCommand]
        private void Logout()
        {
            _authorizationService.Logout();
            CurrentUser = "Guest";
            CurrentView = App.ServiceProvider.GetRequiredService<UCHome>();
            Selectedpage = _localizationService.GetString("Home");
            PageIcon = "Home";

        }
        [RelayCommand(CanExecute = nameof(CanSetting))]
        private void UserManagementMethod()
        {
           UserManagement  userManagement = new UserManagement();
           userManagement.ShowDialog();
        }

        

        partial void OnCurrentUserChanged(string value)
        {
           if ( value == "Guest")
           {
                IsLoginVisibility = Visibility.Visible;
                IsLogoutVisibility = Visibility.Collapsed;
            }
           else
           {
                IsLoginVisibility = Visibility.Collapsed;
                IsLogoutVisibility = Visibility.Visible;
            }
        }
    }
}

