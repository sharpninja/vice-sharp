namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-LAUNCH-001. A neutral handle to a game that has been downloaded to local storage, ready to
/// be attached and booted by an <see cref="IGameLauncher"/>. It carries the local path and the media
/// nature but no engine or transport detail.
/// </summary>
/// <param name="LocalPath">The absolute path to the downloaded file on local storage.</param>
/// <param name="FileName">The bare file name.</param>
/// <param name="Kind">The media nature resolved from the extension.</param>
public sealed record AcquiredGame(string LocalPath, string FileName, MediaKind Kind);
