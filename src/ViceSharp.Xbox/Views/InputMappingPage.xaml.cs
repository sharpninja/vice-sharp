// PLAN-XBOXUWP S34 + FEAT-XCTRLBIND-001: input-mapping page code-behind.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ViceSharp.Xbox.Input;
using ViceSharp.Xbox.ViewModels;

/// <summary>Controls page: locked joystick map + remappable system buttons.</summary>
public sealed partial class InputMappingPage : Page
{
    /// <summary>The shared input-mapping ViewModel, bound by compiled {x:Bind}.</summary>
    public InputMappingViewModel ViewModel { get; }

    /// <summary>Creates the page and binds the shared InputMappingViewModel.</summary>
    public InputMappingPage()
    {
        ViewModel = App.Instance.InputMappingVm;
        DataContext = ViewModel;
        InitializeComponent();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(InputMappingViewModel.Rows) or nameof(InputMappingViewModel.StatusText))
                Bindings.Update();
        };
    }

    private void OnCycleCommand(object sender, RoutedEventArgs e)
    {
        if (MappingList.SelectedItem is not InputMappingRow row || row.IsLocked || row.Input is null)
            return;

        var input = row.Input.Value;
        var current = ViewModel.Profile.Gameplay.FirstOrDefault(b => b.Input == input);
        var currentCmd = current?.Command ?? AppCommand.None;
        var commands = InputMappingViewModel.RemappableCommands;
        var index = Array.IndexOf(commands, currentCmd);
        var next = commands[(index + 1) % commands.Length];
        ViewModel.TryRebind(input, next);
        Bindings.Update();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        ViewModel.Save();
        Bindings.Update();
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetToDefaults();
        Bindings.Update();
    }

    private void OnVirtualKeyboard(object sender, RoutedEventArgs e)
    {
        App.Instance.InputMappingVm.RequestOpenVirtualKeyboard();
        App.Instance.Navigation.IsVirtualKeyboardOpen = true;
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }
}
#endif
