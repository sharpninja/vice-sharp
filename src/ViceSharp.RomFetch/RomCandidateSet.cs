using System.Text;

namespace ViceSharp.RomFetch;

/// <summary>
/// Mutable, per-instance overlay of ROM candidate filenames seeded from the
/// built-in <see cref="ViceRomCatalog"/>. Lets a user add their own candidate
/// dumps and reorder preference per (system, role); the resolver consults
/// <see cref="GetCandidates"/> for the effective, ordered list.
///
/// User customization can be driven programmatically (Add/Move/SetOrder) or via
/// a hand-editable text block (<see cref="ApplyOverrides"/> /
/// <see cref="RenderOverrides"/>) of the form
/// <c>&lt;system&gt;.&lt;role&gt; = file1, file2, ...</c> (most-preferred first).
/// </summary>
public sealed class RomCandidateSet
{
    private readonly Dictionary<string, Dictionary<string, List<string>>> _systems;

    /// <summary>Create a set seeded with the built-in catalog defaults.</summary>
    public RomCandidateSet()
    {
        _systems = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (system, info) in ViceRomCatalog.Systems)
        {
            var roles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (role, candidates) in info.Roles)
                roles[role] = [.. candidates];
            _systems[system] = roles;
        }
    }

    /// <summary>Effective candidate filenames for a system/role, most-preferred first.</summary>
    public IReadOnlyList<string> GetCandidates(string system, string role) =>
        _systems.TryGetValue(system, out var roles) && roles.TryGetValue(role, out var list)
            ? [.. list]
            : Array.Empty<string>();

    /// <summary>
    /// Add a candidate filename. Appends (lowest preference) by default, or
    /// inserts at <paramref name="index"/>. Returns false if blank or already
    /// present (case-insensitive). Creates the system/role entry if absent.
    /// </summary>
    public bool AddCandidate(string system, string role, string fileName, int index = -1)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var list = GetOrCreate(system, role);
        if (list.Any(n => string.Equals(n, fileName, StringComparison.OrdinalIgnoreCase)))
            return false;

        var trimmed = fileName.Trim();
        if (index < 0 || index >= list.Count)
            list.Add(trimmed);
        else
            list.Insert(index, trimmed);
        return true;
    }

    /// <summary>Move an existing candidate to a new preference index. Returns false if not found.</summary>
    public bool MoveCandidate(string system, string role, string fileName, int newIndex)
    {
        if (!_systems.TryGetValue(system, out var roles) || !roles.TryGetValue(role, out var list))
            return false;

        var current = list.FindIndex(n => string.Equals(n, fileName, StringComparison.OrdinalIgnoreCase));
        if (current < 0)
            return false;

        var value = list[current];
        list.RemoveAt(current);
        list.Insert(Math.Clamp(newIndex, 0, list.Count), value);
        return true;
    }

    /// <summary>Replace a role's candidates with a user-supplied order (blanks and case-insensitive dups dropped).</summary>
    public void SetOrder(string system, string role, IEnumerable<string> orderedFileNames)
    {
        ArgumentNullException.ThrowIfNull(orderedFileNames);

        var list = GetOrCreate(system, role);
        list.Clear();
        foreach (var name in orderedFileNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var trimmed = name.Trim();
            if (!list.Any(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase)))
                list.Add(trimmed);
        }
    }

    /// <summary>Remove a candidate. Returns true if anything was removed.</summary>
    public bool RemoveCandidate(string system, string role, string fileName) =>
        _systems.TryGetValue(system, out var roles)
        && roles.TryGetValue(role, out var list)
        && list.RemoveAll(n => string.Equals(n, fileName, StringComparison.OrdinalIgnoreCase)) > 0;

    /// <summary>Revert a role's candidates to the built-in catalog default.</summary>
    public void ResetRole(string system, string role)
    {
        var list = GetOrCreate(system, role);
        list.Clear();
        list.AddRange(ViceRomCatalog.Candidates(system, role));
    }

    /// <summary>
    /// Apply user overrides from a text block. Each non-comment line is
    /// <c>&lt;system&gt;.&lt;role&gt; = file1, file2, ...</c> and replaces that
    /// role's order (so the user can both add new dumps and reorder). Lines
    /// starting with '#' or ';' are comments.
    /// </summary>
    public void ApplyOverrides(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = line[..eq].Trim();
            var dot = key.IndexOf('.');
            if (dot <= 0 || dot >= key.Length - 1)
                continue;

            var system = key[..dot].Trim();
            var role = key[(dot + 1)..].Trim();
            var names = line[(eq + 1)..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            SetOrder(system, role, names);
        }
    }

    /// <summary>Render the current effective lists as an editable override text block.</summary>
    public string RenderOverrides()
    {
        var builder = new StringBuilder()
            .AppendLine("# ViceSharp ROM candidate overrides.")
            .AppendLine("# <system>.<role> = file1, file2, ...  (ordered, most-preferred first)");

        foreach (var system in _systems.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var role in _systems[system].Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                var list = _systems[system][role];
                if (list.Count == 0)
                    continue;
                builder.Append(system).Append('.').Append(role).Append(" = ").AppendLine(string.Join(", ", list));
            }
        }

        return builder.ToString();
    }

    private List<string> GetOrCreate(string system, string role)
    {
        if (!_systems.TryGetValue(system, out var roles))
        {
            roles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            _systems[system] = roles;
        }

        if (!roles.TryGetValue(role, out var list))
        {
            list = [];
            roles[role] = list;
        }

        return list;
    }
}
