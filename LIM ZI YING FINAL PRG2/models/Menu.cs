namespace LIM_ZI_YING_FINAL_PRG2.models;

public class Menu
{
    public string MenuId { get; }
    public string MenuName { get; }

    private readonly List<FoodItem> _foodItems = new();
    public IReadOnlyList<FoodItem> FoodItems => _foodItems;

    public Menu(string menuId, string menuName)
    {
        MenuId = menuId;
        MenuName = menuName;
    }

    public void AddFoodItem(FoodItem item) => _foodItems.Add(item);

    public void DisplayFoodItems()
    {
        foreach (var fi in _foodItems)
            Console.WriteLine($" - {fi}");
    }
}
