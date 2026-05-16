using System;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;


namespace CSD.Models
{
    internal static class RandomPickerSettings
    {
        private const string EnabledKey = "randomPicker.enabled";
        private const string MinNumberKey = "randomPicker.minNumber";
        private const string MaxNumberKey = "randomPicker.maxNumber";
        private const string DefaultCountKey = "randomPicker.defaultCount";
        private const string AnimationKey = "randomPicker.animation";

        public static bool IsEnabled
        {
            get
            {
                if (AppSettings.Values.TryGetValue(EnabledKey, out var value) && value is bool b)
                    return b;
                return true;
            }
        }

        public static int MinNumber
        {
            get
            {
                if (AppSettings.Values.TryGetValue(MinNumberKey, out var value))
                    return Convert.ToInt32(value);
                return 1;
            }
        }

        public static int MaxNumber
        {
            get
            {
                if (AppSettings.Values.TryGetValue(MaxNumberKey, out var value))
                    return Convert.ToInt32(value);
                return 60;
            }
        }

        public static int DefaultCount
        {
            get
            {
                if (AppSettings.Values.TryGetValue(DefaultCountKey, out var value))
                    return Convert.ToInt32(value);
                return 1;
            }
        }

        public static bool AnimationEnabled
        {
            get
            {
                if (AppSettings.Values.TryGetValue(AnimationKey, out var value) && value is bool b)
                    return b;
                return true;
            }
        }
    }
}
