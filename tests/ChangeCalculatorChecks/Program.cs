using Intravision.Services;

var calculator = new ChangeCalculator();
var random = new Random(42);
var checks = 0;
foreach (var unit in new[] { 1m, 0.25m })
{
    for (var iteration = 0; iteration < 400; iteration++)
    {
        var values = new[] { 1, 2, 5, 10 };
        var counts = values.Select(_ => random.Next(0, 16)).ToArray();
        var total = values.Select((v, i) => v * counts[i]).Sum();
        var possible = new bool[total + 11];
        possible[0] = true;
        for (var i = 0; i < values.Length; i++)
            for (var n = 0; n < counts[i]; n++)
                for (var sum = possible.Length - 1; sum >= values[i]; sum--)
                    possible[sum] |= possible[sum - values[i]];
        var inventory = values.Select((v, i) => (v, i)).ToDictionary(x => x.v * unit, x => counts[x.i]);
        for (var amount = 0; amount < possible.Length; amount++)
        {
            var result = calculator.Calculate(amount * unit, inventory);
            if ((result is not null) != possible[amount]) throw new Exception($"Wrong reachability: {amount * unit}, {string.Join(',', counts)}");
            if (result is not null && (result.Sum(c => c.Key * c.Value) != amount * unit || result.Any(c => c.Value <= 0 || c.Value > inventory[c.Key])))
                throw new Exception("Invalid payout");
            checks++;
        }
    }
}
var large = calculator.Calculate(99999999m, new Dictionary<decimal, int> { [10m] = 9999999, [1m] = 9 });
if (large is null || large.Sum(c => c.Key * c.Value) != 99999999m) throw new Exception("Large payout failed");
if (calculator.Calculate(0.5m, new Dictionary<decimal, int> { [1m] = 100 }) is not null) throw new Exception("Fractional change should be impossible");
Console.WriteLine($"PASS: {checks} comparisons against exhaustive change oracle, fractional coins, large payout and unavailable change.");
