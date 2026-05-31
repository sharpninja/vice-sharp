using System.ComponentModel;
using System.Runtime.CompilerServices;
using ViceSharp.Architectures.Adhoc;

namespace ViceSharp.AdhocHelper;

/// <summary>
/// Headless view-model that drives the ad-hoc machine YAML helper window.
/// Wraps <see cref="AdhocMachineYamlLoader"/> so the editor / validate /
/// open / save commands can be unit tested without spinning up Avalonia.
/// </summary>
/// <remarks>
/// The view-model deliberately exposes only plain CLR properties so xUnit
/// tests can flip <see cref="YamlText"/>, call <see cref="Validate"/>, and
/// then read <see cref="ValidationMessage"/> without any UI thread or
/// Avalonia dispatcher infrastructure.
/// </remarks>
public sealed class AdhocHelperViewModel : INotifyPropertyChanged
{
    private readonly AdhocMachineYamlLoader _loader = new();
    private string _yamlText = string.Empty;
    private string? _validationMessage;
    private string? _currentFilePath;
    private bool _isValid;

    /// <summary>The YAML source text shown in the editor.</summary>
    public string YamlText
    {
        get => _yamlText;
        set
        {
            if (_yamlText == value) return;
            _yamlText = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Result of the last <see cref="Validate"/> call. Either an
    /// <c>"OK: ..."</c> summary or the loader's exception message.
    /// </summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage == value) return;
            _validationMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// True when the last <see cref="Validate"/> call produced an
    /// "OK" message. The Save command can use this to gate writes if it
    /// wants to refuse to save invalid documents.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set
        {
            if (_isValid == value) return;
            _isValid = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Path of the currently loaded document, or null if the editor
    /// holds an unsaved buffer.
    /// </summary>
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            if (_currentFilePath == value) return;
            _currentFilePath = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Validate <see cref="YamlText"/> using <see cref="AdhocMachineYamlLoader"/>
    /// and update <see cref="ValidationMessage"/> with either an OK summary
    /// or the validation error message.
    /// </summary>
    public void Validate()
    {
        try
        {
            var blueprint = _loader.LoadFromString(YamlText ?? string.Empty);

            // We need region + chip counts. Re-parse just for counts via the
            // public surface: the blueprint hides the plan, so we use a
            // second pass through the loader's document binder via reflection
            // on the public descriptor + best-effort secondary parse.
            var (chipCount, regionCount) = CountChipsAndRegions(YamlText ?? string.Empty);

            ValidationMessage =
                $"OK: machine '{blueprint.Descriptor.MachineName}', {chipCount} chips, {regionCount} regions";
            IsValid = true;
        }
        catch (AdhocMachineValidationException ex)
        {
            ValidationMessage = ex.Message;
            IsValid = false;
        }
    }

    /// <summary>
    /// Load YAML from disk into <see cref="YamlText"/> and update
    /// <see cref="CurrentFilePath"/>. Does not validate; call
    /// <see cref="Validate"/> afterwards if needed.
    /// </summary>
    public void OpenFromPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        YamlText = File.ReadAllText(path);
        CurrentFilePath = path;
        ValidationMessage = null;
        IsValid = false;
    }

    /// <summary>
    /// Save the current <see cref="YamlText"/> to <paramref name="path"/>
    /// and remember it as <see cref="CurrentFilePath"/>.
    /// </summary>
    public void SaveToPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        File.WriteAllText(path, YamlText ?? string.Empty);
        CurrentFilePath = path;
    }

    private static (int Chips, int Regions) CountChipsAndRegions(string yaml)
    {
        // The loader does not expose the parsed document, so we do a
        // lightweight second pass with YamlDotNet's representation model
        // to count chips[] and memory.regions[] entries. If anything goes
        // wrong here we fall back to (0, 0); the validate path already
        // returned OK so the YAML is structurally sound.
        try
        {
            var stream = new YamlDotNet.RepresentationModel.YamlStream();
            stream.Load(new StringReader(yaml));
            if (stream.Documents.Count == 0) return (0, 0);
            if (stream.Documents[0].RootNode is not YamlDotNet.RepresentationModel.YamlMappingNode root)
                return (0, 0);

            var chipCount = 0;
            var regionCount = 0;
            foreach (var entry in root.Children)
            {
                if (entry.Key is not YamlDotNet.RepresentationModel.YamlScalarNode keyNode)
                    continue;
                if (keyNode.Value == "chips" && entry.Value is YamlDotNet.RepresentationModel.YamlSequenceNode chipsSeq)
                {
                    chipCount = chipsSeq.Children.Count;
                }
                else if (keyNode.Value == "memory" && entry.Value is YamlDotNet.RepresentationModel.YamlMappingNode memMap)
                {
                    foreach (var memEntry in memMap.Children)
                    {
                        if (memEntry.Key is YamlDotNet.RepresentationModel.YamlScalarNode { Value: "regions" }
                            && memEntry.Value is YamlDotNet.RepresentationModel.YamlSequenceNode regSeq)
                        {
                            regionCount = regSeq.Children.Count;
                        }
                    }
                }
            }
            return (chipCount, regionCount);
        }
        catch
        {
            return (0, 0);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
