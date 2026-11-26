namespace BudApka;

public partial class MainPage
{
    static readonly TimeSpan _saveDelay = TimeSpan.FromMilliseconds(500);
    readonly LocalStateStore _stateStore = new(FileSystem.AppDataDirectory);
    long _stateSequence;
    bool _isSaveScheduled;
    Window? _lifecycleWindow;

    void RestoreState()
    {
        var snapshot = _stateStore.Load();
        if (snapshot == null)
        {
            return;
        }

        _viewModel.RestoreSnapshot(snapshot);
        _stateStore.DeleteUnusedPhotos(snapshot.PhotoPaths());
    }

    // Collapses bursts of UI updates into one write.
    void ScheduleStateSave()
    {
        if (_isSaveScheduled)
        {
            return;
        }

        _isSaveScheduled = true;
        Dispatcher.DispatchDelayed(_saveDelay, () =>
        {
            _isSaveScheduled = false;
            var snapshot = _viewModel.CreateSnapshot();
            long sequence = ++_stateSequence;
            _ = Task.Run(() => _stateStore.Save(snapshot, sequence));
        });
    }

    void SaveStateNow() => _stateStore.Save(_viewModel.CreateSnapshot(), ++_stateSequence);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Window == null || Window == _lifecycleWindow)
        {
            return;
        }

        if (_lifecycleWindow != null)
        {
            _lifecycleWindow.Stopped -= OnWindowStopped;
            _lifecycleWindow.Destroying -= OnWindowStopped;
        }

        _lifecycleWindow = Window;
        // Text fields update the view model without re-rendering, so save on leaving too.
        _lifecycleWindow.Stopped += OnWindowStopped;
        _lifecycleWindow.Destroying += OnWindowStopped;
    }

    void OnWindowStopped(object? sender, EventArgs args) => SaveStateNow();
}
