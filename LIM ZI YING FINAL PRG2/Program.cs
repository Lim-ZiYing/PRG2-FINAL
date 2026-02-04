using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using LIM_ZI_YING_FINAL_PRG2.models;
using LIM_ZI_YING_FINAL_PRG2.services;

class Program
{
    // Data storage
    static List<Restaurant> restaurants = new();
    static List<Customer> customers = new();
    static List<Order> orders = new();
    static Stack<Order> refundStack = new(); // Cancelled/Rejected refunds

    // Paths
    static string baseDataPath = "";
    static string ordersCsvPath = "";

    static void Main()
    {
        baseDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        ordersCsvPath = Path.Combine(baseDataPath, "orders.csv");

        // Load
        restaurants = CsvLoader.LoadRestaurants(Path.Combine(baseDataPath, "restaurants.csv"));
        CsvLoader.LoadFoodItems(Path.Combine(baseDataPath, "fooditems - Copy.csv"), restaurants);
        CsvLoader.LoadSpecialOffers(Path.Combine(baseDataPath, "specialoffers.csv"), restaurants);
        customers = CsvLoader.LoadCustomers(Path.Combine(baseDataPath, "customers.csv"));
        orders = CsvLoader.LoadOrders(ordersCsvPath, restaurants, customers);

        // Attach orders to customers
        var custMap = customers.ToDictionary(c => c.Email, c => c, StringComparer.OrdinalIgnoreCase);
        foreach (var o in orders)
            if (custMap.TryGetValue(o.CustomerEmail, out var c)) c.AddOrder(o);

        Console.WriteLine("Welcome to the Gruberoo Food Delivery System");
        Console.WriteLine($"{restaurants.Count} restaurants loaded!");
        Console.WriteLine($"{restaurants.Sum(r => r.Menus.Sum(m => m.FoodItems.Count))} food items loaded!");
        Console.WriteLine($"{customers.Count} customers loaded!");
        Console.WriteLine($"{orders.Count} orders loaded!");
        Console.WriteLine();

        // Menu loop
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
                case 7:
                    ViewRefundStack();
                    break;

                // Advanced (a)
                case 8:
                    BulkProcessPendingOrdersForCurrentDay();
                    break;

                // Advanced (b)
                case 9:
                    AdvancedReport_OrderAmountRefundsEarnings();
                    break;

                case 0:
                    ExitAndSave();
                    return;

                default:
                    Console.WriteLine("Invalid choice. Please enter 0 to 9.");
                    break;
            }

