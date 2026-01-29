using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LIM_ZI_YING_FINAL_PRG2.models;
using LIM_ZI_YING_FINAL_PRG2.services;

class Program
{
    static List<Restaurant> restaurants = new();
    static List<Customer> customers = new();
    static List<Order> orders = new();
    static Stack<Order> refundStack = new(); // rejected/cancelled copies

    static string dataPath = "";
    static string ordersPath = "";

    static void Main()
    {
        dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        ordersPath = Path.Combine(dataPath, "orders.csv");
        string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        // Load restaurants + food items + customers
        var restaurants = CsvLoader.LoadRestaurants(Path.Combine(dataPath, "restaurants.csv"));
        CsvLoader.LoadFoodItems(Path.Combine(dataPath, "fooditems - Copy.csv"), restaurants);
        var customers = CsvLoader.LoadCustomers(Path.Combine(dataPath, "customers.csv"));

        // Load orders
        var orders = CsvLoader.LoadOrders(Path.Combine(dataPath, "orders.csv"), restaurants, customers);

        Console.WriteLine("Welcome to the Gruberoo Food Delivery System");
        Console.WriteLine($"{restaurants.Count} restaurants loaded!");
        Console.WriteLine($"{restaurants.Sum(r => r.Menus.Sum(m => m.FoodItems.Count))} food items loaded!");
        Console.WriteLine($"{customers.Count} customers loaded!");
        Console.WriteLine($"{orderQueue.Count + processedStack.Count} orders loaded!");
        Console.WriteLine();

        // Feature: List all orders
        ListAllOrders(orders, restaurants, customers);

        Console.ReadLine();
    }

    static void ListAllOrders(List<Order> orders, List<Restaurant> restaurants, List<Customer> customers)
    {
        var allOrders = queue.Concat(stack).OrderBy(o => o.OrderId).ToList();

        Console.WriteLine("Order ID  Customer           Restaurant         Delivery Date/Time     Amount    Status");
        Console.WriteLine("--------  -----------------  ----------------  --------------------  --------  ---------");

        foreach (var o in allOrders)
        {
            string custName = customers.FirstOrDefault(c => c.Email == o.CustomerEmail)?.Name ?? o.CustomerEmail;
            string restName = restaurants.FirstOrDefault(r => r.RestaurantId == o.RestaurantId)?.RestaurantName ?? o.RestaurantId;

            
            string delivery = o.DeliveryDateTime.ToString("dd/MM/yyyy HH:mm");

            double newTotal = order.CalculateTotal();

            if (newTotal > oldTotal)
            {
                Console.WriteLine($"New total is higher: old ${oldTotal:0.00} -> new ${newTotal:0.00}");
                string pay = ReadYesNo("Pay the difference now? [Y/N]: ");
                if (pay == "N")
                {
                    Console.WriteLine("Payment not done. Modification cancelled.");
                    return;
                }
                Console.WriteLine("Payment recorded.");
            }

            Console.WriteLine($"Order {order.OrderId} updated. New Total: ${order.TotalAmount:0.00}");
        }
        else if (opt == "2")
        {
            string newAddr = ReadNonEmpty("Enter new Delivery Address: ");
            order.DeliveryAddress = newAddr;
            Console.WriteLine($"Order {order.OrderId} updated. New Address: {order.DeliveryAddress}");
        }
        else if (opt == "3")
        {
            while (true)
            {
                Console.Write("Enter new Delivery Time (hh:mm): ");
                string? t = Console.ReadLine()?.Trim();
                if (DateTime.TryParseExact(t, "HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime timeOnly))
                {
                    order.DeliveryDateTime = new DateTime(
                        order.DeliveryDateTime.Year,
                        order.DeliveryDateTime.Month,
                        order.DeliveryDateTime.Day,
                        timeOnly.Hour,
                        timeOnly.Minute,
                        0
                    );

                    Console.WriteLine($"Order {order.OrderId} updated. New Delivery Time: {order.DeliveryDateTime:HH:mm}");
                    break;
                }
                Console.WriteLine("Invalid time format.");
            }
        }
        else
        {
            Console.WriteLine("Invalid option.");
            return;
        }
    }
}
// end//
