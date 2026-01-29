using System.Globalization;
using LIM_ZI_YING_FINAL_PRG2.models;

namespace LIM_ZI_YING_FINAL_PRG2.services;

public static class CsvLoader
{
    public static List<Restaurant> LoadRestaurants(string path)
    {
        var list = new List<Restaurant>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            string id = parts[0].Trim();
            string name = parts[1].Trim();
            string email = parts[2].Trim();

            list.Add(new Restaurant(id, name, email));
        }

        return list;
    }

    public static void LoadFoodItems(string path, List<Restaurant> restaurants)
    {
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');

            string restId = parts[0].Trim();
            string itemName = parts[1].Trim();
            string desc = parts[2].Trim();
            double price = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);

            var restaurant = restaurants.FirstOrDefault(r => r.RestaurantId == restId);
            if (restaurant == null) continue;

            if (!restaurant.Menus.Any())
                restaurant.AddMenu(new Menu("M001", "Main Menu"));

            restaurant.Menus[0].AddFoodItem(new FoodItem(itemName, desc, price));
        }
    }

    public static List<Customer> LoadCustomers(string path)
    {
        var list = new List<Customer>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            string name = parts[0].Trim();
            string email = parts[1].Trim();

            list.Add(new Customer(name, email));
        }

        return list;
    }
    public static List<Order> LoadOrders(
    string path,
    List<Restaurant> restaurants,
    List<Customer> customers)
    {
        var orders = new List<Order>();
        var lines = File.ReadAllLines(path);

        // Quick lookup maps
        var restaurantMap = restaurants.ToDictionary(r => r.RestaurantId, r => r);
        var customerMap = customers.ToDictionary(c => c.Email, c => c);

        for (int i = 1; i < lines.Length; i++)
        {
            // Split CSV safely (because Items column has commas but is in quotes)
            var parts = SplitCsvLine(lines[i]);

            int orderId = int.Parse(parts[0].Trim());
            string customerEmail = parts[1].Trim();
            string restaurantId = parts[2].Trim();

            string deliveryDate = parts[3].Trim();   // dd/MM/yyyy
            string deliveryTime = parts[4].Trim();   // HH:mm
            string deliveryAddress = parts[5].Trim();

            string createdDateTimeStr = parts[6].Trim(); // dd/MM/yyyy HH:mm
            double totalAmount = double.Parse(parts[7].Trim(), CultureInfo.InvariantCulture);

            string status = parts[8].Trim();
            string itemsStr = parts[9].Trim().Trim('"');

            // Build DateTimes
            DateTime deliveryDT = DateTime.ParseExact(
                $"{deliveryDate} {deliveryTime}",
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture
            );

            DateTime createdDT = DateTime.ParseExact(
                createdDateTimeStr,
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture
            );

            var order = new Order
            {
                OrderId = orderId,
                CustomerEmail = customerEmail,
                RestaurantId = restaurantId,
                DeliveryDateTime = deliveryDT,
                DeliveryAddress = deliveryAddress,
                CreatedDateTime = createdDT,
                TotalAmount = totalAmount,
                Status = status
            };

            // Parse items and attach FoodItem objects (by item name)
            if (restaurantMap.TryGetValue(restaurantId, out var restaurant))
            {
                ParseItemsIntoOrder(order, restaurant, itemsStr);

                // Put order into Restaurant queue (assignment requirement)
                restaurant.OrderQueue.Enqueue(order);
            }

            // (Optional) you can later also attach to customer's order list when you upgrade Customer class
            // For now we just ensure customer exists:
            if (!customerMap.ContainsKey(customerEmail))
            {
                // If orders.csv has emails not in customers.csv, we still keep the order.
                // (No crash)
            }

            orders.Add(order);
        }

        return orders;
    }

    // -------- helper methods --------

    private static void ParseItemsIntoOrder(Order order, Restaurant restaurant, string itemsStr)
    {
        if (string.IsNullOrWhiteSpace(itemsStr)) return;

        // Items format: ItemName, qty|ItemName, qty
        var parts = itemsStr.Split('|', StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in parts)
        {
            var pair = p.Split(',', 2); // split into [name] [qty]
            if (pair.Length < 2) continue;

            string itemName = pair[0].Trim();
            string qtyStr = pair[1].Trim();

            if (!int.TryParse(qtyStr, out int qty)) continue;

            // find FoodItem by name in restaurant menu(s)
            var foodItem =
                restaurant.Menus
                    .SelectMany(m => m.FoodItems)
                    .FirstOrDefault(fi => fi.ItemName.Equals(itemName, StringComparison.OrdinalIgnoreCase));

            if (foodItem != null)
            {
                order.AddItem(new OrderedFoodItem(foodItem, qty));
            }
        }
    }

    // Handles quotes properly (because Items column contains commas)
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                current.Append(c);
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

}
