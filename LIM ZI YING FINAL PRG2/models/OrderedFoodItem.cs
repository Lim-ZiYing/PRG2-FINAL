namespace LIM_ZI_YING_FINAL_PRG2.models;

public class OrderedFoodItem
{
    public FoodItem FoodItem { get; }
    public int Quantity { get; set; }

    public OrderedFoodItem(FoodItem foodItem, int quantity)
    {
        FoodItem = foodItem;
        Quantity = quantity;
    }

    public double LineTotal() => FoodItem.Price * Quantity;

    public override string ToString() => $"{FoodItem.ItemName} - {Quantity}";
}
