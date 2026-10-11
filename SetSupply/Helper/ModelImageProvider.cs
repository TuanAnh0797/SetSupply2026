using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SetSupply.Helper;

/// <summary>
/// Ảnh thumbnail model. Thứ tự tìm:
/// 1. Đường dẫn ảnh khai báo ở Model Group (đường dẫn đầy đủ, hoặc tên file trong "Resources\ModelImages").
/// 2. "Resources\ModelImages\&lt;tên model&gt;.png|jpg|jpeg|bmp" cạnh file exe.
/// 3. "Resources\ModelImages\&lt;tên nhóm&gt;.png|..."
/// </summary>
public static class ModelImageProvider
{
    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".bmp" };
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static string ImageFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ModelImages");

    public static ImageSource? Get(string model, string groupName, string? imagePath = null)
    {
        return LoadPath(imagePath) ?? Load(model) ?? Load(groupName);
    }

    /// <summary>
    /// Đường dẫn đầy đủ của ảnh khai báo (tên file => trong thư mục ModelImages). Null nếu để trống.
    /// </summary>
    public static string? ResolvePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return null;
        var path = imagePath.Trim().Trim('"');
        return Path.IsPathRooted(path) ? path : Path.Combine(ImageFolder, path);
    }

    private static ImageSource? LoadPath(string? imagePath)
    {
        var path = ResolvePath(imagePath);
        if (path == null) return null;
        if (Cache.TryGetValue("path:" + path, out var cached)) return cached;

        var image = File.Exists(path) ? LoadFile(path) : null;
        Cache["path:" + path] = image;
        return image;
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
                image = LoadFile(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load model image error ({name}): {ex.Message}");
        }

        Cache[name] = image;
        return image;
    }

    private static ImageSource? LoadFile(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // đọc xong đóng file, không khóa file ảnh
            bitmap.DecodePixelHeight = 64;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load model image error ({path}): {ex.Message}");
            return null;
        }
    }
}
