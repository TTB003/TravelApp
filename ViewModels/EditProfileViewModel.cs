using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class EditProfileViewModel : INotifyPropertyChanged
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private string _email = string.Empty;
    private string _fullName = string.Empty;
    private string _countryCode = "+971";
    private string _phoneNumber = string.Empty;
    private readonly IProfileApiClient _profileApiClient;

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUpdateEnabled));
            OnPropertyChanged(nameof(UpdateButtonColor));
        }
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            if (_fullName == value) return;
            _fullName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUpdateEnabled));
            OnPropertyChanged(nameof(UpdateButtonColor));
        }
    }

    public string CountryCode
    {
        get => _countryCode;
        set
        {
            if (_countryCode == value) return;
            _countryCode = value;
            OnPropertyChanged();
        }
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (_phoneNumber == value) return;
            _phoneNumber = value;
            OnPropertyChanged();
        }
    }

    public bool IsUpdateEnabled =>
        !string.IsNullOrWhiteSpace(Email)
        && EmailRegex.IsMatch(Email.Trim())
        && !string.IsNullOrWhiteSpace(FullName);

    public Color UpdateButtonColor => IsUpdateEnabled ? Color.FromArgb("#E31667") : Color.FromArgb("#D7DCEA");

    public ICommand BackCommand { get; }
    public ICommand UpdateProfileCommand { get; }
    public ICommand DeleteAccountCommand { get; }

    public EditProfileViewModel(IProfileApiClient profileApiClient)
    {
        _profileApiClient = profileApiClient;
        Email = UserProfileService.Email;
        FullName = UserProfileService.FullName;
        CountryCode = UserProfileService.CountryCode;
        PhoneNumber = UserProfileService.PhoneNumber;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        UpdateProfileCommand = new Command(async () => await UpdateProfileAsync());
        DeleteAccountCommand = new Command(async () => await DeleteAccountAsync());
    }

    private async Task UpdateProfileAsync()
    {
        if (!IsUpdateEnabled)
            return;

        var request = new UpdateProfileRequestDto(
            Email.Trim(),
            FullName.Trim(),
            CountryCode.Trim(),
            PhoneNumber.Trim(),
            UserProfileService.PreferredLanguage);

        var isSuccess = await _profileApiClient.UpdateMyProfileAsync(request);
        if (!isSuccess)
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Error", "Failed to update profile.", "OK");
            return;
        }

        UserProfileService.Email = request.Email;
        UserProfileService.FullName = request.FullName;
        UserProfileService.CountryCode = request.CountryCode;
        UserProfileService.PhoneNumber = request.PhoneNumber;

        if (Shell.Current is not null)
            await Shell.Current.DisplayAlert("Success", "Profile updated.", "OK");
    }

    private async Task DeleteAccountAsync()
    {
        if (Shell.Current is null)
            return;

        var confirm = await Shell.Current.DisplayAlert("Delete account", "Are you sure you want to delete this account?", "Delete", "Cancel");
        if (!confirm)
            return;

        AuthStateService.IsLoggedIn = false;
        await Shell.Current.GoToAsync("//ExplorePage");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
