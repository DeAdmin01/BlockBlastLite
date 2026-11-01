using BlockBlastLite.Models;

namespace BlockBlastLite.Services;

public class GameEngine
{
    public const int BoardSize = 8;
    public GridCell[,] Board { get; private set; } = new GridCell[BoardSize, BoardSize];
    public List<GamePiece> CurrentPieces { get; private set; } = new();
    
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int ComboStreak { get; private set; }
    public int MaxCombo { get; private set; }
    public int TotalLinesCleared { get; private set; }
    public bool IsGameOver { get; private set; }

    private CancellationTokenSource? _comboCts;
    private readonly Queue<DateTime> _recentClearTimestamps = new();

    public event Action? OnStateChanged;
    public event Action<PlacementResult>? OnLinesCleared;
    public event Action? OnGameOver;

    public GameEngine()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                Board[r, c] = new GridCell();
            }
        }
    }

    public void StartOrRefreshComboTimer(int durationMs = 3400)
    {
        _comboCts?.Cancel();
        _comboCts?.Dispose();
        _comboCts = new CancellationTokenSource();
        var token = _comboCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                // Combo window: 3400ms on line clears, or 2000ms on strategic setup placements
                await Task.Delay(durationMs, token);

                // Gentle decay: step down combo multiplier every 1.0s once timer expires
                while (!token.IsCancellationRequested && ComboStreak > 0)
                {
                    ComboStreak--;
                    OnStateChanged?.Invoke();
                    if (ComboStreak > 0)
                    {
                        await Task.Delay(1000, token);
                    }
                }
            }
            catch (OperationCanceledException) { }
        });
    }

    public void StopComboTimer()
    {
        _comboCts?.Cancel();
        _comboCts?.Dispose();
        _comboCts = null;
        ComboStreak = 0;
    }

    private static readonly Random _rng = new();

    public void GenerateInitialBoardLayout()
    {
        // Variety of engaging, clean starter layouts (8-12 blocks)
        // Leaving 3x3 open areas, while seeding 1-2 lines with 5-6 blocks for early blast & clear potential
        int archetype = _rng.Next(4);
        var cells = new List<(int r, int c)>();

        switch (archetype)
        {
            case 0:
                // Cross Intersection: row 6 and col 3 intersecting
                cells.AddRange(new[] { (6, 1), (6, 2), (6, 3), (6, 4), (6, 5) });
                cells.AddRange(new[] { (2, 3), (3, 3), (4, 3), (5, 3) });
                break;
            case 1:
                // Dual Lines: row 2 and row 5 offset
                cells.AddRange(new[] { (2, 0), (2, 1), (2, 2), (2, 3), (2, 4) });
                cells.AddRange(new[] { (5, 3), (5, 4), (5, 5), (5, 6), (5, 7) });
                break;
            case 2:
                // Dual Wings: row 5 segment and col 6 segment
                cells.AddRange(new[] { (5, 2), (5, 3), (5, 4), (5, 5), (5, 6) });
                cells.AddRange(new[] { (1, 6), (2, 6), (3, 6), (4, 6) });
                break;
            default:
                // Near-Clear Cross: row 4 has 6 blocks, col 4 has 6 blocks, intersecting at (4,4)
                cells.AddRange(new[] { (4, 1), (4, 2), (4, 3), (4, 4), (4, 5), (4, 6) });
                cells.AddRange(new[] { (1, 4), (2, 4), (3, 4), (5, 4), (6, 4) });
                break;
        }

        foreach (var (r, c) in cells)
        {
            if (r >= 0 && r < BoardSize && c >= 0 && c < BoardSize)
            {
                Board[r, c].IsFilled = true;
                Board[r, c].ColorIndex = _rng.Next(1, 7);
            }
        }
    }

    public void StartNewGame(int savedHighScore = 0)
    {
        StopComboTimer();
        HighScore = Math.Max(HighScore, savedHighScore);
        Score = 0;
        ComboStreak = 0;
        MaxCombo = 0;
        TotalLinesCleared = 0;
        IsGameOver = false;
        _recentClearTimestamps.Clear();

        PieceLibrary.ResetPacing();

        // In 35% of games, guarantee that the opening board + opening trio can be 100% cleared (Clean Slate)
        bool guaranteeInitialClear = _rng.NextDouble() < 0.35;
        if (guaranteeInitialClear)
        {
            List<GamePiece>? cleanSlateTrio = null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                InitializeBoard();
                GenerateInitialBoardLayout();

                bool[,] boardFilled = new bool[BoardSize, BoardSize];
                for (int r = 0; r < BoardSize; r++)
                {
                    for (int c = 0; c < BoardSize; c++)
                    {
                        if (Board[r, c].IsFilled) boardFilled[r, c] = true;
                    }
                }

                cleanSlateTrio = PieceLibrary.TryFindCleanSlateTrio(boardFilled);
                if (cleanSlateTrio != null && cleanSlateTrio.Count == 3)
                {
                    break;
                }
            }

            if (cleanSlateTrio != null && cleanSlateTrio.Count == 3)
            {
                for (int i = cleanSlateTrio.Count - 1; i > 0; i--)
                {
                    int swap = _rng.Next(i + 1);
                    (cleanSlateTrio[i], cleanSlateTrio[swap]) = (cleanSlateTrio[swap], cleanSlateTrio[i]);
                }
                CurrentPieces = cleanSlateTrio;
            }
            else
            {
                CurrentPieces = PieceLibrary.GeneratePlayableTrio(Board);
            }
        }
        else
        {
            InitializeBoard();
            GenerateInitialBoardLayout();
            CurrentPieces = PieceLibrary.GeneratePlayableTrio(Board);
        }

        OnStateChanged?.Invoke();
    }

    public void SetHighScore(int highScore)
    {
        if (highScore > HighScore)
        {
            HighScore = highScore;
            OnStateChanged?.Invoke();
        }
    }

    public bool CanPlacePiece(GamePiece piece, int startRow, int startCol)
    {
        if (piece.IsUsed || IsGameOver) return false;

        int pRows = piece.Rows;
        int pCols = piece.Cols;

        // Check board boundaries
        if (startRow < 0 || startCol < 0 || startRow + pRows > BoardSize || startCol + pCols > BoardSize)
        {
            return false;
        }

        // Check cell collision
        for (int r = 0; r < pRows; r++)
        {
            for (int c = 0; c < pCols; c++)
            {
                if (piece.Shape[r, c])
                {
                    int targetRow = startRow + r;
                    int targetCol = startCol + c;

                    if (Board[targetRow, targetCol].IsFilled)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public bool CanPieceBePlacedAnywhere(GamePiece piece)
    {
        if (piece.IsUsed) return false;

        for (int r = 0; r <= BoardSize - piece.Rows; r++)
        {
            for (int c = 0; c <= BoardSize - piece.Cols; c++)
            {
                if (CanPlacePiece(piece, r, c))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool CheckHasAnyValidMoves()
    {
        var availablePieces = CurrentPieces.Where(p => !p.IsUsed).ToList();
        if (availablePieces.Count == 0) return true; // Will spawn new trio

        foreach (var piece in availablePieces)
        {
            if (CanPieceBePlacedAnywhere(piece))
            {
                return true;
            }
        }

        return false;
    }

    public PlacementResult PlacePiece(GamePiece piece, int startRow, int startCol)
    {
        var result = new PlacementResult();

        if (!CanPlacePiece(piece, startRow, startCol))
        {
            result.Success = false;
            return result;
        }

        result.Success = true;
        result.PlacedBlocks = piece.BlockCount;

        // 1. Fill cells with piece blocks
        for (int r = 0; r < piece.Rows; r++)
        {
            for (int c = 0; c < piece.Cols; c++)
            {
                if (piece.Shape[r, c])
                {
                    int boardRow = startRow + r;
                    int boardCol = startCol + c;
                    Board[boardRow, boardCol].IsFilled = true;
                    Board[boardRow, boardCol].ColorIndex = piece.ColorIndex;
                }
            }
        }

        piece.IsUsed = true;
        int points = piece.BlockCount;

        // 2. Identify full rows and columns
        var rowsToClear = new List<int>();
        var colsToClear = new List<int>();

        for (int r = 0; r < BoardSize; r++)
        {
            bool fullRow = true;
            for (int c = 0; c < BoardSize; c++)
            {
                if (!Board[r, c].IsFilled)
                {
                    fullRow = false;
                    break;
                }
            }
            if (fullRow) rowsToClear.Add(r);
        }

        for (int c = 0; c < BoardSize; c++)
        {
            bool fullCol = true;
            for (int r = 0; r < BoardSize; r++)
            {
                if (!Board[r, c].IsFilled)
                {
                    fullCol = false;
                    break;
                }
            }
            if (fullCol) colsToClear.Add(c);
        }

        int linesCleared = rowsToClear.Count + colsToClear.Count;
        result.ClearedLinesCount = linesCleared;
        result.ClearedRows = rowsToClear;
        result.ClearedCols = colsToClear;

        // 3. Score calculation, combo handling and Shoutout evaluation
        ShoutoutType shoutout = ShoutoutType.None;

        if (linesCleared > 0)
        {
            ComboStreak++;
            if (ComboStreak > MaxCombo) MaxCombo = ComboStreak;
            TotalLinesCleared += linesCleared;

            int linePoints = (linesCleared * (linesCleared + 1) / 2) * 10;
            int comboBonus = (ComboStreak > 1) ? (ComboStreak * 15 * linesCleared) : 0;
            points += linePoints + comboBonus;

            // Combo window: 3.4 seconds on line clear
            StartOrRefreshComboTimer(3400);

            // Trigger visual clearing animation on cells
            foreach (var r in rowsToClear)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    Board[r, c].IsClearing = true;
                }
            }
            foreach (var c in colsToClear)
            {
                for (int r = 0; r < BoardSize; r++)
                {
                    Board[r, c].IsClearing = true;
                }
            }

            // Check remaining cells after clearing to detect Clean Slate
            int remainingAfterClearing = 0;
            for (int r = 0; r < BoardSize; r++)
            {
                if (rowsToClear.Contains(r)) continue;
                for (int c = 0; c < BoardSize; c++)
                {
                    if (colsToClear.Contains(c)) continue;
                    if (Board[r, c].IsFilled) remainingAfterClearing++;
                }
            }

            // =========================================================================
            // Authentic Block Blast Voice Trigger Logic with Balanced Weighting
            // Voice Trigger = max(Weight(Lines Cleared), Weight(Combo Count))
            // =========================================================================

            // 1. Multi-clear Weight
            // 1 line: 0 (standard sound only)
            // 2 lines: 1 (Good!)
            // 3 lines: 2 (Great!)
            // 4 lines: 5 (Amazing!)
            // 5 lines: 6 (Unbelievable!)
            // Clean Slate (100% full board wipe): 6 (Unbelievable!), or 7 (Insane!) if combo streak >= 2
            // 6+ lines: 7 (Insane!)
            int linesWeight = 0;
            if (remainingAfterClearing == 0)
            {
                linesWeight = (ComboStreak >= 2) ? 7 : 6;
            }
            else if (linesCleared >= 6) linesWeight = 7;
            else if (linesCleared == 5) linesWeight = 6;
            else if (linesCleared == 4) linesWeight = 5;
            else if (linesCleared == 3) linesWeight = 2;
            else if (linesCleared == 2) linesWeight = 1;
            else linesWeight = 0;

            // 2. Combo Streak Weight (Milestone progression - eliminates voice spam on routine 1-line clears)
            // Triggers exclusively at milestones: 4, 7, 10, 13, 16 (and every 4 after 16)
            int comboWeight = 0;
            if (ComboStreak >= 16 && (ComboStreak == 16 || ComboStreak % 4 == 0)) comboWeight = 7;
            else if (ComboStreak == 13) comboWeight = 6;
            else if (ComboStreak == 10) comboWeight = 5;
            else if (ComboStreak == 7) comboWeight = 4;
            else if (ComboStreak == 4) comboWeight = 3;
            else comboWeight = 0;

            // 3. Priority Rule: Max of Lines Cleared vs Combo Streak
            int finalRank = Math.Max(linesWeight, comboWeight);

            shoutout = finalRank switch
            {
                7 => ShoutoutType.Insane,
                6 => ShoutoutType.Unbelievable,
                5 => ShoutoutType.Amazing,
                4 => ShoutoutType.Excellent,
                3 => ShoutoutType.Impressive,
                2 => ShoutoutType.Great,
                1 => ShoutoutType.Good,
                _ => ShoutoutType.None
            };
        }
        else
        {
            // Evaluate whether this placement was a strategic setup
            // (e.g. prepares a row/column to 7/8 filled, or tight survival fit on crowded board)
            int nearFullLines = 0; // lines with 7/8 filled
            int totalFilledCells = 0;
            for (int r = 0; r < BoardSize; r++)
            {
                int filledInRow = 0;
                for (int c = 0; c < BoardSize; c++)
                {
                    if (Board[r, c].IsFilled)
                    {
                        filledInRow++;
                        totalFilledCells++;
                    }
                }
                if (filledInRow == 7) nearFullLines++;
            }
            for (int c = 0; c < BoardSize; c++)
            {
                int filledInCol = 0;
                for (int r = 0; r < BoardSize; r++)
                {
                    if (Board[r, c].IsFilled) filledInCol++;
                }
                if (filledInCol == 7) nearFullLines++;
            }

            bool isStrategicSetup = nearFullLines >= 1 || (totalFilledCells > 35 && piece.BlockCount >= 4);

            if (isStrategicSetup)
            {
                // Strategic setup placement: grants a comfortable 2.0s grace period to keep the combo alive
                if (_comboCts != null && !_comboCts.IsCancellationRequested && ComboStreak > 0)
                {
                    StartOrRefreshComboTimer(2000);
                }

                // Non-clearing shoutout (subtle, only on truly exceptional setups)
                if (nearFullLines >= 3)
                {
                    shoutout = ShoutoutType.Impressive;
                }
                else if (nearFullLines == 2 && _rng.NextDouble() < 0.50)
                {
                    shoutout = ShoutoutType.Great;
                }
            }
            else
            {
                // Routine/random placement: does NOT extend combo timer
                // Combo timer continues running down without refresh
            }
        }

        // Calculate shoutout score bonus with combo multiplier
        int baseShoutoutBonus = shoutout switch
        {
            ShoutoutType.Good => 25,
            ShoutoutType.Great => 60,
            ShoutoutType.Impressive => 120,
            ShoutoutType.Excellent => 250,
            ShoutoutType.Amazing => 450,
            ShoutoutType.Unbelievable => 800,
            ShoutoutType.Insane => 1200,
            _ => 0
        };

        int multiplier = Math.Max(1, ComboStreak);
        int shoutoutBonus = baseShoutoutBonus * multiplier;
        points += shoutoutBonus;

        result.Shoutout = shoutout;
        result.ShoutoutBonusPoints = shoutoutBonus;
        result.ShoutoutMultiplier = multiplier;

        result.ComboStreak = ComboStreak;
        result.PointsEarned = points;
        Score += points;
        if (Score > HighScore)
        {
            HighScore = Score;
        }

        // 4. If no lines were cleared, finalize immediately; otherwise Home.razor calls FinishLineClearing after animation
        if (linesCleared == 0)
        {
            if (CurrentPieces.All(p => p.IsUsed))
            {
                CurrentPieces = PieceLibrary.GeneratePlayableTrio(Board);
            }

            if (!CheckHasAnyValidMoves())
            {
                IsGameOver = true;
                result.IsGameOver = true;
                OnGameOver?.Invoke();
            }
        }
        else
        {
            OnLinesCleared?.Invoke(result);
        }

        OnStateChanged?.Invoke();
        return result;
    }

    public bool FinishLineClearing(List<int> rowsToClear, List<int> colsToClear)
    {
        foreach (var r in rowsToClear)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                Board[r, c].Reset();
            }
        }

        foreach (var c in colsToClear)
        {
            for (int r = 0; r < BoardSize; r++)
            {
                Board[r, c].Reset();
            }
        }

        bool isBoardClear = true;
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                if (Board[r, c].IsFilled)
                {
                    isBoardClear = false;
                    break;
                }
            }
            if (!isBoardClear) break;
        }

        if (isBoardClear)
        {
            // Bonus points for Clean Slate are awarded exclusively via ShoutoutType.Unbelievable (+800 * multiplier) in PlacePiece
            HighScore = Math.Max(HighScore, Score);
        }

        if (CurrentPieces.All(p => p.IsUsed))
        {
            CurrentPieces = PieceLibrary.GeneratePlayableTrio(Board);
        }

        if (!CheckHasAnyValidMoves())
        {
            IsGameOver = true;
            OnGameOver?.Invoke();
        }

        OnStateChanged?.Invoke();
        return isBoardClear;
    }

    public void ClearPreview()
    {
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                Board[r, c].IsPreview = false;
                Board[r, c].PreviewColor = 0;
                Board[r, c].IsValidPlacementPreview = false;
                Board[r, c].WillBeCleared = false;
            }
        }
    }

    public void SetPreview(GamePiece piece, int startRow, int startCol)
    {
        ClearPreview();
        bool valid = CanPlacePiece(piece, startRow, startCol);

        int pRows = piece.Rows;
        int pCols = piece.Cols;

        for (int r = 0; r < pRows; r++)
        {
            for (int c = 0; c < pCols; c++)
            {
                if (piece.Shape[r, c])
                {
                    int br = startRow + r;
                    int bc = startCol + c;
                    if (br >= 0 && br < BoardSize && bc >= 0 && bc < BoardSize)
                    {
                        Board[br, bc].IsPreview = true;
                        Board[br, bc].PreviewColor = piece.ColorIndex;
                        Board[br, bc].IsValidPlacementPreview = valid;
                    }
                }
            }
        }

        // Highlight cells in rows/columns that WILL be cleared if placed here
        if (valid)
        {
            bool[,] sim = new bool[BoardSize, BoardSize];
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    sim[r, c] = Board[r, c].IsFilled;
                }
            }
            for (int r = 0; r < pRows; r++)
            {
                for (int c = 0; c < pCols; c++)
                {
                    if (piece.Shape[r, c])
                    {
                        sim[startRow + r, startCol + c] = true;
                    }
                }
            }

            var willClearRows = new List<int>();
            for (int r = 0; r < BoardSize; r++)
            {
                bool full = true;
                for (int c = 0; c < BoardSize; c++)
                {
                    if (!sim[r, c]) { full = false; break; }
                }
                if (full) willClearRows.Add(r);
            }

            var willClearCols = new List<int>();
            for (int c = 0; c < BoardSize; c++)
            {
                bool full = true;
                for (int r = 0; r < BoardSize; r++)
                {
                    if (!sim[r, c]) { full = false; break; }
                }
                if (full) willClearCols.Add(c);
            }

            foreach (var r in willClearRows)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    Board[r, c].WillBeCleared = true;
                }
            }
            foreach (var c in willClearCols)
            {
                for (int r = 0; r < BoardSize; r++)
                {
                    Board[r, c].WillBeCleared = true;
                }
            }
        }

        OnStateChanged?.Invoke();
    }
}
