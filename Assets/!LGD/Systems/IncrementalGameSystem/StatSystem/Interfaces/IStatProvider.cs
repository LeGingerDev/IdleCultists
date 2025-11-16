using System.Collections.Generic;

public interface IStatProvider
{
    List<StatModifier> GetModifiersForStat(StatType statType);
}