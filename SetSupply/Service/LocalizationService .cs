using APP.Interface.language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace APP.Service;

public class LocalizationService: ILocalizationService
{
    private ResourceDictionary _dict;

    public event Action<string> OnLanguageChanged;


    public LocalizationService(string lang = "vi")
    {
        ChangeLanguage(lang);
    }

    public string GetString(string key)
    {
        if (_dict.Contains(key)) return _dict[key].ToString();
        return $"[{key}]";
    }

    public void ChangeLanguage(string lang)
    {
        string source = $"Resources/Language/StringResources.{lang}.xaml";
        _dict = new ResourceDictionary() { Source = new Uri(source, UriKind.Relative) };
        OnLanguageChanged?.Invoke(lang);
    }
}
