using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace dotnet_mvc.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType()
            .GetMember(value.ToString(), BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault();

        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName()
            ?? value.ToString();
    }
}
