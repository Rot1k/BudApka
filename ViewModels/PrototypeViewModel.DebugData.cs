#if DEBUG
namespace BudApka;

internal sealed partial class PrototypeViewModel
{
    private static readonly WorkArea _testArea = new("UA80000000000093317",
        "UA80000000000093317-city", "UA80000000000126643", "Київ · Голосіївський");

    public bool HasDebugData { get; private set; }

    public void SeedDebugData()
    {
        if (HasDebugData || Mode != AccountMode.Client)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            ProfileName = "Тестовий користувач";
        }
        if (!InputRules.TryNormalizePhone(Phone, out _))
        {
            Phone = "+380500000000";
        }
        IsSignedIn = true;

        if (!IsMasterProfileActive)
        {
            IsMasterProfileActive = true;
            MasterArea = _testArea with { DistrictCode = "", DisplayName = "Київ" };
            MasterCategories.UnionWith(["plumbing", "electrical", "appliances", "handyman"]);
            MasterAbout = "Тестовий профіль майстра для перевірки сценаріїв ремонту.";
        }

        var area = MasterArea.RegionCode.Length > 0 && MasterArea.RegionCode != _testArea.RegionCode
            ? MasterArea : MasterArea.DistrictCode.Length > 0 ? MasterArea : _testArea;
        const string localMaster = LocalMasterId;
        const string anotherMaster = "debug-iryna";
        const string thirdMaster = "debug-maksym";

        AddOrder("plumbing", "Заміна крана", OrderStatus.Completed, localMaster, rating: 3);
        AddOrder("electrical", "Заміна розетки", OrderStatus.Completed, localMaster, rating: 4);
        AddOrder("appliances", "Ремонт пральної машини", OrderStatus.Completed, localMaster,
            rating: 5);
        AddOrder("handyman", "Монтаж полиці", OrderStatus.Completed, localMaster);
        AddOrder("plumbing", "Ремонт сифона", OrderStatus.Completed, anotherMaster, rating: 5);
        AddOrder("flooring", "Заміна плінтуса", OrderStatus.Completed, thirdMaster, rating: 4);
        AddOrder("painting", "Фарбування кімнати", OrderStatus.Cancelled);
        AddOrder("handyman", "Кріплення карниза", OrderStatus.Cancelled, localMaster);

        var waiting = AddOrder("appliances", "Діагностика духовки",
            OrderStatus.VisitRecorded, localMaster);
        ReportTestCompletion(waiting);
        AddOrder("electrical", "Установлення світильника",
            OrderStatus.VisitRecorded, localMaster);
        AddOrder("plumbing", "Підтікає кран", OrderStatus.MasterSelected, localMaster);
        AddOrder("windows", "Відрегулювати вікно", OrderStatus.MasterSelected, anotherMaster);
        AddOrder("handyman", "Повісити дзеркало", OrderStatus.Submitted);
        AddOrder("painting", "Пофарбувати стелю", OrderStatus.Submitted);
        var proposals = AddOrder("plumbing", "Низький тиск води", OrderStatus.HasResponses);
        proposals.Offers.AddRange([
            CreateOffer(anotherMaster, proposals.Request.Category),
            CreateOffer(thirdMaster, proposals.Request.Category),
            CreateOffer(localMaster, proposals.Request.Category),
        ]);
        MasterProposals[proposals.Request.Id] = ("Можу оглянути змішувач.", "", true);

        var plumbingService = AddService("plumbing", area);
        AddService("electrical", area);
        var recommendation = AddOrder("electrical", "Вибиває автомат", OrderStatus.HasResponses);
        var invitation = AddOrder("plumbing", "Замінити змішувач", OrderStatus.HasResponses);
        if (plumbingService.Matches(invitation.Request))
        {
            InviteForService(invitation, plumbingService);
        }

        _notifications.Add(new(proposals.Request.Id, AccountMode.Client,
            "Тестові майстри надіслали пропозиції", proposals.Request.Category));
        _notifications.Add(new(recommendation.Request.Id, AccountMode.Master,
            "Нова тестова заявка", recommendation.Request.Category));

        HasDebugData = true;
        NotifyAll();

        RepairOrder AddOrder(string categoryId, string title, OrderStatus status,
            string? masterId = null, int? rating = null)
        {
            var category = RepairCatalog.Find(categoryId)!;
            int id = _nextRequestId++;
            var request = new RepairRequest(id, categoryId, category.Name, $"Тест · {title}",
                title + ". Тестова заявка для перевірки інтерфейсу.", category.Suggestions[0],
                id % 2 == 0 ? "1500" : "", area, null);
            var order = new RepairOrder(request, $"вул. Тестова, {id - 2000}") { Status = status };
            if (status == OrderStatus.Submitted)
            {
                order.Offers.Clear();
            }
            if (masterId != null)
            {
                var offer = CreateOffer(masterId, category.Name);
                order.Offers.Add(offer);
                order.SelectedMaster = offer;
            }
            if (status is OrderStatus.MasterSelected or OrderStatus.VisitRecorded)
            {
                order.Messages.Add(new("Вітаю! Коли зручно прийняти майстра?", "10:20", true));
                order.Messages.Add(new("Можу прийти завтра після 17:00.", "10:24", false));
            }
            if (status == OrderStatus.VisitRecorded)
            {
                order.RecordedVisitText = $"{order.VisitDate:d} · {order.VisitWindow}";
            }
            if (rating is int value && masterId != null)
            {
                order.Review = new MasterReview(masterId, value,
                    "Тестовий відгук про виконану роботу.",
                    DateTime.Now.AddDays(-(id % 7)), IsDemo: true);
            }

            _orders.Add(id, order);
            return order;
        }

        MasterOffer CreateOffer(string id, string category) => id switch
        {
            anotherMaster => new(id, "Ірина · демо", category, 6,
                "Вільна завтра після 17:00.", "1 400 грн", "master_iryna.jpg"),
            thirdMaster => new(id, "Максим · демо", category, 4,
                "Спочатку уточню обсяг робіт.", "", ""),
            _ => new(id, ProfileName.Trim(), category, 0, "Можу взятися за роботу.", "", ""),
        };

        ServiceCard AddService(string categoryId, WorkArea serviceArea)
        {
            var matchingCard = ServiceCards.FirstOrDefault(card =>
                card.CategoryId == categoryId && card.Area.Includes(serviceArea));
            if (matchingCard != null)
            {
                return matchingCard;
            }

            var category = RepairCatalog.Find(categoryId)!;
            var card = new ServiceCard(_nextServiceId++, categoryId, category.Name,
                "Тестова картка послуги.", serviceArea);
            _serviceCards.Add(card);
            return card;
        }

        void ReportTestCompletion(RepairOrder order)
        {
            order.MasterReportedCompletion = true;
            _notifications.Add(new(order.Request.Id, AccountMode.Client,
                "Майстер повідомив, що тестову роботу виконано", order.Request.Category)
            {
                OpensOrderDetails = true,
            });
        }
    }
}
#endif
