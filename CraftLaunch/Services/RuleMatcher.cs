using CraftLaunch.Models;

namespace CraftLaunch.Services;

public static class RuleMatcher
{
    public static bool IsAllowed(List<MojangRule>? rules)
    {
        if (rules is null || rules.Count == 0)
            return true;

        var allowed = false;
        foreach (var rule in rules)
        {
            if (rule.Features is { Count: > 0 })
                continue;
            if (rule.Os?.Arch == "x86")
                continue;
            if (rule.Os?.Name is not (null or "" or "windows"))
                continue;
            allowed = rule.Action == "allow";
        }
        return allowed;
    }
}
