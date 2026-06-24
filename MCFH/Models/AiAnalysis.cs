using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class AiAnalysis
{
    public int AnalysisId { get; set; }

    public int FeedbackId { get; set; }

    public string? MainSentiment { get; set; }

    public double? ConfidenceScore { get; set; }

    public bool? IsCrisisAlert { get; set; }

    public int? SentimentOverrideBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public double? AgreementRate { get; set; }

    public virtual ScrapedFeedback Feedback { get; set; } = null!;

    public virtual ICollection<FeedbackAspect> FeedbackAspects { get; set; } = new List<FeedbackAspect>();

    public virtual User? SentimentOverrideByNavigation { get; set; }
}
