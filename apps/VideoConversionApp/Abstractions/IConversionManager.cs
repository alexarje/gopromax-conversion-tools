using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IConversionManager
{
    void AddToConversionCandidates(ConvertibleVideoModel? conversionCandidate);
    void RemoveFromConversionCandidates(ConvertibleVideoModel? conversionCandidate);
}