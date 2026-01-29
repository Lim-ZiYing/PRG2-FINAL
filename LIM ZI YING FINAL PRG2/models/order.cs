namespace LIM_ZI_YING_FINAL_PRG2.models;

public class Order
{
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string RestaurantId { get; set; } = "";

    public DateTime DeliveryDateTime { get; set; }
    public string DeliveryAddress { get; set; } = "";

    public DateTime CreatedDateTime { get; set; }
    public double TotalAmount { get; set; }
    public string Status { get; set; } = "";

    private readonly List<OrderedFoodItem> _items = new();
    public IReadOnlyList<OrderedFoodItem> Items => _items;

    public void AddItem(OrderedFoodItem item) => _items.Add(item);

    public override string ToString()
        => $"{OrderId} {CustomerEmail} {RestaurantId} {DeliveryDateTime:dd/MM/yyyy HH:mm} ${TotalAmount:0.00} {Status}";
}
