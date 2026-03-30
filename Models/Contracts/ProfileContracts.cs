namespace TravelApp.Models.Contracts;

public record ProfileDto(
    string Email,
    string FullName,
    string CountryCode,
    string PhoneNumber,
    string PreferredLanguage = "en");

public record UpdateProfileRequestDto(
    string Email,
    string FullName,
    string CountryCode,
    string PhoneNumber,
    string PreferredLanguage = "en");
