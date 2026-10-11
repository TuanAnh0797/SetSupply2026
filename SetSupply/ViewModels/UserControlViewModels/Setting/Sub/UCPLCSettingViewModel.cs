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
        // Kiểm tra trước khi lưu (cấu hình sai làm vòng giao tiếp PLC lỗi liên tục)
        if (!System.Net.IPAddress.TryParse(IPAddress?.Trim(), out _))
        {
            MessageBox.Show("Địa chỉ IP không hợp lệ (VD: 192.168.1.10).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(Port, out int port) || port < 1 || port > 65535)
        {
            MessageBox.Show("Port phải là số từ 1 đến 65535.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(AddressData, out int d) || d < 0 ||
            !int.TryParse(AddressTrigger, out int trigger) || trigger < 0 ||
            !int.TryParse(AddressAlive, out int alive) || alive < 0)
        {
            MessageBox.Show("Địa chỉ D / M phải là số nguyên không âm.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        int minWords = (PLCService.CodeLength + 1) / 2;
        if (!int.TryParse(TotalRegisterData, out int words) || words < minWords || words > 960)
        {
            MessageBox.Show($"Số word dữ liệu phải từ {minWords} đến 960 (mã gồm {PLCService.CodeLength} ký tự, 2 ký tự/word).",
                "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (trigger == alive)
        {
            MessageBox.Show("Bit Trigger và bit Alive không được trùng nhau.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var data = _db.PLCSetting.ToList();
            foreach (var item in data)
                _db.PLCSetting.Remove(item);

            var setting = new PLCSetting
            {
                Name = "SetSuply",
                IPAddress = IPAddress.Trim(),
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

