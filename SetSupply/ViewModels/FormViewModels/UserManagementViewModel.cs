using APP.Database;
using APP.Models.Database;
using APP.ViewModels.UserControlViewModels.Setting.Sub;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace APP.ViewModels.FormViewModels;

 public partial class UserManagementViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    [ObservableProperty]
    ObservableCollection<User> users = new ObservableCollection<User>();
    [ObservableProperty]
    private List<string> roles = new List<string>() {"Admin", "Operater" };
    [ObservableProperty]
    private Visibility isShowAdd = Visibility.Collapsed;
    [ObservableProperty]
    string roleNew = "Operater";
    [ObservableProperty]
    string userID;
    [ObservableProperty]
    string userName;
    [ObservableProperty]
    string passWord;

    public UserManagementViewModel(AppDbContext db)
    {
        _db = db;
        Reload();
    }

    private void Reload()
    {
        Users.Clear();
        var data = _db.Users.ToList();
        foreach (var item in data)
        {
            Users.Add(item);
        }
    }
    [RelayCommand]
    private async Task Update(User user)
    {
        var datachange = await _db.Users.FirstAsync(p => p.UserID == user.UserID);
        datachange.UserName = user.UserName;
        datachange.PassWord = user.PassWord;
        datachange.Role = user.Role;
        _db.SaveChanges();
        Reload();
       
    }

    [RelayCommand]
    private async Task DeleteAsync(User user)
    {
        var datachange = await _db.Users.FirstAsync(p => p.UserID == user.UserID);
        _db.Users.Remove(datachange);
        _db.SaveChanges();
        Reload();
        
    }
    [RelayCommand]
    private void ShowAdd()
    {
        IsShowAdd = Visibility.Visible;
    }

    [RelayCommand]
    private void CloseAdd()
    {
        IsShowAdd = Visibility.Collapsed;
    }
    [RelayCommand]
    private async Task SaveAdd()
    {
        try
        {
            
            User newusser = new User()
            {
                UserID = UserID,
                UserName = UserName,
                PassWord = PassWord,
                Role = RoleNew
            };
            await _db.Users.AddAsync(newusser);
            await _db.SaveChangesAsync();
            Reload();
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            MessageBox.Show(ex.Message);
        }
        
      
       
    }



}
