using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Models.Database;


public partial class User : ObservableObject
{
    public string UserID { get; set; }
    [ObservableProperty]
    public string userName;
    [ObservableProperty]
    public string passWord;
    [ObservableProperty]
    public string role;
}

