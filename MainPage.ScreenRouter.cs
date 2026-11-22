namespace BudApka;

public partial class MainPage
{
    readonly Stack<string> _backStack = new();
    AccountMode _lastRenderedMode;
    bool _returning;

    string ModeHome => _viewModel.Mode == AccountMode.Client ? "Home"
        : _viewModel.IsMasterProfileActive ? "AvailableRequests" : "MasterActivation";

    bool CanGoBack => _backStack.Count > 0
        || (_viewModel.Mode == AccountMode.Master && _viewModel.CurrentScreen == "MasterActivation");

    protected override bool OnBackButtonPressed() => GoBack() || base.OnBackButtonPressed();

    bool GoBack()
    {
        if (_backStack.TryPop(out string? previous))
        {
            _returning = true;
            _viewModel.CurrentScreen = previous;
            return true;
        }

        if (CanGoBack)
        {
            _viewModel.SetMode(AccountMode.Client);
            return true;
        }

        return false;
    }

    void NavigateTab(string screen)
    {
        if (screen == _viewModel.CurrentScreen)
        {
            return;
        }

        _backStack.Clear();
        if (screen != ModeHome)
        {
            _backStack.Push(ModeHome);
        }
        _returning = true;
        _viewModel.CurrentScreen = screen;
    }

    void TrackNavigation(string screen)
    {
        string? previous = _lastRenderedScreen;
        if (previous == null || _lastRenderedMode != _viewModel.Mode)
        {
            _backStack.Clear();
            _lastRenderedMode = _viewModel.Mode;
            _returning = false;
            return;
        }

        if (_returning)
        {
            _returning = false;
            return;
        }

        // Once an action is committed, Back must not reopen its submission form.
        string? safeParent = screen switch
        {
            "Submitted" => "Home",
            "ProposalSent" => "AvailableRequests",
            "Chat" when previous is "Responses" or "MasterProfile" => "Orders",
            "Orders" when previous is "VisitDetails" or "Completion" or "Submitted" => ModeHome,
            _ => null,
        };
        if (safeParent != null)
        {
            _backStack.Clear();
            if (safeParent != ModeHome)
            {
                _backStack.Push(ModeHome);
            }
            _backStack.Push(safeParent);
        }
        else if (_backStack.Contains(screen))
        {
            while (_backStack.Pop() != screen)
            {
            }
        }
        else
        {
            _backStack.Push(previous);
        }
    }

    View Screen() =>
        _viewModel.CurrentScreen switch
        {
            "NewRequest" => NewRequest(),
            "Address" => Address(),
            "ReviewRequest" => ReviewRequest(),
            "SignIn" => SignIn(),
            "Submitted" => Submitted(),
            "Responses" => Responses(),
            "MasterProfile" => MasterProfile(),
            "Chat" => Chat(),
            "VisitDetails" => Visit(),
            "Completion" => Completion(),
            "Orders" => Orders(),
            "Profile" => Profile(),
            "MasterActivation" => ActivateMaster(),
            "AvailableRequests" => Available(),
            "MasterRequest" => MasterRequest(),
            "Proposal" => Proposal(),
            "ProposalSent" => ProposalSentScreen(),
            "MasterCompleted" => MasterCompleted(),
            "Equipment" => Equipment(),
            "EquipmentForm" => EquipmentForm(),
            "AttachEquipment" => AttachEquipment(),
            "MasterFilters" => MasterFilters(),
            _ => Home(),
        };
}
