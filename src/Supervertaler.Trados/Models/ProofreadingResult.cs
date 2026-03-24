using System;
using System.Collections.Generic;
using System.Linq;

namespace Supervertaler.Trados.Models
{
    public class ProofreadingIssue
    {
        public int SegmentIndex { get; set; }       // 0-based
        public int SegmentNumber { get; set; }       // 1-based display
        public string SourceText { get; set; }
        public string TargetText { get; set; }
        public bool IsOk { get; set; }
        public string IssueDescription { get; set; }
        public string Suggestion { get; set; }
        public object SegmentPairRef { get; set; }   // ISegmentPair or string[] for navigation
        public string ParagraphUnitId { get; set; }
        public string SegmentId { get; set; }
    }

    public class ProofreadingReport
    {
        public List<ProofreadingIssue> Issues { get; set; } = new List<ProofreadingIssue>();
        public int TotalSegmentsChecked { get; set; }
        public int IssueCount => Issues.Count(i => !i.IsOk);
        public int OkCount => Issues.Count(i => i.IsOk);
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; }
    }

    public enum BatchMode { Translate, Proofread, PostEdit }
    public enum ProofreadScope { ConfirmedOnly, TranslatedAndConfirmed, AllSegments, Filtered, FilteredConfirmedOnly }
    public enum PostEditLevel { Light, Medium, Heavy }
}
