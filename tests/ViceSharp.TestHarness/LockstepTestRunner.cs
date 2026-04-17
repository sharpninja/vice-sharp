namespace ViceSharp.TestHarness;

using System.Reflection;
using ViceSharp.Abstractions;

/// <summary>
/// Cycle accurate lockstep test runner that compares chip state after every cycle
/// </summary>
public abstract class LockstepTestRunner<TChip> where TChip : IClockedDevice
{
    protected readonly TChip Chip;

    protected LockstepTestRunner(TChip chip)
    {
        Chip = chip;
    }

    /// <summary>
    /// Execute single cycle and compare state
    /// </summary>
    public TestResult StepAndCompare()
    {
        // Capture pre-state
        var beforeState = CaptureState(Chip);

        // Execute cycle
        Chip.Tick();

        // Capture post-state
        var afterState = CaptureState(Chip);

        // Get expected state from golden reference
        var expectedState = GetExpectedState();

        // Compare all fields
        return CompareStates(afterState, expectedState);
    }

    /// <summary>
    /// Reflection based state capture for all public and private fields
    /// </summary>
    protected Dictionary<string, object> CaptureState(object instance)
    {
        var state = new Dictionary<string, object>();
        var type = instance.GetType();

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            state[field.Name] = field.GetValue(instance)!;
        }

        return state;
    }

    /// <summary>
    /// Bitwise state comparison with exact matching
    /// </summary>
    protected TestResult CompareStates(Dictionary<string, object> actual, Dictionary<string, object> expected)
    {
        var differences = new List<StateDifference>();

        foreach (var key in expected.Keys)
        {
            if (!actual.TryGetValue(key, out var actualValue))
            {
                differences.Add(new StateDifference(key, "MISSING", expected[key]));
                continue;
            }

            if (!Equals(actualValue, expected[key]))
            {
                differences.Add(new StateDifference(key, actualValue, expected[key]));
            }
        }

        return differences.Count == 0
            ? TestResult.Pass()
            : TestResult.Fail(differences);
    }

    protected abstract Dictionary<string, object> GetExpectedState();
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
    public object Actual { get; }
    public object Expected { get; }

    public StateDifference(string fieldName, object actual, object expected)
    {
        FieldName = fieldName;
        Actual = actual;
        Expected = expected;
    }

    public override string ToString()
        => $"{FieldName}: Actual = {Actual}, Expected = {Expected}";
}