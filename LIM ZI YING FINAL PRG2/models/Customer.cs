namespace LIM_ZI_YING_FINAL_PRG2.models;

public class Customer
{
    public string Name { get; }
    public string Email { get; }

    public Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public override string ToString() => $"{Name} ({Email})";
}
