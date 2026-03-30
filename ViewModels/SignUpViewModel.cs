using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class SignUpViewModel : INotifyPropertyChanged
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isPasswordHidden = true;
    private bool _isConfirmPasswordHidden = true;
    private readonly IAuthApiClient _authApiClient;

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSignUpEnabled));
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
            OnPropertyChanged(nameof(IsSignUpEnabled));
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (_confirmPassword == value) return;
            _confirmPassword = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSignUpEnabled));
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

    public bool IsSignUpEnabled =>
        !string.IsNullOrWhiteSpace(Email)
        && EmailRegex.IsMatch(Email.Trim())
        && !string.IsNullOrWhiteSpace(Password)
        && Password.Length >= 6
        && !string.IsNullOrWhiteSpace(ConfirmPassword)
        && string.Equals(Password, ConfirmPassword, StringComparison.Ordinal);
    public bool IsConfirmPasswordHidden
    {
        get => _isConfirmPasswordHidden;
        set
        {
            if (_isConfirmPasswordHidden == value) return;
            _isConfirmPasswordHidden = value;
            OnPropertyChanged();
        }
    }

    public ICommand BackCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand ToggleConfirmPasswordVisibilityCommand { get; }
    public ICommand SignUpCommand { get; }
    public ICommand OpenLoginCommand { get; }

    public SignUpViewModel(IAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        TogglePasswordVisibilityCommand = new Command(() => IsPasswordHidden = !IsPasswordHidden);
        ToggleConfirmPasswordVisibilityCommand = new Command(() => IsConfirmPasswordHidden = !IsConfirmPasswordHidden);
        OpenLoginCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        SignUpCommand = new Command(async () =>
        {
            if (!await ValidateInputAsync())
                return;

            var result = await _authApiClient.RegisterAsync(new RegisterRequestDto(Email.Trim(), Password, Email.Trim()));
            if (result is null)
            {
                if (Shell.Current is not null)
                    await Shell.Current.DisplayAlert("Sign up failed", "Unable to create account with the provided details.", "OK");
                return;
            }

            AuthStateService.IsLoggedIn = true;
            UserProfileService.Email = Email.Trim();
            UserProfileService.FullName = Email.Trim();
            await Shell.Current.GoToAsync("//ExplorePage");
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
                await Shell.Current.DisplayAlert("Validation", "Please enter password.", "OK");
            return false;
        }

        if (Password.Length < 6)
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Validation", "Password must be at least 6 characters.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Validation", "Please confirm password.", "OK");
            return false;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Validation", "Password and confirm password do not match.", "OK");
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
