using System.Collections.Generic;

/// <summary>
/// Interface for components that provide barks.
/// Used by CentralizedBarkProvider to query BarkRegistry.
/// </summary>
public interface IBarkProvider
{
    List<string> GetBarksForContext(BarkContext context);
    int GetPriority();
}