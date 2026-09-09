namespace Intravision.Services.Contracts;
public interface IChangeCalculator
{
    Dictionary<decimal, int>? Calculate(decimal amount, IReadOnlyDictionary<decimal, int> available);
}
