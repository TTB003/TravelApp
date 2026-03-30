namespace TravelApp.Services;

public static class UserProfileService
{
    private static string _email = "voquockhanh2009@gmail.com";
    private static string _fullName = "Võ Quốc Khánh (Drakew)";
    private static string _phoneNumber = string.Empty;
    private static string _countryCode = "+971";
    private static string _preferredLanguage = "en";

    public static string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string FullName
    {
        get => _fullName;
        set
        {
            if (_fullName == value) return;
            _fullName = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (_phoneNumber == value) return;
            _phoneNumber = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string CountryCode
    {
        get => _countryCode;
        set
        {
            if (_countryCode == value) return;
            _countryCode = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string PreferredLanguage
    {
        get => _preferredLanguage;
        set
        {
            if (_preferredLanguage == value) return;
            _preferredLanguage = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static event EventHandler? ProfileChanged;
}
