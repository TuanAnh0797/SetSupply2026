using APP.Models.Database;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Service;

public partial class UserSession : ObservableObject
{
    [ObservableProperty] private User? currentUser;

    public string Role => CurrentUser?.Role ?? "Guest";
    public bool IsLoggedIn => CurrentUser != null;
}
