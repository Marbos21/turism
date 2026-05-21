namespace FamilyTrip.Application.Integrations;

public sealed record CountryFacts(
    string CountryName,
    string? Region,
    string? Capital,
    IReadOnlyList<string> Languages,
    string? Currency);

public interface ICountryFactsService
{
    Task<CountryFacts?> GetAsync(string countryName, CancellationToken cancellationToken = default);
}
