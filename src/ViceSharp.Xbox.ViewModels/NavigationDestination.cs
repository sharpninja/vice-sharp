namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// The PUSHABLE full-screen pages of the 10-foot (couch) UI that the shell can
/// navigate between with a controller. Each value is a distinct top-level page the
/// <see cref="NavigationViewModel"/> pushes onto (and pops off) its explicit back
/// stack.
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S20 (IMPL-XBOXUWP-020), FR-XBOXUI-001 / TR-XBOXUI-001. This is the
/// reconciled page set that replaces the S19 placeholder
/// (<c>Gameplay, MainMenu, Settings, Devices, Roms, VirtualKeyboard</c>), which
/// conflated three different concepts. In the real topology:
/// </para>
/// <list type="bullet">
///   <item><description>
///   The always-present In-emulator (gameplay) view is NOT a destination: it is the
///   permanent video surface that the pages overlay, modeled as
///   <see cref="NavigationViewModel.Current"/> == <c>null</c> (an empty back stack),
///   and it maps to <see cref="ViceSharp.Xbox.Input.InputContext.Gameplay"/>.
///   </description></item>
///   <item><description>
///   The quick menu and the on-screen virtual keyboard are OVERLAY FLAGS
///   (<see cref="NavigationViewModel.IsQuickMenuOpen"/> /
///   <see cref="NavigationViewModel.IsVirtualKeyboardOpen"/>), not stack entries, so
///   the Back control never dismisses an overlay by popping a page.
///   </description></item>
///   <item><description>
///   ROM provisioning is hosted INSIDE <see cref="DeviceSetup"/> (there is no
///   first-class <c>Roms</c> page), keeping the media-attach and ROM-set surfaces on
///   one device page.
///   </description></item>
/// </list>
/// </remarks>
public enum NavigationDestination
{
    /// <summary>The launch / home page (game library and top-level entry points).</summary>
    Home,

    /// <summary>The settings page (display / audio / input / resource options).</summary>
    Settings,

    /// <summary>
    /// The device-setup page: attach, eject, and swap media in the drive/tape/cartridge
    /// slots, choose the drive model, and provision the machine ROM set.
    /// </summary>
    DeviceSetup,

    /// <summary>The input-mapping page: view and rebind the controller bindings.</summary>
    InputMapping,

    /// <summary>The about page: GPL-2.0-or-later disclosure, VICE attribution, and source offer.</summary>
    About,
}
