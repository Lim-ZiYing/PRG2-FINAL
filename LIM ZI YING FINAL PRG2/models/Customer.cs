namespace LIM_ZI_YING_FINAL_PRG2.models;

public class Customer
{
    public string Name { get; }
    public string Email { get; }

    private readonly List<Order> _orders = new();
    public IReadOnlyList<Order> Orders => _orders;

    public Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public void AddOrder(Order o) => _orders.Add(o);

    public override string ToString() => $"{Name} ({Email})";
}
