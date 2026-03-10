using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Interface.language;

public interface ILocalizationService
{
    string GetString(string key);
    void ChangeLanguage(string lang);
    public event Action<string> OnLanguageChanged;
}
