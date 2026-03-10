using APP.Database;
using APP.Models.Database;
using APP.Service;
using APP.ViewModels.FormViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace APP.ViewModels.UserControlViewModels.Setting.Sub;

public partial class UCPLCSettingViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    [ObservableProperty]
    private string iPAddress;

    [ObservableProperty]
    private string port;

    [ObservableProperty]
    private string addressData;

    [ObservableProperty]
    private string totalRegisterData;

    [ObservableProperty]
    private string addressTrigger;

    [ObservableProperty]
    private string addressConfirmFinalReadData;

    [ObservableProperty]
    private string addressAlive;

    public event Action PLCConfigChanged;

    public UCPLCSettingViewModel(AppDbContext db)
    {
        _db = db;
        GetConfigPLC();
    }

    private void GetConfigPLC()
    {
        var config = _db.PLCSetting.FirstOrDefault();
        if (config == null) return;

        IPAddress = config.IPAddress;
        Port = config.Port.ToString();
        AddressData = config.AddressData.ToString();
        TotalRegisterData = config.TotalRegisterData.ToString();
        AddressTrigger = config.AddressTrigger.ToString();
        AddressConfirmFinalReadData = config.AddressConnfirmFinalReadData.ToString();
        AddressAlive = config.AddressAlive.ToString();
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var data = _db.PLCSetting.ToList();
            foreach (var item in data)
                _db.PLCSetting.Remove(item);

            var setting = new PLCSetting
            {
                Name = "SetSuply",
                IPAddress = IPAddress,
                Port = int.Parse(Port),
                AddressData = int.Parse(AddressData),
                TotalRegisterData = int.Parse(TotalRegisterData),
                AddressTrigger = int.Parse(AddressTrigger),
                AddressConnfirmFinalReadData = 0,
                AddressAlive = int.Parse(AddressAlive),
                TimeUpdate = DateTime.Now,
            };

            _db.PLCSetting.Add(setting);
            _db.SaveChanges();
            GetConfigPLC();
            PLCConfigChanged?.Invoke();

            MessageBox.Show("Lưu cài đặt PLC thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Reload()
    {
        GetConfigPLC();
    }
}

