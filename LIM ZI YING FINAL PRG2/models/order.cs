namespace LIM_ZI_YING_FINAL_PRG2.models;

public class Order
{
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string RestaurantId { get; set; } = "";

    public DateTime DeliveryDateTime { get; set; }
    public string DeliveryAddress { get; set; } = "";

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;

    public string PaymentMethod { get; set; } = ""; // CC / PP / CD
    public string Status { get; set; } = "Pending"; // Pending/Preparing/Delivered/Rejected/Cancelled etc

    public string? SpecialRequest { get; set; } // only one allowed

    private readonly List<OrderedFoodItem> _items = new();
    public IReadOnlyList<OrderedFoodItem> Items => _items;

    public double TotalAmount { get; set; } // include delivery fee

    public void ClearItems() => _items.Clear();
    public void AddItem(OrderedFoodItem item) => _items.Add(item);

    public double CalculateTotal()
    {
        double itemsTotal = _items.Sum(i => i.LineTotal());
        TotalAmount = itemsTotal + 5.00; // fixed delivery fee
        return TotalAmount;
    }

    public void DisplayItems()
    {
        for (int i = 0; i < _items.Count; i++)
            Console.WriteLine($"{i + 1}. {_items[i]}");
        if (!string.IsNullOrWhiteSpace(SpecialRequest))
            Console.WriteLine($"Special request: {SpecialRequest}");
    }
}
