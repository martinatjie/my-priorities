namespace PrioritizationApp.Models;

public enum ComparisonOutcome
{
    PreferFirst,
    PreferSecond,
    Skip,
    Cancel
}

public record PrioritizationSessionResult(List<Guid> RankedItemIds, bool Cancelled);

public record InsertResult(List<Guid> RankedItemIds, bool Skipped, bool Cancelled);
