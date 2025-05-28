using System;
using System.Collections.Generic;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.Services;

public class ConversionManager : IConversionManager
{
    private List<ConvertibleVideoModel?> _convertibleVideoModels = new ();
    public IReadOnlyList<ConvertibleVideoModel?> ConversionCandidates => _convertibleVideoModels;

    public ConversionManager()
    {
    }

    public void AddToConversionCandidates(ConvertibleVideoModel? conversionCandidate)
    {
        if (conversionCandidate == null)
            throw new ArgumentNullException(nameof(conversionCandidate));
        
        if (!_convertibleVideoModels.Contains(conversionCandidate))
            _convertibleVideoModels.Add(conversionCandidate);
        
    }

    public void RemoveFromConversionCandidates(ConvertibleVideoModel? conversionCandidate)
    {
        if (conversionCandidate == null)
            return;
        
        if (_convertibleVideoModels.Contains(conversionCandidate))
            _convertibleVideoModels.Remove(conversionCandidate);
    }
}