            Console.WriteLine();
        }
    }

    // ---------------- MENU ----------------

    static void ShowMenu()
    {
        Console.WriteLine("===== Gruberoo Food Delivery System =====");
        Console.WriteLine("1. List all restaurants and menu items");
        Console.WriteLine("2. List all orders");
        Console.WriteLine("3. Create a new order");
        Console.WriteLine("4. Process an order");
        Console.WriteLine("5. Modify an existing order");
        Console.WriteLine("6. Delete an existing order");
        Console.WriteLine("7. View refund stack (latest first)");
        Console.WriteLine("8. Advanced (a) Bulk process Pending orders for current day");
        Console.WriteLine("9. Advanced (b) Display total order amount / refunds / earnings");
        Console.WriteLine("0. Exit");
    }

    // ---------------- INPUT HELPERS ----------------

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
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dt))
            {
                return dt;
            }


            Console.WriteLine("Invalid date/time format. Try again.");
        }
    }

    

    // ---------------- FIND HELPERS ----------------

    static Restaurant? GetRestaurantById(string id)
        => restaurants.FirstOrDefault(r => r.RestaurantId.Equals(id, StringComparison.OrdinalIgnoreCase));

    static Customer? GetCustomerByEmail(string email)
        => customers.FirstOrDefault(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

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
            string custName = customers.FirstOrDefault(c => c.Email.Equals(o.CustomerEmail, StringComparison.OrdinalIgnoreCase))?.Name ?? o.CustomerEmail;
            string restName = restaurants.FirstOrDefault(r => r.RestaurantId.Equals(o.RestaurantId, StringComparison.OrdinalIgnoreCase))?.RestaurantName ?? o.RestaurantId;
            string delivery = o.DeliveryDateTime.ToString("dd/MM/yyyy HH:mm");

            Console.WriteLine($"{o.OrderId,-8}  {custName,-17}  {restName,-16}  {delivery,-20}  ${o.TotalAmount,7:0.00}  {o.Status}");
        }
    }

    // ---------------- FEATURE 3: CREATE ----------------

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

        var foodList = rest.Menus.SelectMany(m => m.FoodItems).ToList();
        if (foodList.Count == 0)
        {
            Console.WriteLine("No food items available for this restaurant.");
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

            var existing = newOrder.Items.FirstOrDefault(x =>
                x.FoodItem.ItemName.Equals(fi.ItemName, StringComparison.OrdinalIgnoreCase));

            if (existing != null) existing.Quantity += qty;
            else newOrder.AddItem(new OrderedFoodItem(fi, qty));
        }

        if (newOrder.Items.Count == 0)
        {
            Console.WriteLine("No items selected. Order not created.");
            return;
        }

        string sr = ReadYesNo("Add special request? [Y/N]: ");
        if (sr == "Y")
            newOrder.SpecialRequest = ReadNonEmpty("Enter special request: ");

        // ----- totals  -----
        double itemsTotal = newOrder.Items.Sum(i => i.LineTotal());
        double discountAmount = 0.0;

        if (rest.SpecialOffers.Count > 0)
        {
            Console.WriteLine("Available Special Offers:");
            foreach (var so in rest.SpecialOffers)
                Console.WriteLine($"- {so.OfferCode}: {so.OfferDesc} ({so.Discount:0.#}%)");

            string useOffer = ReadYesNo("Apply a special offer? [Y/N]: ");
            if (useOffer == "Y")
            {
                string code = ReadNonEmpty("Enter offer code: ").ToUpper();

                var offer = rest.SpecialOffers
                    .FirstOrDefault(x => x.OfferCode.Equals(code, StringComparison.OrdinalIgnoreCase));

                if (offer != null)
                {
                    newOrder.AppliedOfferCode = offer.OfferCode;
                    newOrder.DiscountPercent = offer.Discount;

                    discountAmount = itemsTotal * (offer.Discount / 100.0);
                    Console.WriteLine($"Offer applied: -${discountAmount:0.00}");
                }
                else
                {
                    Console.WriteLine("Invalid offer code. No offer applied.");
                }
            }
        }


        double deliveryFee = 5.00;
        double finalTotal = (itemsTotal - discountAmount) + deliveryFee;

        newOrder.TotalAmount = Math.Round(finalTotal, 2);

        Console.WriteLine($"Items total: ${itemsTotal:0.00}");
        Console.WriteLine($"Discount:   -${discountAmount:0.00}");
        Console.WriteLine($"Delivery:   +${deliveryFee:0.00}");
        Console.WriteLine($"Final total: ${newOrder.TotalAmount:0.00}");

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

        orders.Add(newOrder);
        cust.AddOrder(newOrder);
        rest.OrderQueue.Enqueue(newOrder);

        CsvLoader.RewriteOrdersCsv(ordersCsvPath, orders);

        Console.WriteLine($"Order {newOrder.OrderId} created successfully! Status: {newOrder.Status}");
    }

    //  FEATURE 4: PROCESS  

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

        var o = rest.OrderQueue.Peek();

        Console.WriteLine($"Order {o.OrderId}:");
        Console.WriteLine($"Customer: {customers.FirstOrDefault(c => c.Email.Equals(o.CustomerEmail, StringComparison.OrdinalIgnoreCase))?.Name ?? o.CustomerEmail}");
        Console.WriteLine("Ordered Items:");
        o.DisplayItems();
        Console.WriteLine($"Delivery date/time: {o.DeliveryDateTime:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Total Amount: ${o.TotalAmount:0.00}");
        Console.WriteLine($"Order Status: {o.Status}");

        Console.Write("[C]onfirm / [R]eject / [D]eliver / [X]Cancel (skip): ");
        string action = (Console.ReadLine() ?? "").Trim().ToUpper();

        if (action == "C")
        {
            if (o.Status == "Pending")
            {
                o.Status = "Preparing";
                Console.WriteLine($"Order {o.OrderId} confirmed. Status: {o.Status}");
            }
            else Console.WriteLine("Only Pending orders can be confirmed.");
        }
        else if (action == "R")
        {
            if (o.Status == "Pending")
            {
                o.Status = "Rejected";
                refundStack.Push(o);
                Console.WriteLine($"Order {o.OrderId} rejected. Refund of ${o.TotalAmount:0.00} processed.");
            }
            else Console.WriteLine("Only Pending orders can be rejected.");
        }
        else if (action == "D")
        {
            if (o.Status == "Preparing")
            {
                o.Status = "Delivered";
                Console.WriteLine($"Order {o.OrderId} delivered. Status: {o.Status}");
            }
            else Console.WriteLine("Only Preparing orders can be delivered.");
        }
        else if (action == "X")
        {
            Console.WriteLine("No action taken (kept in queue).");
        }
        else
        {
            Console.WriteLine("Invalid action.");
        }

        if (o.Status == "Delivered" || o.Status == "Rejected")
        {
            rest.OrderQueue.Dequeue();
        }

        CsvLoader.RewriteOrdersCsv(ordersCsvPath, orders);
    }

    // FEATURE 5: MODIFY 

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

        var pending = cust.Orders.Where(o => o.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(o => o.OrderId)
                                 .ToList();
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
                var existing = order.Items.FirstOrDefault(x =>
                    x.FoodItem.ItemName.Equals(fi.ItemName, StringComparison.OrdinalIgnoreCase));

                if (existing != null) existing.Quantity += qty;
                else order.AddItem(new OrderedFoodItem(fi, qty));
            }

            if (order.Items.Count == 0)
            {
                Console.WriteLine("No items selected. Modification cancelled.");
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
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
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

        CsvLoader.RewriteOrdersCsv(ordersCsvPath, orders);
    }

    //FEATURE 6: DELETE/CANCEL 

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

        var pending = cust.Orders.Where(o => o.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(o => o.OrderId)
                                 .ToList();
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
        // Remove cancelled order from restaurant queue as well
        var rest = GetRestaurantById(order.RestaurantId);
        if (rest != null)
        {
            int n = rest.OrderQueue.Count;
            for (int i = 0; i < n; i++)
            {
                var q = rest.OrderQueue.Dequeue();
                if (q.OrderId != order.OrderId)
                    rest.OrderQueue.Enqueue(q);
            }
        }


        CsvLoader.RewriteOrdersCsv(ordersCsvPath, orders);

        Console.WriteLine($"Order {order.OrderId} cancelled. Refund of ${order.TotalAmount:0.00} processed.");
    }

    // EXTRA: VIEW REFUND STACK

    static void ViewRefundStack()
    {
        Console.WriteLine("Refund Stack (Latest First)");
        Console.WriteLine("===========================");

        if (refundStack.Count == 0)
        {
            Console.WriteLine("Refund stack is empty.");
            return;
        }

        int i = 1;
        foreach (var o in refundStack)
        {
            Console.WriteLine($"{i}. Order {o.OrderId} | {o.CustomerEmail} | {o.RestaurantId} | ${o.TotalAmount:0.00} | {o.Status}");
            i++;
        }
    }

    // ADVANCED (a): BULK PROCESS PENDING FOR CURRENT DAY 
    // - identify all orders with status "Pending" (in order queues) for current day
    // - show total number in order queues with this status
    // - for each such order:
    //      if delivery time < 1 hour => Rejected
    //      else => Preparing
    // - show summary stats (processed, Preparing vs Rejected, % auto processed vs all queue orders)
    static void BulkProcessPendingOrdersForCurrentDay()
    {
        Console.WriteLine("Advanced (a): Bulk process Pending orders for current day");
        Console.WriteLine("=========================================================");

        DateTime now = DateTime.Now;
        DateTime today = now.Date;

        int totalInAllQueues = restaurants.Sum(r => r.OrderQueue.Count);

        // count pending orders in queues for today
        int pendingTodayCount = 0;
        foreach (var r in restaurants)
        {
            foreach (var o in r.OrderQueue)
            {
                if (o.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                    o.DeliveryDateTime.Date == today)
                {
                    pendingTodayCount++;
                }
            }
        }

        Console.WriteLine($"Today: {today:dd/MM/yyyy}");
        Console.WriteLine($"Total orders in ALL queues: {totalInAllQueues}");
        Console.WriteLine($"Total PENDING orders in queues for today: {pendingTodayCount}");
        Console.WriteLine();

        if (pendingTodayCount == 0)
        {
            Console.WriteLine("No Pending orders for today to process.");
            return;
        }

        int processed = 0;
        int preparing = 0;
        int rejected = 0;

        foreach (var r in restaurants)
        {
            int n = r.OrderQueue.Count;
            for (int i = 0; i < n; i++)
            {
                var o = r.OrderQueue.Dequeue();

                bool isPendingToday =
                    o.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                    o.DeliveryDateTime.Date == today;

                if (!isPendingToday)
                {
                    // keep unchanged
                    r.OrderQueue.Enqueue(o);
                    continue;
                }

                processed++;

                TimeSpan diff = o.DeliveryDateTime - now;

                if (diff.TotalMinutes < 60)
                {
                    o.Status = "Rejected";
                    refundStack.Push(o);
                    rejected++;
                    // do NOT enqueue back (rejected removed from queue)
                }
                else
                {
                    o.Status = "Preparing";
                    preparing++;
                    // keep in queue
                    r.OrderQueue.Enqueue(o);
                }
            }
        }

        double percent = (totalInAllQueues == 0) ? 0.0 : (processed * 100.0 / totalInAllQueues);

        Console.WriteLine("Summary Statistics");
        Console.WriteLine("------------------");
        Console.WriteLine($"Orders processed (auto): {processed}");
        Console.WriteLine($"Preparing: {preparing}");
        Console.WriteLine($"Rejected:  {rejected}");
        Console.WriteLine($"% auto processed vs ALL queued orders: {percent:0.00}%");

        // persist
        CsvLoader.RewriteOrdersCsv(ordersCsvPath, orders);
    }

    // ADVANCED (b): DISPLAY TOTAL ORDER AMOUNT / REFUNDS / EARNINGS 
    // Requirement:
    // For each restaurant:
    //  - delivered orders total (LESS delivery fee per order)
    //  - refunded orders total
    // After all restaurants:
    //  - total order amount
    //  - total refunds
    //  - final amount Gruberoo earns
    static void AdvancedReport_OrderAmountRefundsEarnings()
    {
        Console.WriteLine("Advanced (b): Display total order amount / refunds / earnings");
        Console.WriteLine("=============================================================");

        const double deliveryFee = 5.00;

        double grandOrderAmount = 0.0; // delivered totals LESS delivery fee per order
        double grandRefunds = 0.0;
        int deliveredCount = 0;

        foreach (var r in restaurants)
        {
            var delivered = orders.Where(o =>
                o.RestaurantId.Equals(r.RestaurantId, StringComparison.OrdinalIgnoreCase) &&
                o.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase)).ToList();

            var refunded = orders.Where(o =>
                o.RestaurantId.Equals(r.RestaurantId, StringComparison.OrdinalIgnoreCase) &&
                (o.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                 o.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))).ToList();

            deliveredCount += delivered.Count;

            double deliveredLessFee = delivered.Sum(o => o.TotalAmount - deliveryFee);
            double refundsTotal = refunded.Sum(o => o.TotalAmount);

            grandOrderAmount += deliveredLessFee;
            grandRefunds += refundsTotal;

            Console.WriteLine($"\nRestaurant: {r.RestaurantName} ({r.RestaurantId})");
            Console.WriteLine($"Delivered order amount (less delivery fee): ${deliveredLessFee:0.00}");
            Console.WriteLine($"Total refunds: ${refundsTotal:0.00}");
        }

        double gruberooEarns = deliveredCount * deliveryFee;

        Console.WriteLine("\nOverall Totals");
        Console.WriteLine("--------------");
        Console.WriteLine($"Total order amount : ${grandOrderAmount:0.00}");
        Console.WriteLine($"Total refunds      : ${grandRefunds:0.00}");
        Console.WriteLine($"Final Gruberoo earns: ${gruberooEarns:0.00}");
    }

    // EXIT SAVE 

    static void ExitAndSave()
    {
        string queuePath = Path.Combine(baseDataPath, "queue.csv");
        string stackPath = Path.Combine(baseDataPath, "stack.csv");

        CsvLoader.SaveQueueToCsv(queuePath, restaurants);
        CsvLoader.SaveRefundStackToCsv(stackPath, refundStack);
        CsvLoader.RewriteOrdersCsv(ordersCsvPath, orders);

        Console.WriteLine("Saved queue.csv and stack.csv. Goodbye!");
    }
}
