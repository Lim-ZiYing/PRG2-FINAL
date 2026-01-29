using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LIM_ZI_YING_FINAL_PRG2.models;
using LIM_ZI_YING_FINAL_PRG2.services;

class Program
{
    static void Main()
    {
        // IMPORTANT: your folder name is "data" (lowercase)
        string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        // Load restaurants + food items + customers
        var restaurants = CsvLoader.LoadRestaurants(Path.Combine(dataPath, "restaurants.csv"));
        CsvLoader.LoadFoodItems(Path.Combine(dataPath, "fooditems - Copy.csv"), restaurants);
        var customers = CsvLoader.LoadCustomers(Path.Combine(dataPath, "customers.csv"));

        // Load orders
        var orders = CsvLoader.LoadOrders(Path.Combine(dataPath, "orders.csv"), restaurants, customers);

        // Startup message
        Console.WriteLine("Welcome to the Gruberoo Food Delivery System");
        Console.WriteLine($"{restaurants.Count} restaurants loaded!");
        Console.WriteLine($"{restaurants.Sum(r => r.Menus.Sum(m => m.FoodItems.Count))} food items loaded!");
        Console.WriteLine($"{customers.Count} customers loaded!");
        Console.WriteLine($"{orders.Count} orders loaded!");
        Console.WriteLine();

        // Feature: List all orders
        ListAllOrders(orders, restaurants, customers);

        Console.ReadLine();
    }

    static void ListAllOrders(List<Order> orders, List<Restaurant> restaurants, List<Customer> customers)
    {
        Console.WriteLine("All Orders");
        Console.WriteLine("==========");
        Console.WriteLine("Order ID  Customer           Restaurant         Delivery Date/Time     Amount    Status");
        Console.WriteLine("--------  -----------------  ----------------  --------------------  --------  ---------");

        foreach (var o in orders.OrderBy(x => x.OrderId))
        {
            string custName = customers.FirstOrDefault(c => c.Email == o.CustomerEmail)?.Name ?? o.CustomerEmail;
            string restName = restaurants.FirstOrDefault(r => r.RestaurantId == o.RestaurantId)?.RestaurantName ?? o.RestaurantId;

            
            string delivery = o.DeliveryDateTime.ToString("dd/MM/yyyy HH:mm");

            Console.WriteLine(
                $"{o.OrderId,-8}  {custName,-17}  {restName,-16}  {delivery,-20}  ${o.TotalAmount,7:0.00}  {o.Status}"
            );
        }
    }
}
