using System.Diagnostics.CodeAnalysis;

namespace MouseClicker
{
    internal static class UpdateVersion
    {
        public static Version Normalize(Version version)
        {
            ArgumentNullException.ThrowIfNull(version);
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
        }

        public static bool TryParseStable(string? value, [NotNullWhen(true)] out Version? version)
        {
            version = null;
            if (string.IsNullOrEmpty(value)) return false;
            string number = value[0] is 'v' or 'V' ? value[1..] : value;
            string[] components = number.Split('.');
            if (components.Length is not (3 or 4) || components.Any(component =>
                component.Length == 0 ||
                (component.Length > 1 && component[0] == '0') ||
                component.Any(character => character < '0' || character > '9')))
            {
                return false;
            }

            if (!Version.TryParse(number, out Version? parsed)) return false;
            version = Normalize(parsed);
            return true;
        }
    }
}
