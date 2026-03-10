using APP.Database;
using APP.Models.Database;
using APP.ViewModels.FormViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Service;

public class AuthorizationService
{
    private readonly AppDbContext _db;
    private readonly UserSession _session;

    public AuthorizationService(AppDbContext db, UserSession session)
    {
        _db = db;
        _session = session;
    }

    public async Task<bool> LoginAsync(string userid, string password, bool UseRemember)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.UserID == userid && u.PassWord == password);

        if (user != null)
        {
            _session.CurrentUser = user;
            var Main = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            Main.CurrentUser = user.UserName;


            if (UseRemember)
            {
                var data = _db.RememberUser.ToList();
                foreach (RememberUser item in data)
                {
                     _db.RememberUser.Remove(item);
                }

               

                RememberUser rememberUser = new RememberUser() {UserID = user.UserID, PassWord = user.PassWord };
                await _db.RememberUser.AddAsync(rememberUser);
                _db.SaveChanges();
            }
            return true;
        }

        return false;
    }

    public void Logout()
    {
        _session.CurrentUser = null;
        var data = _db.RememberUser.ToList();
        foreach (RememberUser item in data)
        {
            _db.RememberUser.Remove(item);
        }
    }

    public bool HasRole(params string[] roles)
    {
        var current = _session.CurrentUser;
        if (current == null) return false;
        return roles.Contains(current.Role);
    }
}
