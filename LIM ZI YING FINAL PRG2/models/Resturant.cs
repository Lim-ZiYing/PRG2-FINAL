namespace LIM_ZI_YING_FINAL_PRG2.models;

public class Restaurant
{
    public string RestaurantId { get; }
    public string RestaurantName { get; }
    public string RestaurantEmail { get; }

    private readonly List<Menu> _menus = new();
    public IReadOnlyList<Menu> Menus => _menus;

    private readonly List<SpecialOffer> _specialOffers = new();
    public IReadOnlyList<SpecialOffer> SpecialOffers => _specialOffers;

    public Queue<Order> OrderQueue { get; } = new();

    public Restaurant(string id, string name, string email)
    {
        RestaurantId = id;
        RestaurantName = name;
        RestaurantEmail = email;
    }

    public void AddMenu(Menu menu) => _menus.Add(menu);
    public void AddSpecialOffer(SpecialOffer offer) => _specialOffers.Add(offer);

    public override string ToString() => $"{RestaurantName} ({RestaurantId})";
}
