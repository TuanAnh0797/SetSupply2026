using APP.Service;
using APP.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace APP.ViewModels.FormViewModels;

public partial class LoginWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string userID;
    [ObservableProperty]
    private string password;
    [ObservableProperty]
    private bool isRemember = true;
    [ObservableProperty]
    private string status ;
    private AuthorizationService _authorizationService;
    public LoginWindowViewModel(AuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    [RelayCommand]
    private async Task Login()
    {
        Status = "";
        bool rs = await _authorizationService.LoginAsync(userID, password,IsRemember);
        if (rs)
        {
            WeakReferenceMessenger.Default.Send(new CloseWindowMessage());
        }
        else
        {
            Status = "Login Fail!!!!";
        }
    }

    [RelayCommand]
    private void Close()
    {
        WeakReferenceMessenger.Default.Send(new CloseWindowMessage());
    }

}
public class CloseWindowMessage { }
