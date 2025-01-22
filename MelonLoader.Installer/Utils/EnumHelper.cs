using System.ComponentModel;

namespace MelonLoader.Installer.Utils;

/// <summary>
/// Class for converting Description annotations into string
/// </summary>
public static class EnumHelper
{
    public static string? GetDescription<T>(this T enumValue) 
        where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
            return null;

        var description = enumValue.ToString();
        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

        if (fieldInfo == null)
            return description;

        var attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
        if (attrs.Length > 0)
            description = ((DescriptionAttribute)attrs[0]).Description;

        return description;
    }
}
