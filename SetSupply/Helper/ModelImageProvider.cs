using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SetSupply.Helper;

/// <summary>
/// Ảnh thumbnail model, đặt trong thư mục "Resources\ModelImages" cạnh file exe.
/// Tên file = tên model (VD: NR-C33ES2-C.png), không có thì thử tên nhóm (VD: NR-C33ES.png).
/// </summary>
public static class ModelImageProvider
{
    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".bmp" };
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static string ImageFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ModelImages");

    public static ImageSource? Get(string model, string groupName)
    {
        return Load(model) ?? Load(groupName);
    }

    public static void ClearCache() => Cache.Clear();

    private static ImageSource? Load(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (Cache.TryGetValue(name, out var cached)) return cached;

        ImageSource? image = null;
        try
        {
            var safeName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
            var path = Extensions
                .Select(ext => Path.Combine(ImageFolder, safeName + ext))
                .FirstOrDefault(File.Exists);

            if (path != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelHeight = 64;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                image = bitmap;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load model image error ({name}): {ex.Message}");
        }

        Cache[name] = image;
        return image;
    }
}
