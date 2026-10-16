namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-COVER-001. Opens a cover image stream for a <see cref="CoverRef"/>, applying the correct
/// auth rule (unauthenticated for <see cref="CoverRef.Url"/>, bearer for <see cref="CoverRef.Path"/>).
/// Implemented in the adapter and wrapped by a caching layer.
/// </summary>
public interface ILibraryImageLoader
{
    /// <summary>AC-COVER-01. Opens the cover image stream.</summary>
    /// <param name="cover">The cover reference.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Stream> OpenCoverAsync(CoverRef cover, CancellationToken cancellationToken = default);
}
