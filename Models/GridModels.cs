namespace BlockBlastLite.Models;

public class GridCell
{
    public bool IsFilled { get; set; }
    public int ColorIndex { get; set; }
    public bool IsClearing { get; set; }
    public bool IsPreview { get; set; }
    public int PreviewColor { get; set; }
    public bool IsValidPlacementPreview { get; set; }
    public bool WillBeCleared { get; set; }

    public void Reset()
    {
        IsFilled = false;
        ColorIndex = 0;
        IsClearing = false;
        IsPreview = false;
        PreviewColor = 0;
        IsValidPlacementPreview = false;
        WillBeCleared = false;
    }
}

public enum ShoutoutType
{
    None,
    Good,
    Great,
    Impressive,
    Excellent,
    Amazing,
    Unbelievable,
    Insane
}

public class PlacementResult
{
    public bool Success { get; set; }
    public int PlacedBlocks { get; set; }
    public int ClearedLinesCount { get; set; }
    public List<int> ClearedRows { get; set; } = new();
    public List<int> ClearedCols { get; set; } = new();
    public int PointsEarned { get; set; }
    public int ComboStreak { get; set; }
    public bool IsGameOver { get; set; }
    public ShoutoutType Shoutout { get; set; } = ShoutoutType.None;
    public int ShoutoutBonusPoints { get; set; }
    public int ShoutoutMultiplier { get; set; } = 1;
}

public class DynamicPalette
{
    public string AccentPrimary { get; set; } = "#3B82F6";
    public string AccentSubtle { get; set; } = "#1E3A8A";
    public string AccentContainer { get; set; } = "#2563EB";
    public string Background { get; set; } = "#0F172A";
    public string Surface { get; set; } = "#1E293B";
    public string SurfaceVariant { get; set; } = "#334155";
    public string TextPrimary { get; set; } = "#F8FAFC";
    public bool IsDark { get; set; } = true;
}
