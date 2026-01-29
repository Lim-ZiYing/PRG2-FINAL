namespace LIM_ZI_YING_FINAL_PRG2.models;

public class SpecialOffer
{
    public string OfferCode { get; }
    public string OfferDesc { get; }
    public double Discount { get; }

    public SpecialOffer(string code, string desc, double discount)
    {
        OfferCode = code;
        OfferDesc = desc;
        Discount = discount;
    }

    public override string ToString() => $"{OfferCode}: {OfferDesc} ({Discount:0.#}%)";
}
