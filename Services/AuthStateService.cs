namespace TravelApp.Services;

public static class AuthStateService
{
    private static bool _isLoggedIn;

    public static bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn == value) return;
            _isLoggedIn = value;
            AuthStateChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static event EventHandler? AuthStateChanged;
}
