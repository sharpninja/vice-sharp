namespace ViceSharp.TestHarness;

using System.Reflection;
using ViceSharp.Abstractions;

/// <summary>
/// Cycle accurate lockstep test runner that compares chip state after every cycle
/// </summary>
public abstract class LockstepTestRunner<TChip> where TChip : class, IClockedDevice
{
    protected TChip Chip { get; private set; } = null!;

    /// <summary>
    /// Execute single cycle and compare state
    /// </summary>
    public TestResult StepAndCompare()
    {
        EnsureInitialized();
        StepManaged();
        return CompareStates(GetActualState(), GetExpectedState());
    }

    protected void InitializeChip(TChip chip)
    {
        Chip = chip ?? throw new ArgumentNullException(nameof(chip));
    }

    protected virtual void StepManaged() => Chip.Tick();

    protected virtual Dictionary<string, object?> GetActualState() => CaptureState(Chip);

    /// <summary>
    /// Reflection based state capture for all public and private fields
    /// </summary>
    protected Dictionary<string, object?> CaptureState(object instance)
    {
        var state = new Dictionary<string, object?>();
        var type = instance.GetType();

        while (type is not null)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                state.TryAdd(field.Name, field.GetValue(instance));
            }

            type = type.BaseType;
        }

        return state;
    }

    /// <summary>
    /// Bitwise state comparison with exact matching
    /// </summary>
    protected TestResult CompareStates(Dictionary<string, object?> actual, Dictionary<string, object?> expected)
    {
        var differences = new List<StateDifference>();

        foreach (var key in expected.Keys)
        {
            if (!actual.TryGetValue(key, out var actualValue))
            {
                differences.Add(new StateDifference(key, "MISSING", expected[key]));
                continue;
            }

            if (!ValuesEqual(actualValue, expected[key]))
            {
                differences.Add(new StateDifference(key, actualValue, expected[key]));
            }
        }

        return differences.Count == 0
            ? TestResult.Pass()
            : TestResult.Fail(differences);
    }

    protected abstract Dictionary<string, object?> GetExpectedState();

    private void EnsureInitialized()
    {
        if (Chip is null)
            throw new InvalidOperationException($"{GetType().Name} must call {nameof(InitializeChip)} before use.");
    }

    private static bool ValuesEqual(object? actual, object? expected)
    {
        if (ReferenceEquals(actual, expected))
            return true;

        if (actual is null || expected is null)
            return false;

        if (actual is Array actualArray && expected is Array expectedArray)
        {
            if (actualArray.Length != expectedArray.Length)
                return false;

            for (var i = 0; i < actualArray.Length; i++)
            {
                if (!ValuesEqual(actualArray.GetValue(i), expectedArray.GetValue(i)))
                    return false;
            }

            return true;
        }

        return Equals(actual, expected);
    }
}

public readonly struct TestResult
{
    public bool Passed { get; }
    public IReadOnlyList<StateDifference> Differences { get; }

    private TestResult(bool passed, List<StateDifference> differences)
    {
        Passed = passed;
        Differences = differences;
    }

    public static TestResult Pass() => new(true, new List<StateDifference>());
    public static TestResult Fail(List<StateDifference> differences) => new(false, differences);
}

public readonly struct StateDifference
{
    public string FieldName { get; }
    public object? Actual { get; }
    public object? Expected { get; }

    public StateDifference(string fieldName, object? actual, object? expected)
    {
        FieldName = fieldName;
        Actual = actual;
        Expected = expected;
    }

    public override string ToString()
        => $"{FieldName}: Actual = {Actual}, Expected = {Expected}";
}
