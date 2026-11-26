namespace BudApka;

public enum VisitProposalStatus
{
    Pending,
    Accepted,
    Declined,
    Withdrawn,
}

/// <summary>A new visit time suggested in chat. The current time stays until it is accepted.</summary>
public sealed record VisitProposal(AccountMode ProposedBy, DateTime Date, string Window,
    DateTime CreatedAt, VisitProposalStatus Status = VisitProposalStatus.Pending)
{
    public string Text => PrototypeViewModel.FormatVisit(Date, Window);
}

internal sealed partial class PrototypeViewModel
{
    public static string FormatVisit(DateTime date, string window) =>
        $"{date:d} · {window.Trim()}";

    public static bool HasPendingVisitProposal(RepairOrder order) =>
        order.VisitProposal?.Status == VisitProposalStatus.Pending;

    // Only the local master is a real second side in this session, so only that
    // conversation can confirm a proposal. Demo masters keep the manual note.
    public bool CanProposeVisit(RepairOrder order) =>
        order.IsLocalMasterSelected && CanChat(order) && !order.MasterReportedCompletion
        && !HasPendingVisitProposal(order);

    public bool CanRecordVisit(RepairOrder order) =>
        Mode == AccountMode.Client && !order.IsLocalMasterSelected && CanChat(order);

    public void RecordVisit(RepairOrder order, DateTime date, string window)
    {
        if (CurrentOrder != order || !CanRecordVisit(order) || !IsValidVisit(date, window))
        {
            return;
        }

        ApplyVisit(order, date, window);
        CurrentScreen = "Orders";
    }

    public bool ProposeVisit(RepairOrder order, DateTime date, string window)
    {
        if (CurrentOrder != order || !CanProposeVisit(order) || !IsValidVisit(date, window))
        {
            return false;
        }

        var proposal = new VisitProposal(Mode, date.Date, window.Trim(), DateTime.Now);
        order.VisitProposal = proposal;
        string action = order.Status == OrderStatus.VisitRecorded
            ? "Пропоную перенести візит на " : "Пропоную час візиту: ";
        AddVisitMessage(order, action + proposal.Text);
        NotifyOtherSide(order, Mode == AccountMode.Client
            ? "Клієнт пропонує новий час візиту" : "Майстер пропонує новий час візиту");
        CurrentScreen = "Chat";
        return true;
    }

    public bool RespondToVisitProposal(RepairOrder order, bool accept)
    {
        if (order.VisitProposal is not { Status: VisitProposalStatus.Pending } proposal
            || proposal.ProposedBy == Mode || !order.IsLocalMasterSelected || !CanChat(order))
        {
            return false;
        }

        order.VisitProposal = proposal with
        {
            Status = accept ? VisitProposalStatus.Accepted : VisitProposalStatus.Declined,
        };
        if (accept)
        {
            ApplyVisit(order, proposal.Date, proposal.Window);
        }

        AddVisitMessage(order, accept
            ? "Підтверджую час візиту: " + proposal.Text
            : "Не можу в запропонований час " + proposal.Text + ".");
        NotifyOtherSide(order, accept ? "Новий час візиту підтверджено"
            : "Запропонований час візиту відхилено");
        NotifyAll();
        return true;
    }

    public bool WithdrawVisitProposal(RepairOrder order)
    {
        if (order.VisitProposal is not { Status: VisitProposalStatus.Pending } proposal
            || proposal.ProposedBy != Mode || !CanChat(order))
        {
            return false;
        }

        order.VisitProposal = proposal with { Status = VisitProposalStatus.Withdrawn };
        AddVisitMessage(order, "Скасовую пропозицію часу " + proposal.Text + ".");
        NotifyAll();
        return true;
    }

    private static bool IsValidVisit(DateTime date, string window) =>
        date.Date >= DateTime.Today && !string.IsNullOrWhiteSpace(window);

    private static void ApplyVisit(RepairOrder order, DateTime date, string window)
    {
        order.VisitDate = date.Date;
        order.VisitWindow = window.Trim();
        order.RecordedVisitText = FormatVisit(order.VisitDate, order.VisitWindow);
        order.Status = OrderStatus.VisitRecorded;
    }

    private void AddVisitMessage(RepairOrder order, string text) =>
        order.Messages.Add(new(text, DateTime.Now.ToString("HH:mm"),
            Mode == AccountMode.Client));

    private void NotifyOtherSide(RepairOrder order, string title)
    {
        var recipient = Mode == AccountMode.Client ? AccountMode.Master : AccountMode.Client;
        _notifications.Add(new(order.Request.Id, recipient, title, order.Request.Category));
    }
}
