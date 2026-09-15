namespace Gb64Import;

public sealed class Gb64Nfo
{
    public string? GbVersion { get; set; }
    public string? Filename { get; set; }
    public string? Screenshot { get; set; }
    public string? Sid { get; set; }
    public string? UniqueId { get; set; }
    public string? Name { get; set; }
    public string? Language { get; set; }
    public string? Genre { get; set; }
    public string? Players { get; set; }
    public string? Control { get; set; }
    public string? Comment { get; set; }
    public string? Published { get; set; }
    public string? CrackedCrunched { get; set; }
    public string? Trainers { get; set; }
    public string? TrueDriveEmul { get; set; }
    public string? PalNtsc { get; set; }

    public static Gb64Nfo Parse(string text)
    {
        var nfo = new Gb64Nfo();
        foreach (var raw in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0 || line.StartsWith("====") || line.StartsWith("----"))
                continue;
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var key = line[..idx].Trim();
            var val = line[(idx + 1)..].Trim();
            switch (key)
            {
                case "GB-Version": nfo.GbVersion = val; break;
                case "Filename": nfo.Filename = val; break;
                case "Screenshot": nfo.Screenshot = val; break;
                // First SID: only (header). Later free-text blocks may repeat "SID:" (e.g. CGSC notes).
                case "SID":
                    if (string.IsNullOrWhiteSpace(nfo.Sid))
                        nfo.Sid = val;
                    break;
                case "Unique-ID": nfo.UniqueId = val; break;
                case "Name": nfo.Name = val; break;
                case "Language": nfo.Language = val; break;
                case "Genre": nfo.Genre = val; break;
                case "Players": nfo.Players = val; break;
                case "Control": nfo.Control = val; break;
                case "Comment": nfo.Comment = val; break;
                case "Published": nfo.Published = val; break;
                case "Cracked/Crunched": nfo.CrackedCrunched = val; break;
                case "Trainers": nfo.Trainers = val; break;
                case "True Drive Emul.": nfo.TrueDriveEmul = val; break;
                case "Pal/NTSC": nfo.PalNtsc = val; break;
            }
        }
        return nfo;
    }
}
