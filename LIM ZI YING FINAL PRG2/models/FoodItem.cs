namespace LIM_ZI_YING_FINAL_PRG2.models;

public class FoodItem
{
    public string ItemName { get; }
    public string Description { get; }
    public double Price { get; }

    public FoodItem(string itemName, string description, double price)
    {
        ItemName = itemName;
        Description = description;
        Price = price;
    }

    public override string ToString()
        => $"{ItemName}: {Description} - ${Price:0.00}";
}
