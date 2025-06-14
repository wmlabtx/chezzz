using Chezzz.Properties;
using System.Configuration;

namespace Chezzz;

public class IntSetting(string settingName, IEnumerable<int> allowedValues)
{
    private readonly string _settingName = settingName;
    private readonly int[] _allowedValues = [.. allowedValues.OrderBy(v => v)];

    public int GetValue()
    {
        try {
            var value = Convert.ToInt32(Settings.Default[_settingName], System.Globalization.CultureInfo.InvariantCulture);
            return Array.BinarySearch(_allowedValues, value) >= 0 ? value : FindClosestAllowedValue(value);
        }
        catch (Exception) {
            return GetDefaultValue();
        }
    }

    public void SetValue(int value)
    {
        if (Array.BinarySearch(_allowedValues, value) < 0) {
            value = FindClosestAllowedValue(value);
        }

        Settings.Default[_settingName] = value;
        Settings.Default.Save();
    }

    public bool CanIncreaseValue()
    {
        return GetValue() < _allowedValues.Last();
    }

    public bool CanDescreaseValue()
    {
        return GetValue() > _allowedValues.First();
    }

    public int ChangeValue(int delta)
    {
        var value = GetValue();
        var index = Array.BinarySearch(_allowedValues, value);
        if (index < 0) {
            index = ~index;
        }

        index = Math.Max(0, Math.Min(index + delta, _allowedValues.Length - 1));
        value = _allowedValues[index];
        SetValue(value);
        return value;
    }

    private int FindClosestAllowedValue(int value)
    {
        return _allowedValues.MinBy(v => Math.Abs(v - value));
    }

    private int GetDefaultValue()
    {
        try {
            var settingsType = Settings.Default.GetType();
            var propertyInfo = settingsType.GetProperty(_settingName);

            if (propertyInfo != null) {
                if (propertyInfo.GetCustomAttributes(typeof(DefaultSettingValueAttribute), false)
                        .FirstOrDefault() is DefaultSettingValueAttribute attribute) {
                    return Convert.ToInt32(attribute.Value, System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return _allowedValues[0];
        }
        catch {
            return _allowedValues[0];
        }
    }
}