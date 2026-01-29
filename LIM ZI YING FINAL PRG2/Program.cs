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
        string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        // Load data
        var restaurants = CsvLoader.LoadRestaurants(Path.Combine(dataPath, "restaurants.csv"));
        CsvLoader.LoadFoodItems(Path.Combine(dataPath, "fooditems - Copy.csv"), restaurants);
        var customers = CsvLoader.LoadCustomers(Path.Combine(dataPath, "customers.csv"));
        var orderList = CsvLoader.LoadOrders(Path.Combine(dataPath, "orders.csv"), restaurants, customers);

        Queue<Order> orderQueue = new Queue<Order>(orderList.Where(o => o.Status != "Completed"));
        Stack<Order> processedStack = new Stack<Order>(orderList.Where(o => o.Status == "Completed"));

        // Startup message
        Console.WriteLine("Welcome to the Gruberoo Food Delivery System");
        Console.WriteLine($"{restaurants.Count} restaurants loaded!");
        Console.WriteLine($"{restaurants.Sum(r => r.Menus.Sum(m => m.FoodItems.Count))} food items loaded!");
        Console.WriteLine($"{customers.Count} customers loaded!");
        Console.WriteLine($"{orderQueue.Count + processedStack.Count} orders loaded!");
        Console.WriteLine();

        // Main menu loop
        while (true)
        {
            Console.WriteLine("===== Gruberoo Food Delivery System =====");
            Console.WriteLine("1. List all restaurants and menu items");
            Console.WriteLine("2. List all orders");
            Console.WriteLine("3. Create a new order");
            Console.WriteLine("4. Process an order");
            Console.WriteLine("5. Modify an existing order");
            Console.WriteLine("6. Delete an existing order");
            Console.WriteLine("0. Exit");
            Console.Write("Enter your choice: ");

            string choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    ListRestaurants(restaurants);
                    break;

                case "2":
                    ListAllOrders(orderQueue, processedStack, restaurants, customers);
                    break;

                case "3":
                    CreateOrder(orderQueue, restaurants, customers);
                    break;

                case "4":
                    ProcessOrder(orderQueue, processedStack);
                    break;

                case "5":
                    ModifyOrder(orderQueue);
                    break;

                case "6":
                    DeleteOrder(orderQueue);
                    break;

                case "0":
                    SaveQueueAndStack(orderQueue, processedStack, dataPath);
                    Console.WriteLine("Goodbye!");
                    return;

                default:
                    Console.WriteLine("Invalid choice!");
                    break;
            }

            Console.WriteLine();
        }
    }

    // ============ FEATURE 1 ============
    static void ListRestaurants(List<Restaurant> restaurants)
    {
        foreach (var r in restaurants)
        {
            Console.WriteLine($"[{r.RestaurantId}] {r.RestaurantName}");
            foreach (var m in r.Menus)
            {
                Console.WriteLine($"  - {m.MenuName}");
                foreach (var f in m.FoodItems)
                {
                    Console.WriteLine($"      {f.ItemName} {f.Description} ${f.Price:0.00}");
                }
            }
            Console.WriteLine();
        }
    }

    // ============ FEATURE 2 ============
    static void ListAllOrders(Queue<Order> queue, Stack<Order> stack, List<Restaurant> restaurants, List<Customer> customers)
    {
        var allOrders = queue.Concat(stack).OrderBy(o => o.OrderId).ToList();

        Console.WriteLine("Order ID  Customer           Restaurant         Delivery Date/Time     Amount    Status");
        Console.WriteLine("--------  -----------------  ----------------  --------------------  --------  ---------");

        foreach (var o in allOrders)
        {
            string custName = customers.FirstOrDefault(c => c.Email == o.CustomerEmail)?.Name ?? o.CustomerEmail;
            string restName = restaurants.FirstOrDefault(r => r.RestaurantId == o.RestaurantId)?.RestaurantName ?? o.RestaurantId;
            string delivery = o.DeliveryDateTime.ToString("dd/MM/yyyy HH:mm");

            Console.WriteLine(
                $"{o.OrderId,-8}  {custName,-17}  {restName,-16}  {delivery,-20}  ${o.TotalAmount,7:0.00}  {o.Status}"
            );
        }
    }

    // ============ FEATURE 3 ============
    static void CreateOrder(Queue<Order> queue, List<Restaurant> restaurants, List<Customer> customers)
    {
        Console.Write("Enter customer email: ");
        string email = Console.ReadLine();

        if (!customers.Any(c => c.Email == email))
        {
            Console.WriteLine("Customer not found!");
            return;
        }

        Console.Write("Enter restaurant id: ");
        string restId = Console.ReadLine();

        if (!restaurants.Any(r => r.RestaurantId == restId))
        {
            Console.WriteLine("Restaurant not found!");
            return;
        }

        int newId = (queue.Count == 0) ? 1 : queue.Max(o => o.OrderId) + 1;

        Order o = new Order
        {
            OrderId = newId,
            CustomerEmail = email,
            RestaurantId = restId,
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now.AddHours(1),
            DeliveryAddress = "Not specified",
            Status = "Pending",
            TotalAmount = 0
        };

        queue.Enqueue(o);
        Console.WriteLine($"Order {newId} created.");
    }

    // ============ FEATURE 4 ============
    static void ProcessOrder(Queue<Order> queue, Stack<Order> stack)
    {
        if (queue.Count == 0)
        {
            Console.WriteLine("No pending orders.");
            return;
        }

        var o = queue.Dequeue();
        o.Status = "Completed";
        stack.Push(o);

        Console.WriteLine($"Order {o.OrderId} processed.");
    }

    // ============ FEATURE 5 ============
    static void ModifyOrder(Queue<Order> queue)
    {
        Console.Write("Enter order ID to modify: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        bool found = false;
        var temp = new Queue<Order>();

        while (queue.Count > 0)
        {
            var o = queue.Dequeue();

            if (o.OrderId == id)
            {
                found = true;
                Console.Write("Enter new delivery address: ");
                o.DeliveryAddress = Console.ReadLine();
            }

            temp.Enqueue(o);
        }

        while (temp.Count > 0)
            queue.Enqueue(temp.Dequeue());

        Console.WriteLine(found ? "Order updated." : "Order not found or already processed.");
    }

    // ============ FEATURE 6 ============
    static void DeleteOrder(Queue<Order> queue)
    {
        Console.Write("Enter order ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        bool found = false;
        var temp = new Queue<Order>();

        while (queue.Count > 0)
        {
            var o = queue.Dequeue();
            if (o.OrderId == id)
            {
                found = true;
                continue;
            }
            temp.Enqueue(o);
        }

        while (temp.Count > 0)
            queue.Enqueue(temp.Dequeue());

        Console.WriteLine(found ? "Order deleted." : "Order not found or already processed.");
    }

    // ============ SAVE FILES ============
    static void SaveQueueAndStack(Queue<Order> queue, Stack<Order> stack, string dataPath)
    {
        File.WriteAllLines(
            Path.Combine(dataPath, "queue.csv"),
            queue.Select(o => string.Join(",",
                o.OrderId,
                o.CustomerEmail,
                o.RestaurantId,
                o.DeliveryDateTime.ToString("yyyy-MM-dd HH:mm"),
                o.DeliveryAddress.Replace(",", " "),
                o.CreatedDateTime.ToString("yyyy-MM-dd HH:mm"),
                o.TotalAmount.ToString("0.00"),
                o.Status
            ))
        );

        File.WriteAllLines(
            Path.Combine(dataPath, "stack.csv"),
            stack.Reverse().Select(o => string.Join(",",
                o.OrderId,
                o.CustomerEmail,
                o.RestaurantId,
                o.DeliveryDateTime.ToString("yyyy-MM-dd HH:mm"),
                o.DeliveryAddress.Replace(",", " "),
                o.CreatedDateTime.ToString("yyyy-MM-dd HH:mm"),
                o.TotalAmount.ToString("0.00"),
                o.Status
            ))
        );

        Console.WriteLine("queue.csv and stack.csv saved.");
    }
}

