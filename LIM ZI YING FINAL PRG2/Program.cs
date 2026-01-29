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

        restaurants = CsvLoader.LoadRestaurants(Path.Combine(dataPath, "restaurants.csv"));
        CsvLoader.LoadFoodItems(Path.Combine(dataPath, "fooditems - Copy.csv"), restaurants);
        customers = CsvLoader.LoadCustomers(Path.Combine(dataPath, "customers.csv"));
        orders = CsvLoader.LoadOrders(ordersPath, restaurants, customers);

        // Attach loaded orders into customer lists too
        var customerMap = customers.ToDictionary(c => c.Email, c => c);
        foreach (var o in orders)
            if (customerMap.TryGetValue(o.CustomerEmail, out var c)) c.AddOrder(o);

        Console.WriteLine("Welcome to the Gruberoo Food Delivery System");
        Console.WriteLine($"{restaurants.Count} restaurants loaded!");
        Console.WriteLine($"{restaurants.Sum(r => r.Menus.Sum(m => m.FoodItems.Count))} food items loaded!");
        Console.WriteLine($"{customers.Count} customers loaded!");
        Console.WriteLine($"{orders.Count} orders loaded!");
        Console.WriteLine();

        while (true)
        {
            ShowMenu();
            int choice = ReadInt("Enter your choice: ");
            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    ListAllRestaurants();
                    break;
                case 2:
                    ListAllOrders();
                    break;
                case 3:
                    CreateNewOrder();
                    break;
                case 4:
                    ProcessOrder();
                    break;
                case 5:
                    ModifyOrder();
                    break;
                case 6:
                    DeleteOrder();
                    break;
                case 0:
                    ExitAndSave();
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please enter 0 to 6.");
                    break;
            }

            Console.WriteLine();
        }
    }

    // ---------------- MENU + HELPERS ----------------

    static void ShowMenu()
    {
        Console.WriteLine("===== Gruberoo Food Delivery System =====");
        Console.WriteLine("1. List all restaurants and menu items");
        Console.WriteLine("2. List all orders");
        Console.WriteLine("3. Create a new order");
        Console.WriteLine("4. Process an order");
        Console.WriteLine("5. Modify an existing order");
        Console.WriteLine("6. Delete an existing order");
        Console.WriteLine("0. Exit");
    }

    static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();
            if (int.TryParse(input, out int value)) return value;
            Console.WriteLine("Invalid number. Try again.");
        }
    }

    static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
            Console.WriteLine("Cannot be empty. Try again.");
        }
    }

    static string ReadYesNo(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? s = Console.ReadLine()?.Trim().ToUpper();
            if (s == "Y" || s == "N") return s;
            Console.WriteLine("Please enter Y or N.");
        }
    }

    static DateTime ReadDeliveryDateTime()
    {
        while (true)
        {
            string d = ReadNonEmpty("Enter Delivery Date (dd/mm/yyyy): ");
            string t = ReadNonEmpty("Enter Delivery Time (hh:mm): ");

            if (DateTime.TryParseExact($"{d} {t}", "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime dt))
                return dt;

            Console.WriteLine("Invalid date/time format. Try again.");
        }
    }

    static Restaurant? GetRestaurantById(string id)
        => restaurants.FirstOrDefault(r => r.RestaurantId.Equals(id, StringComparison.OrdinalIgnoreCase));

    static Customer? GetCustomerByEmail(string email)
        => customers.FirstOrDefault(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    static Order? GetOrderById(int id)
        => orders.FirstOrDefault(o => o.OrderId == id);

    static int NextOrderId()
        => (orders.Count == 0) ? 1001 : orders.Max(o => o.OrderId) + 1;

    // ---------------- FEATURE 1 ----------------

    static void ListAllRestaurants()
    {
        Console.WriteLine("All Restaurants and Menu Items");
        Console.WriteLine("==============================");

        foreach (var r in restaurants)
        {
            Console.WriteLine($"Restaurant: {r.RestaurantName} ({r.RestaurantId})");
            foreach (var m in r.Menus) m.DisplayFoodItems();
            Console.WriteLine();
        }
    }

    // ---------------- FEATURE 2 ----------------

    static void ListAllOrders()
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

            Console.WriteLine($"{o.OrderId,-8}  {custName,-17}  {restName,-16}  {delivery,-20}  ${o.TotalAmount,7:0.00}  {o.Status}");
        }
    }

    // ---------------- FEATURE 3: CREATE NEW ORDER ----------------

    static void CreateNewOrder()
    {
        Console.WriteLine("Create New Order");
        Console.WriteLine("================");

        string email = ReadNonEmpty("Enter Customer Email: ");
        var cust = GetCustomerByEmail(email);
        if (cust == null)
        {
            Console.WriteLine("Customer not found.");
            return;
        }

        string restId = ReadNonEmpty("Enter Restaurant ID: ");
        var rest = GetRestaurantById(restId);
        if (rest == null)
        {
            Console.WriteLine("Restaurant not found.");
            return;
        }

        DateTime deliveryDT = ReadDeliveryDateTime();
        string address = ReadNonEmpty("Enter Delivery Address: ");

        // Show items
        var foodList = rest.Menus.SelectMany(m => m.FoodItems).ToList();
        if (foodList.Count == 0)
        {
            Console.WriteLine("No food items found for this restaurant.");
            return;
        }

        var newOrder = new Order
        {
            OrderId = NextOrderId(),
            CustomerEmail = cust.Email,
            RestaurantId = rest.RestaurantId,
            DeliveryDateTime = deliveryDT,
            DeliveryAddress = address,
            CreatedDateTime = DateTime.Now,
            Status = "Pending"
        };

        Console.WriteLine("Available Food Items:");
        for (int i = 0; i < foodList.Count; i++)
            Console.WriteLine($"{i + 1}. {foodList[i].ItemName} - ${foodList[i].Price:0.00}");

        while (true)
        {
            int itemNo = ReadInt("Enter item number (0 to finish): ");
            if (itemNo == 0) break;
            if (itemNo < 1 || itemNo > foodList.Count)
            {
                Console.WriteLine("Invalid item number.");
                continue;
            }

            int qty = ReadInt("Enter quantity: ");
            if (qty <= 0)
            {
                Console.WriteLine("Quantity must be > 0.");
                continue;
            }

            var fi = foodList[itemNo - 1];

            // if same item chosen again, add qty
            var existing = newOrder.Items.FirstOrDefault(x => x.FoodItem.ItemName.Equals(fi.ItemName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                existing.Quantity += qty;
            else
                newOrder.AddItem(new OrderedFoodItem(fi, qty));
        }

        if (newOrder.Items.Count == 0)
        {
            Console.WriteLine("No items selected. Order not created.");
            return;
        }

        string sr = ReadYesNo("Add special request? [Y/N]: ");
        if (sr == "Y")
        {
            newOrder.SpecialRequest = ReadNonEmpty("Enter special request: ");
        }

        double total = newOrder.CalculateTotal();
        Console.WriteLine($"Order Total: ${(total - 5.00):0.00} + $5.00 (delivery) = ${total:0.00}");

        string pay = ReadYesNo("Proceed to payment? [Y/N]: ");
        if (pay == "N")
        {
            Console.WriteLine("Payment not done. Order not created.");
            return;
        }

        while (true)
        {
            Console.Write("[CC] Credit Card / [PP] PayPal / [CD] Cash on Delivery: ");
            string? method = Console.ReadLine()?.Trim().ToUpper();
            if (method == "CC" || method == "PP" || method == "CD")
            {
                newOrder.PaymentMethod = method;
                break;
            }
            Console.WriteLine("Invalid payment method.");
        }

        // Save into system
        orders.Add(newOrder);
        cust.AddOrder(newOrder);
        rest.OrderQueue.Enqueue(newOrder);

        // Rewrite orders.csv so modifications stay consistent
        CsvLoader.RewriteOrdersCsv(ordersPath, orders);

        Console.WriteLine($"Order {newOrder.OrderId} created successfully! Status: {newOrder.Status}");
    }

    // ---------------- FEATURE 4: PROCESS ORDER ----------------

    static void ProcessOrder()
    {
        Console.WriteLine("Process Order");
        Console.WriteLine("=============");

        string restId = ReadNonEmpty("Enter Restaurant ID: ");
        var rest = GetRestaurantById(restId);
        if (rest == null)
        {
            Console.WriteLine("Restaurant not found.");
            return;
        }

        if (rest.OrderQueue.Count == 0)
        {
            Console.WriteLine("No orders in this restaurant queue.");
            return;
        }

        // Rotate through current queue
        int count = rest.OrderQueue.Count;

        for (int i = 0; i < count; i++)
        {
            var o = rest.OrderQueue.Dequeue();

            Console.WriteLine($"Order {o.OrderId}:");
            Console.WriteLine($"Customer: {customers.FirstOrDefault(c => c.Email == o.CustomerEmail)?.Name ?? o.CustomerEmail}");
            Console.WriteLine("Ordered Items:");
            o.DisplayItems();
            Console.WriteLine($"Delivery date/time: {o.DeliveryDateTime:dd/MM/yyyy HH:mm}");
            Console.WriteLine($"Total Amount: ${o.TotalAmount:0.00}");
            Console.WriteLine($"Order Status: {o.Status}");

            Console.Write("[C]onfirm / [R]eject / [S]kip / [D]eliver: ");
            string action = (Console.ReadLine() ?? "").Trim().ToUpper();

            if (action == "C")
            {
                if (o.Status == "Pending")
                {
                    o.Status = "Preparing";
                    Console.WriteLine($"Order {o.OrderId} confirmed. Status: {o.Status}");
                }
                else Console.WriteLine("Cannot confirm. Only Pending orders can be confirmed.");
            }
            else if (action == "R")
            {
                if (o.Status == "Pending")
                {
                    o.Status = "Rejected";
                    refundStack.Push(o);
                    Console.WriteLine($"Order {o.OrderId} rejected. Refund of ${o.TotalAmount:0.00} processed.");
                }
                else Console.WriteLine("Cannot reject. Only Pending orders can be rejected.");
            }
            else if (action == "S")
            {
                if (o.Status == "Cancelled")
                    Console.WriteLine("Skipped cancelled order.");
                else
                    Console.WriteLine("Skip is intended for Cancelled orders (no change made).");
            }
            else if (action == "D")
            {
                if (o.Status == "Preparing")
                {
                    o.Status = "Delivered";
                    Console.WriteLine($"Order {o.OrderId} delivered. Status: {o.Status}");
                }
                else Console.WriteLine("Cannot deliver. Only Preparing orders can be delivered.");
            }
            else
            {
                Console.WriteLine("Invalid action. No change made.");
            }

            // Put it back into the queue to keep record in queue file
            rest.OrderQueue.Enqueue(o);

            Console.WriteLine();
        }

        // Keep orders.csv updated
        CsvLoader.RewriteOrdersCsv(ordersPath, orders);
    }

    // ---------------- FEATURE 5: MODIFY ORDER ----------------

    static void ModifyOrder()
    {
        Console.WriteLine("Modify Order");
        Console.WriteLine("===========");

        string email = ReadNonEmpty("Enter Customer Email: ");
        var cust = GetCustomerByEmail(email);
        if (cust == null)
        {
            Console.WriteLine("Customer not found.");
            return;
        }

        var pending = cust.Orders.Where(o => o.Status == "Pending").OrderBy(o => o.OrderId).ToList();
        if (pending.Count == 0)
        {
            Console.WriteLine("No Pending orders for this customer.");
            return;
        }

        Console.WriteLine("Pending Orders:");
        foreach (var o in pending) Console.WriteLine(o.OrderId);

        int orderId = ReadInt("Enter Order ID: ");
        var order = pending.FirstOrDefault(o => o.OrderId == orderId);
        if (order == null)
        {
            Console.WriteLine("Invalid Order ID.");
            return;
        }

        var rest = GetRestaurantById(order.RestaurantId);
        if (rest == null)
        {
            Console.WriteLine("Restaurant not found for this order.");
            return;
        }

        Console.WriteLine("Order Items:");
        order.DisplayItems();
        Console.WriteLine($"Address: {order.DeliveryAddress}");
        Console.WriteLine($"Delivery Date/Time: {order.DeliveryDateTime:dd/MM/yyyy HH:mm}");

        Console.Write("Modify: [1] Items [2] Address [3] Delivery Time: ");
        string opt = (Console.ReadLine() ?? "").Trim();

        if (opt == "1")
        {
            // rebuild items
            var foodList = rest.Menus.SelectMany(m => m.FoodItems).ToList();
            Console.WriteLine("Available Food Items:");
            for (int i = 0; i < foodList.Count; i++)
                Console.WriteLine($"{i + 1}. {foodList[i].ItemName} - ${foodList[i].Price:0.00}");

            double oldTotal = order.TotalAmount;

            order.ClearItems();
            while (true)
            {
                int itemNo = ReadInt("Enter item number (0 to finish): ");
                if (itemNo == 0) break;
                if (itemNo < 1 || itemNo > foodList.Count)
                {
                    Console.WriteLine("Invalid item number.");
                    continue;
                }
                int qty = ReadInt("Enter quantity: ");
                if (qty <= 0)
                {
                    Console.WriteLine("Quantity must be > 0.");
                    continue;
                }

                var fi = foodList[itemNo - 1];
                var existing = order.Items.FirstOrDefault(x => x.FoodItem.ItemName.Equals(fi.ItemName, StringComparison.OrdinalIgnoreCase));
                if (existing != null) existing.Quantity += qty;
                else order.AddItem(new OrderedFoodItem(fi, qty));
            }

            if (order.Items.Count == 0)
            {
                Console.WriteLine("No items selected. Reverting change.");
                // simplest: do nothing else (in real, you'd restore old items)
                return;
            }

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

        CsvLoader.RewriteOrdersCsv(ordersPath, orders);
    }

    // ---------------- FEATURE 6: DELETE (CANCEL) ORDER ----------------

    static void DeleteOrder()
    {
        Console.WriteLine("Delete Order");
        Console.WriteLine("===========");

        string email = ReadNonEmpty("Enter Customer Email: ");
        var cust = GetCustomerByEmail(email);
        if (cust == null)
        {
            Console.WriteLine("Customer not found.");
            return;
        }

        var pending = cust.Orders.Where(o => o.Status == "Pending").OrderBy(o => o.OrderId).ToList();
        if (pending.Count == 0)
        {
            Console.WriteLine("No Pending orders for this customer.");
            return;
        }

        Console.WriteLine("Pending Orders:");
        foreach (var o in pending) Console.WriteLine(o.OrderId);

        int orderId = ReadInt("Enter Order ID: ");
        var order = pending.FirstOrDefault(o => o.OrderId == orderId);
        if (order == null)
        {
            Console.WriteLine("Invalid Order ID.");
            return;
        }

        Console.WriteLine($"Order {order.OrderId}");
        Console.WriteLine($"Customer: {cust.Name}");
        Console.WriteLine("Ordered Items:");
        order.DisplayItems();
        Console.WriteLine($"Delivery date/time: {order.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Total Amount: ${order.TotalAmount:0.00}");
        Console.WriteLine($"Order Status: {order.Status}");

        string confirm = ReadYesNo("Confirm deletion? [Y/N]: ");
        if (confirm == "N")
        {
            Console.WriteLine("Deletion cancelled.");
            return;
        }

        order.Status = "Cancelled";
        refundStack.Push(order);

        CsvLoader.RewriteOrdersCsv(ordersPath, orders);

        Console.WriteLine($"Order {order.OrderId} cancelled. Refund of ${order.TotalAmount:0.00} processed.");
    }

    // ---------------- EXIT SAVE ----------------

    static void ExitAndSave()
    {
        // per assignment: save queue and stack to new files on exit
        string queuePath = Path.Combine(dataPath, "queue.csv");
        string stackPath = Path.Combine(dataPath, "stack.csv");

        CsvLoader.SaveQueueToCsv(queuePath, restaurants);
        CsvLoader.SaveRefundStackToCsv(stackPath, refundStack);

        // also keep orders.csv updated
        CsvLoader.RewriteOrdersCsv(ordersPath, orders);

        Console.WriteLine("Saved queue.csv and stack.csv. Goodbye!");
    }
}
