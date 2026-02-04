using System;
using System.Linq;
using System.Collections.Generic;
namespace LIM_ZI_YING_FINAL_PRG2.models;

public class Order
{
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string RestaurantId { get; set; } = "";

    public DateTime DeliveryDateTime { get; set; }
    public string DeliveryAddress { get; set; } = "";

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;

    public string PaymentMethod { get; set; } = ""; // CC / PP / CD
    public string Status { get; set; } = "Pending";

    public string? SpecialRequest { get; set; }

    // ✅ Keep discount info so Modify recalculations are consistent
    public string? AppliedOfferCode { get; set; }   // e.g. "OFF10"
    public double DiscountPercent { get; set; }     // e.g. 10 means 10%

    private readonly List<OrderedFoodItem> _items = new();
    public IReadOnlyList<OrderedFoodItem> Items => _items;

    public double TotalAmount { get; set; } // includes delivery fee, after discount

    public void ClearItems() => _items.Clear();
    public void AddItem(OrderedFoodItem item) => _items.Add(item);

    public double CalculateTotal()
    {
        const double deliveryFee = 5.00;

        double itemsTotal = _items.Sum(i => i.LineTotal());
        double discountAmount = 0.0;

        if (DiscountPercent > 0)
        {
            discountAmount = itemsTotal * (DiscountPercent / 100.0);
        }

        TotalAmount = Math.Round((itemsTotal - discountAmount) + deliveryFee, 2);
        return TotalAmount;
    }

    public void DisplayItems()
    {
        for (int i = 0; i < _items.Count; i++)
            Console.WriteLine($"{i + 1}. {_items[i]}");

        if (!string.IsNullOrWhiteSpace(SpecialRequest))
            Console.WriteLine($"Special request: {SpecialRequest}");

        if (!string.IsNullOrWhiteSpace(AppliedOfferCode) && DiscountPercent > 0)
            Console.WriteLine($"Offer applied: {AppliedOfferCode} ({DiscountPercent:0.#}%)");
    }
}
