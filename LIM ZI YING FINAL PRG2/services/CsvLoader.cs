using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LIM_ZI_YING_FINAL_PRG2.models;

namespace LIM_ZI_YING_FINAL_PRG2.services;

public static class CsvLoader
{
    // LOAD RESTAURANTS 
    public static List<Restaurant> LoadRestaurants(string path)
    {
        var list = new List<Restaurant>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = SplitCsvLine(lines[i]);
            if (parts.Count < 3) continue;

            string id = parts[0].Trim().Trim('"');
            string name = parts[1].Trim().Trim('"');
            string email = parts[2].Trim().Trim('"');

            list.Add(new Restaurant(id, name, email));
        }

        return list;
    }

    //  LOAD FOOD ITEMS 
    public static void LoadFoodItems(string path, List<Restaurant> restaurants)
    {
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = SplitCsvLine(lines[i]);
            if (parts.Count < 4) continue;

            string restId = parts[0].Trim().Trim('"');
            string itemName = parts[1].Trim().Trim('"');
            string desc = parts[2].Trim().Trim('"');

            if (!double.TryParse(parts[3].Trim().Trim('"'), NumberStyles.Any, CultureInfo.InvariantCulture, out double price))
                continue;

            var restaurant = restaurants.FirstOrDefault(r =>
                r.RestaurantId.Equals(restId, StringComparison.OrdinalIgnoreCase));

            if (restaurant == null) continue;

            // ensure at least 1 menu exists
            if (!restaurant.Menus.Any())
                restaurant.AddMenu(new Menu("M001", "Main Menu"));

            restaurant.Menus[0].AddFoodItem(new FoodItem(itemName, desc, price));
        }
    }

    // LOAD CUSTOMERS 
    public static List<Customer> LoadCustomers(string path)
    {
        var list = new List<Customer>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = SplitCsvLine(lines[i]);
            if (parts.Count < 2) continue;

            string name = parts[0].Trim().Trim('"');
            string email = parts[1].Trim().Trim('"');

            list.Add(new Customer(name, email));
        }

        return list;
    }

    // LOAD SPECIAL OFFERS 
    // Expected CSV format:
    // OfferCode,OfferDesc,Discount,RestaurantId
    public static void LoadSpecialOffers(string filePath, List<Restaurant> restaurants)
    {
        if (!File.Exists(filePath)) return;

        var lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = SplitCsvLine(lines[i]);
            if (parts.Count < 4) continue;

            string offerCode = parts[0].Trim().Trim('"');
            string offerDesc = parts[1].Trim().Trim('"');

            if (!double.TryParse(parts[2].Trim().Trim('"'), NumberStyles.Any, CultureInfo.InvariantCulture, out double discount))
                continue;

            string restaurantId = parts[3].Trim().Trim('"');

            var restaurant = restaurants.FirstOrDefault(r =>
                r.RestaurantId.Equals(restaurantId, StringComparison.OrdinalIgnoreCase));

            if (restaurant == null) continue;

            restaurant.AddSpecialOffer(new SpecialOffer(offerCode, offerDesc, discount));
        }
    }

    // LOAD ORDERS 
    public static List<Order> LoadOrders(string path, List<Restaurant> restaurants, List<Customer> customers)
    {
        var loadedOrders = new List<Order>();
        var lines = File.ReadAllLines(path);

        var restaurantMap = restaurants.ToDictionary(r => r.RestaurantId, r => r);
        var customerMap = customers.ToDictionary(c => c.Email, c => c);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var parts = SplitCsvLine(lines[i]);
            if (parts.Count < 10) continue;

            int orderId = int.Parse(parts[0].Trim().Trim('"'));
            string customerEmail = parts[1].Trim().Trim('"');
            string restaurantId = parts[2].Trim().Trim('"');

            string deliveryDate = parts[3].Trim().Trim('"'); // dd/MM/yyyy
            string deliveryTime = parts[4].Trim().Trim('"'); // HH:mm
            string deliveryAddress = parts[5].Trim().Trim('"');

            string createdStr = parts[6].Trim().Trim('"');   // dd/MM/yyyy HH:mm
            string amountStr = parts[7].Trim().Trim('"');
            string status = parts[8].Trim().Trim('"');
            string itemsStr = parts[9].Trim().Trim('"');

            if (!double.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double totalAmount))
                totalAmount = 0;

            DateTime deliveryDT = DateTime.ParseExact($"{deliveryDate} {deliveryTime}", "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            DateTime createdDT = DateTime.ParseExact(createdStr, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

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

            // Attach items + queue
            if (restaurantMap.TryGetValue(restaurantId, out var restaurant))
            {
                ParseItemsIntoOrder(order, restaurant, itemsStr);
                restaurant.OrderQueue.Enqueue(order);
            }

            // Add to list
            loadedOrders.Add(order);

            // ensure exists, don't crash
            _ = customerMap.ContainsKey(customerEmail);
        }

        return loadedOrders;
    }

    // PARSE ITEMS 
    private static void ParseItemsIntoOrder(Order order, Restaurant restaurant, string itemsStr)
    {
        if (string.IsNullOrWhiteSpace(itemsStr)) return;

        // Items format: ItemName,qty|ItemName,qty
        var parts = itemsStr.Split('|', StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in parts)
        {
            var pair = p.Split(',', 2);
            if (pair.Length < 2) continue;

            string itemName = pair[0].Trim();
            string qtyStr = pair[1].Trim();

            if (!int.TryParse(qtyStr, out int qty)) continue;

            var foodItem = restaurant.Menus
                .SelectMany(m => m.FoodItems)
                .FirstOrDefault(fi => fi.ItemName.Equals(itemName, StringComparison.OrdinalIgnoreCase));

            if (foodItem != null)
                order.AddItem(new OrderedFoodItem(foodItem, qty));
        }
    }

    // SAVE QUEUE 
    public static void SaveQueueToCsv(string path, List<Restaurant> restaurants)
    {
        using var sw = new StreamWriter(path, false);
        sw.WriteLine("RestaurantId,OrderId,CustomerEmail,DeliveryDateTime,TotalAmount,Status");

        foreach (var r in restaurants)
        {
            foreach (var o in r.OrderQueue)
            {
                sw.WriteLine($"{r.RestaurantId},{o.OrderId},{o.CustomerEmail},{o.DeliveryDateTime:dd/MM/yyyy HH:mm},{o.TotalAmount.ToString(CultureInfo.InvariantCulture)},{o.Status}");
            }
        }
    }

    //  SAVE REFUND STACK
    public static void SaveRefundStackToCsv(string path, Stack<Order> refundStack)
    {
        using var sw = new StreamWriter(path, false);
        sw.WriteLine("OrderId,CustomerEmail,RestaurantId,DeliveryDateTime,TotalAmount,Status");

        foreach (var o in refundStack)
        {
            sw.WriteLine($"{o.OrderId},{o.CustomerEmail},{o.RestaurantId},{o.DeliveryDateTime:dd/MM/yyyy HH:mm},{o.TotalAmount.ToString(CultureInfo.InvariantCulture)},{o.Status}");
        }
    }

    //  REWRITE ORDERS.CSV 
    public static void RewriteOrdersCsv(string path, List<Order> orders)
    {
        using var sw = new StreamWriter(path, false);
        sw.WriteLine("OrderId,CustomerEmail,RestaurantId,DeliveryDate,DeliveryTime,DeliveryAddress,CreatedDateTime,TotalAmount,Status,Items");

        foreach (var o in orders.OrderBy(x => x.OrderId))
        {
            string deliveryDate = o.DeliveryDateTime.ToString("dd/MM/yyyy");
            string deliveryTime = o.DeliveryDateTime.ToString("HH:mm");
            string created = o.CreatedDateTime.ToString("dd/MM/yyyy HH:mm");
            string amount = o.TotalAmount.ToString(CultureInfo.InvariantCulture);

            string items = string.Join("|", o.Items.Select(i => $"{i.FoodItem.ItemName},{i.Quantity}"));

            sw.WriteLine($"{o.OrderId},{o.CustomerEmail},{o.RestaurantId},{deliveryDate},{deliveryTime},{o.DeliveryAddress},{created},{amount},{o.Status},\"{items}\"");
        }
    }

    //  SAFE CSV SPLIT (handles quotes) 
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                // keep quotes or drop? keep is fine then Trim('"') later
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
