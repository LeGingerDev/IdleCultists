using LargeNumbers;

public static class StatTypeExtensions
{
    public static bool IsGreaterThan(this StatType subject, StatType comparator)
    {
        AlphabeticNotation subjectValue = StatManager.Instance.QueryStat(subject);
        AlphabeticNotation comparatorValue = StatManager.Instance.QueryStat(comparator);
        return subjectValue > comparatorValue;
    }

    public static bool IsGreaterThanOrEqualTo(this StatType subject, StatType comparator)
    {
        AlphabeticNotation subjectValue = StatManager.Instance.QueryStat(subject);
        AlphabeticNotation comparatorValue = StatManager.Instance.QueryStat(comparator);
        return subjectValue >= comparatorValue;
    }

    public static bool IsLessThan(this StatType subject, StatType comparator)
    {
        AlphabeticNotation subjectValue = StatManager.Instance.QueryStat(subject);
        AlphabeticNotation comparatorValue = StatManager.Instance.QueryStat(comparator);
        return subjectValue < comparatorValue;
    }

    public static bool IsLessThanOrEqualTo(this StatType subject, StatType comparator)
    {
        AlphabeticNotation subjectValue = StatManager.Instance.QueryStat(subject);
        AlphabeticNotation comparatorValue = StatManager.Instance.QueryStat(comparator);
        return subjectValue <= comparatorValue;
    }

    public static bool IsEqualTo(this StatType subject, StatType comparator)
    {
        AlphabeticNotation subjectValue = StatManager.Instance.QueryStat(subject);
        AlphabeticNotation comparatorValue = StatManager.Instance.QueryStat(comparator);
        return subjectValue == comparatorValue;
    }

    public static bool IsNotEqualTo(this StatType subject, StatType comparator)
    {
        AlphabeticNotation subjectValue = StatManager.Instance.QueryStat(subject);
        AlphabeticNotation comparatorValue = StatManager.Instance.QueryStat(comparator);
        return subjectValue != comparatorValue;
    }
}