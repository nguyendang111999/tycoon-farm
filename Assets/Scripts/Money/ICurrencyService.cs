using System;
using Farm.Core;

namespace Farm.Money
{
    /// <summary>Read-only view of currency balances, safe to hand out to systems that only display money.</summary>
    public interface ICurrencyReader
    {
        BigNumber GetBalance(CurrencyType type);
    }

    /// <summary>Full currency mutation API. Only gameplay systems that grant or charge money should hold this.</summary>
    public interface ICurrencyService : ICurrencyReader
    {
        /// <summary>Raised after a balance changes: (type, new balance, signed delta).</summary>
        event Action<CurrencyType, BigNumber, BigNumber> BalanceChanged;

        void Add(CurrencyType type, BigNumber amount);

        bool TrySpend(CurrencyType type, BigNumber amount);
    }
}
