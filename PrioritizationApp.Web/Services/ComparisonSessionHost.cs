using PrioritizationApp.Models;

namespace PrioritizationApp.Web.Services;

public class ComparisonSessionHost
{
    public bool IsActive { get; private set; }
    public Item? FirstItem { get; private set; }
    public Item? SecondItem { get; private set; }
    public string ProgressText { get; private set; } = "";

    private TaskCompletionSource<ComparisonOutcome>? _pending;

    public event Action? StateChanged;

    public ValueTask<ComparisonOutcome> PickAsync(Item first, Item second)
    {
        FirstItem = first;
        SecondItem = second;
        IsActive = true;
        _pending = new TaskCompletionSource<ComparisonOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        NotifyChanged();
        return new ValueTask<ComparisonOutcome>(_pending.Task);
    }

    public void SetProgress(string progressText)
    {
        ProgressText = progressText;
        NotifyChanged();
    }

    public void Choose(ComparisonOutcome outcome)
    {
        if (_pending is null)
            return;

        IsActive = false;
        FirstItem = null;
        SecondItem = null;
        ProgressText = "";
        _pending.TrySetResult(outcome);
        _pending = null;
        NotifyChanged();
    }

    private void NotifyChanged() => StateChanged?.Invoke();
}
