using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _isPasswordHidden = true;
    private readonly IAuthApiClient _authApiClient;

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSignInEnabled));
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value) return;
            _password = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSignInEnabled));
        }
    }

    public bool IsPasswordHidden
    {
        get => _isPasswordHidden;
        set
        {
            if (_isPasswordHidden == value) return;
            _isPasswordHidden = value;
            OnPropertyChanged();
        }
    }

    public bool IsSignInEnabled =>
        !string.IsNullOrWhiteSpace(Email)
        && EmailRegex.IsMatch(Email.Trim())
        && !string.IsNullOrWhiteSpace(Password);
    public ICommand BackCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand SignInCommand { get; }
    public ICommand OpenSignUpCommand { get; }

    public LoginViewModel(IAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        TogglePasswordVisibilityCommand = new Command(() => IsPasswordHidden = !IsPasswordHidden);
        OpenSignUpCommand = new Command(async () => await Shell.Current.GoToAsync("SignUpPage"));
        SignInCommand = new Command(async () =>
        {
            if (!await ValidateInputAsync())
                return;

            var result = await _authApiClient.LoginAsync(new LoginRequestDto(Email.Trim(), Password));
            if (result is null)
            {
                if (Shell.Current is not null)
                    await Shell.Current.DisplayAlert("Sign in failed", "Unable to sign in with current credentials.", "OK");
                return;
            }

            AuthStateService.IsLoggedIn = true;
            await Shell.Current.GoToAsync("..");
        });
    }

    private async Task<bool> ValidateInputAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Validation", "Please enter your e-mail address.", "OK");
            return false;
        }

        if (!EmailRegex.IsMatch(Email.Trim()))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Validation", "Please enter a valid e-mail address.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Validation", "Please enter your password.", "OK");
            return false;
        }

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
