using Intravision.Models;
using Intravision.Services.Contracts;

namespace Intravision.Services;

public sealed class ChangeCalculator : IChangeCalculator
{
    public Dictionary<decimal, int>? Calculate(decimal amount, IReadOnlyDictionary<decimal, int> available)
    {
        if (amount < 0 || amount > MoneyLimits.MaxValue || decimal.Round(amount, 2) != amount) return null;
        if (amount == 0) return [];
        var coins = available.Where(c => c.Key > 0 && c.Value > 0).OrderByDescending(c => c.Key).ToArray();
        if (coins.Length == 0) return null;
        var values = coins.Select(c => checked((int)(c.Key * 100))).ToArray();
        var unit = values.Aggregate(Gcd);
        var cents = checked((long)(amount * 100));
        if (cents % unit != 0) return null;
        for (var i = 0; i < values.Length; i++) values[i] /= unit;
        var target = cents / unit;

        // Exchange whole bundles of a common value (10 rubles for 1/2/5/10).
        // Keep one extra bundle loose per denomination, so every partial bundle
        // remains representable. DP size depends on denominations, not balance.
        var common = values.Aggregate((a, b) => checked(a / Gcd(a, b) * b));
        var bundles = new int[coins.Length];
        var loose = new int[coins.Length];
        long totalBundles = 0;
        var looseTotal = 0;
        for (var i = 0; i < coins.Length; i++)
        {
            var size = common / values[i];
            bundles[i] = Math.Max(0, coins[i].Value / size - 1);
            loose[i] = coins[i].Value - bundles[i] * size;
            totalBundles += bundles[i];
            looseTotal = checked(looseTotal + loose[i] * values[i]);
        }
        var limit = (int)Math.Min(target, looseTotal);
        var reachable = new bool[limit + 1];
        reachable[0] = true;
        var choices = new List<int[]>();
        for (var i = 0; i < coins.Length; i++)
        {
            var used = new int[limit + 1];
            Array.Fill(used, -1);
            for (var sum = 0; sum <= limit; sum++)
            {
                if (reachable[sum]) used[sum] = 0;
                else if (sum >= values[i] && used[sum - values[i]] >= 0 && used[sum - values[i]] < loose[i])
                    used[sum] = used[sum - values[i]] + 1;
            }
            choices.Add(used);
            for (var sum = 0; sum <= limit; sum++) reachable[sum] = used[sum] >= 0;
        }
        var remainder = -1;
        for (var sum = 0; sum <= limit; sum++)
            if (reachable[sum] && (target - sum) % common == 0 && (target - sum) / common <= totalBundles)
            { remainder = sum; break; }
        if (remainder < 0) return null;
        var neededBundles = (target - remainder) / common;
        var result = new Dictionary<decimal, int>();
        for (var i = coins.Length - 1; i >= 0; i--)
        {
            var count = choices[i][remainder];
            if (count > 0) result[coins[i].Key] = count;
            remainder -= count * values[i];
        }
        for (var i = 0; i < coins.Length; i++)
        {
            var take = (int)Math.Min(neededBundles, bundles[i]);
            if (take > 0) result[coins[i].Key] = result.GetValueOrDefault(coins[i].Key) + take * (common / values[i]);
            neededBundles -= take;
        }
        return result;
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }
}