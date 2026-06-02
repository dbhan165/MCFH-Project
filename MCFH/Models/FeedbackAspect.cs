using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class FeedbackAspect
{
    public int AspectId { get; set; }

    public int AnalysisId { get; set; }

    public string Category { get; set; } = null!;

    public string? Sentiment { get; set; }

    public double? ConfidenceScore { get; set; }

    public virtual AiAnalysis Analysis { get; set; } = null!;
}
