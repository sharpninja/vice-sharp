using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-ROMM-COVER-001 AC-COVER-03/04: an owned cover bitmap with latest-request-wins
/// publication. Detached/off-screen images cancel pending work and release their bitmap.
/// </summary>
public sealed class LibraryCoverImage : Image
{
    private CancellationTokenSource? _request;

    /// <summary>Clears the previous image and publishes only this request's result.</summary>
    public async Task LoadAsync(Func<CancellationToken, Task<Bitmap?>> loader)
    {
        ArgumentNullException.ThrowIfNull(loader);
        Clear();
        using var request = new CancellationTokenSource();
        _request = request;
        Bitmap? bitmap = null;
        try
        {
            bitmap = await loader(request.Token);
            if (ReferenceEquals(_request, request) && !request.IsCancellationRequested)
            {
                Source = bitmap;
                bitmap = null; // Ownership transfers to the displayed image.
            }
        }
        catch (Exception)
        {
            // A missing, canceled, malformed or failed cover leaves the text fallback visible.
        }
        finally
        {
            bitmap?.Dispose();
            if (ReferenceEquals(_request, request)) _request = null;
        }
    }

    /// <summary>Invalidates pending loads before releasing the currently displayed bitmap.</summary>
    public void Clear()
    {
        var request = _request;
        _request = null;
        request?.Cancel();
        var bitmap = Source as IDisposable;
        Source = null;
        bitmap?.Dispose();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Clear();
        base.OnDetachedFromVisualTree(e);
    }
}
