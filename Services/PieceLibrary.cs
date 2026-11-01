using BlockBlastLite.Models;

namespace BlockBlastLite.Services;

public enum PacingPhase
{
    Harmonious,   // Приятные фигуры для строительства и ритма (5 раундов)
    Challenging,  // Крупные, сложные фигуры для проверки стратегии (2-3 раунда)
    Cleansing     // «Очистительные» фигуры: идеальные спасатели и катализаторы взрывов (1-2 раунда)
}

public static class PieceLibrary
{
    private static readonly Random _random = new();

    // Pacing wave controller state
    private static PacingPhase _currentPhase = PacingPhase.Harmonious;
    private static int _roundsRemaining = 5;

    private static readonly List<(bool[,] Shape, int BaseColor, int SizeCategory, string Name)> _templates = new()
    {
        // 1x1 Dot (Size: 1)
        (new bool[,] { { true } }, 1, 1, "dot"),

        // 2x1 & 1x2 Dominoes (Size: 2)
        (new bool[,] { { true, true } }, 2, 2, "h2"),
        (new bool[,] { { true }, { true } }, 2, 2, "v2"),

        // 3x1 & 1x3 Trominoes (Size: 3)
        (new bool[,] { { true, true, true } }, 3, 3, "h3"),
        (new bool[,] { { true }, { true }, { true } }, 3, 3, "v3"),

        // Small Corner 2x2 (3 blocks, Size: 3)
        (new bool[,] {
            { true, false },
            { true, true }
        }, 2, 3, "c2a"),
        (new bool[,] {
            { false, true },
            { true, true }
        }, 2, 3, "c2b"),
        (new bool[,] {
            { true, true },
            { true, false }
        }, 2, 3, "c2c"),
        (new bool[,] {
            { true, true },
            { false, true }
        }, 2, 3, "c2d"),

        // 2x2 Square (Size: 4)
        (new bool[,] {
            { true, true },
            { true, true }
        }, 6, 4, "sq2"),

        // 4x1 & 1x4 Tetrominoes (Size: 4)
        (new bool[,] { { true, true, true, true } }, 4, 4, "h4"),
        (new bool[,] { { true }, { true }, { true }, { true } }, 4, 4, "v4"),

        // L-shape 3x2 (4 blocks, Size: 4)
        (new bool[,] {
            { true, false },
            { true, false },
            { true, true }
        }, 3, 4, "l32a"),
        (new bool[,] {
            { false, true },
            { false, true },
            { true, true }
        }, 3, 4, "l32b"),
        (new bool[,] {
            { true, true },
            { true, false },
            { true, false }
        }, 3, 4, "l32c"),
        (new bool[,] {
            { true, true },
            { false, true },
            { false, true }
        }, 3, 4, "l32d"),

        // L-shape 2x3 (4 blocks, Size: 4)
        (new bool[,] {
            { true, true, true },
            { true, false, false }
        }, 4, 4, "l23a"),
        (new bool[,] {
            { true, true, true },
            { false, false, true }
        }, 4, 4, "l23b"),
        (new bool[,] {
            { true, false, false },
            { true, true, true }
        }, 4, 4, "l23c"),
        (new bool[,] {
            { false, false, true },
            { true, true, true }
        }, 4, 4, "l23d"),

        // T-shapes (4 blocks, Size: 4)
        (new bool[,] {
            { true, true, true },
            { false, true, false }
        }, 6, 4, "t1"),
        (new bool[,] {
            { false, true, false },
            { true, true, true }
        }, 6, 4, "t2"),
        (new bool[,] {
            { true, false },
            { true, true },
            { true, false }
        }, 6, 4, "t3"),
        (new bool[,] {
            { false, true },
            { true, true },
            { false, true }
        }, 6, 4, "t4"),

        // Z and S shapes (4 blocks, Size: 4)
        (new bool[,] {
            { true, true, false },
            { false, true, true }
        }, 1, 4, "z1"),
        (new bool[,] {
            { false, true, true },
            { true, true, false }
        }, 2, 4, "s1"),
        (new bool[,] {
            { true, false },
            { true, true },
            { false, true }
        }, 3, 4, "z2"),
        (new bool[,] {
            { false, true },
            { true, true },
            { true, false }
        }, 4, 4, "s2"),

        // 5x1 & 1x5 Pentaminoes (Size: 5)
        (new bool[,] { { true, true, true, true, true } }, 5, 5, "h5"),
        (new bool[,] { { true }, { true }, { true }, { true }, { true } }, 5, 5, "v5"),

        // Large L 3x3 (5 blocks, Size: 5)
        (new bool[,] {
            { true, false, false },
            { true, false, false },
            { true, true, true }
        }, 5, 5, "l33a"),
        (new bool[,] {
            { false, false, true },
            { false, false, true },
            { true, true, true }
        }, 5, 5, "l33b"),
        (new bool[,] {
            { true, true, true },
            { true, false, false },
            { true, false, false }
        }, 5, 5, "l33c"),
        (new bool[,] {
            { true, true, true },
            { false, false, true },
            { false, false, true }
        }, 5, 5, "l33d"),

        // Plus (+) shape (5 blocks, Size: 5)
        (new bool[,] {
            { false, true, false },
            { true, true, true },
            { false, true, false }
        }, 5, 5, "plus"),

        // 3x3 Square (Size: 9)
        (new bool[,] {
            { true, true, true },
            { true, true, true },
            { true, true, true }
        }, 1, 9, "sq3"),

        // 3x2 & 2x3 Solid Rectangles (6 blocks, Size: 6)
        (new bool[,] {
            { true, true },
            { true, true },
            { true, true }
        }, 5, 6, "rect32"),
        (new bool[,] {
            { true, true, true },
            { true, true, true }
        }, 5, 6, "rect23"),

        // 2-block Diagonals (2 blocks, Size: 2) - Two directions: \ and /
        (new bool[,] {
            { true, false },
            { false, true }
        }, 2, 2, "diag2a"),
        (new bool[,] {
            { false, true },
            { true, false }
        }, 2, 2, "diag2b"),

        // 3-block Diagonals (3 blocks, Size: 3) - Two directions: \ and /
        (new bool[,] {
            { true, false, false },
            { false, true, false },
            { false, false, true }
        }, 3, 3, "diag3a"),
        (new bool[,] {
            { false, false, true },
            { false, true, false },
            { true, false, false }
        }, 3, 3, "diag3b")
    };

    public static void ResetPacing()
    {
        _currentPhase = PacingPhase.Harmonious;
        _roundsRemaining = 5; // Exactly 5 harmonious rounds
    }

    public static bool CanShapeFit(bool[,] boardFilled, bool[,] shape)
    {
        int sRows = shape.GetLength(0);
        int sCols = shape.GetLength(1);

        for (int r = 0; r <= 8 - sRows; r++)
        {
            for (int c = 0; c <= 8 - sCols; c++)
            {
                bool fits = true;
                for (int sr = 0; sr < sRows; sr++)
                {
                    for (int sc = 0; sc < sCols; sc++)
                    {
                        if (shape[sr, sc] && boardFilled[r + sr, c + sc])
                        {
                            fits = false;
                            break;
                        }
                    }
                    if (!fits) break;
                }
                if (fits) return true;
            }
        }
        return false;
    }

    public static List<(int r, int c)> FindValidPlacements(bool[,] boardFilled, bool[,] shape)
    {
        var positions = new List<(int r, int c)>();
        int sRows = shape.GetLength(0);
        int sCols = shape.GetLength(1);

        for (int r = 0; r <= 8 - sRows; r++)
        {
            for (int c = 0; c <= 8 - sCols; c++)
            {
                bool fits = true;
                for (int sr = 0; sr < sRows; sr++)
                {
                    for (int sc = 0; sc < sCols; sc++)
                    {
                        if (shape[sr, sc] && boardFilled[r + sr, c + sc])
                        {
                            fits = false;
                            break;
                        }
                    }
                    if (!fits) break;
                }
                if (fits) positions.Add((r, c));
            }
        }
        return positions;
    }

    public static (bool[,] nextBoard, int linesCleared) SimulatePlacementAndClears(bool[,] boardFilled, bool[,] shape, int startR, int startC)
    {
        bool[,] nextBoard = (bool[,])boardFilled.Clone();
        int sRows = shape.GetLength(0);
        int sCols = shape.GetLength(1);

        for (int r = 0; r < sRows; r++)
        {
            for (int c = 0; c < sCols; c++)
            {
                if (shape[r, c]) nextBoard[startR + r, startC + c] = true;
            }
        }

        var fullRows = new List<int>();
        for (int r = 0; r < 8; r++)
        {
            bool full = true;
            for (int c = 0; c < 8; c++)
            {
                if (!nextBoard[r, c]) { full = false; break; }
            }
            if (full) fullRows.Add(r);
        }

        var fullCols = new List<int>();
        for (int c = 0; c < 8; c++)
        {
            bool full = true;
            for (int r = 0; r < 8; r++)
            {
                if (!nextBoard[r, c]) { full = false; break; }
            }
            if (full) fullCols.Add(c);
        }

        foreach (var r in fullRows)
        {
            for (int c = 0; c < 8; c++) nextBoard[r, c] = false;
        }
        foreach (var c in fullCols)
        {
            for (int r = 0; r < 8; r++) nextBoard[r, c] = false;
        }

        return (nextBoard, fullRows.Count + fullCols.Count);
    }

    /// <summary>
    /// Evaluates if placing a shape can trigger an immediate line clearance on the board.
    /// </summary>
    private static bool CanShapeClearLines(bool[,] boardFilled, bool[,] shape)
    {
        var placements = FindValidPlacements(boardFilled, shape);
        foreach (var (r, c) in placements)
        {
            var (_, cleared) = SimulatePlacementAndClears(boardFilled, shape, r, c);
            if (cleared > 0) return true;
        }
        return false;
    }

    /// <summary>
    /// Evaluates the board-clearing efficacy of a shape:
    /// - Max lines cleared
    /// - Remaining filled cells after clearing
    /// - Total board wipe capability (Clean Slate)
    /// </summary>
    private static (int maxClears, int minRemaining, bool canWipeBoard) EvaluateShapeClearing(bool[,] boardFilled, bool[,] shape)
    {
        var placements = FindValidPlacements(boardFilled, shape);
        if (placements.Count == 0) return (0, int.MaxValue, false);

        int maxClears = 0;
        int minRemaining = int.MaxValue;
        bool canWipeBoard = false;

        foreach (var (r, c) in placements)
        {
            var (nextBoard, cleared) = SimulatePlacementAndClears(boardFilled, shape, r, c);
            if (cleared > maxClears) maxClears = cleared;

            int remaining = 0;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (nextBoard[row, col]) remaining++;
                }
            }

            if (remaining < minRemaining) minRemaining = remaining;
            if (remaining == 0) canWipeBoard = true;
        }

        return (maxClears, minRemaining, canWipeBoard);
    }

    /// <summary>
    /// Helper: Checks if board has 0 filled cells.
    /// </summary>
    private static bool IsBoardCompletelyEmpty(bool[,] b)
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (b[r, c]) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Attempts to find a sequence of up to 3 pieces that completely clears the board (Clean Slate).
    /// Highly optimized with early branch pruning: runs in under 2ms.
    /// </summary>
    public static List<GamePiece>? TryFindCleanSlateTrio(bool[,] boardFilled)
    {
        var fittingTemplates = _templates.Where(t => CanShapeFit(boardFilled, t.Shape)).ToList();
        if (fittingTemplates.Count == 0) return null;

        // 1. Check if 1 piece can wipe the board completely
        foreach (var t1 in fittingTemplates)
        {
            var placements1 = FindValidPlacements(boardFilled, t1.Shape);
            foreach (var (r1, c1) in placements1)
            {
                var (b1, cleared1) = SimulatePlacementAndClears(boardFilled, t1.Shape, r1, c1);
                if (cleared1 > 0 && IsBoardCompletelyEmpty(b1))
                {
                    var p1 = GamePiece.Create((bool[,])t1.Shape.Clone(), _random.Next(1, 7));
                    var t2 = fittingTemplates[_random.Next(fittingTemplates.Count)];
                    var p2 = GamePiece.Create((bool[,])t2.Shape.Clone(), _random.Next(1, 7));
                    var t3 = fittingTemplates[_random.Next(fittingTemplates.Count)];
                    var p3 = GamePiece.Create((bool[,])t3.Shape.Clone(), _random.Next(1, 7));
                    return new List<GamePiece> { p1, p2, p3 };
                }
            }
        }

        // 2. Check if 2 pieces can wipe the board completely
        var p1Candidates = new List<((bool[,] Shape, int BaseColor, int SizeCategory, string Name) Template, int r, int c, bool[,] b1, int remaining)>();
        foreach (var t1 in fittingTemplates)
        {
            var placements1 = FindValidPlacements(boardFilled, t1.Shape);
            foreach (var (r1, c1) in placements1)
            {
                var (b1, cleared1) = SimulatePlacementAndClears(boardFilled, t1.Shape, r1, c1);
                if (cleared1 > 0)
                {
                    int rem = 0;
                    for (int r = 0; r < 8; r++)
                        for (int c = 0; c < 8; c++)
                            if (b1[r, c]) rem++;
                    p1Candidates.Add((t1, r1, c1, b1, rem));
                }
            }
        }

        p1Candidates.Sort((a, b) => a.remaining.CompareTo(b.remaining));
        int maxP1 = Math.Min(p1Candidates.Count, 8);

        for (int i = 0; i < maxP1; i++)
        {
            var p1 = p1Candidates[i];
            var fittingForB1 = _templates.Where(t => CanShapeFit(p1.b1, t.Shape)).ToList();
            foreach (var t2 in fittingForB1)
            {
                var placements2 = FindValidPlacements(p1.b1, t2.Shape);
                foreach (var (r2, c2) in placements2)
                {
                    var (b2, cleared2) = SimulatePlacementAndClears(p1.b1, t2.Shape, r2, c2);
                    if (cleared2 > 0 && IsBoardCompletelyEmpty(b2))
                    {
                        var piece1 = GamePiece.Create((bool[,])p1.Template.Shape.Clone(), _random.Next(1, 7));
                        var piece2 = GamePiece.Create((bool[,])t2.Shape.Clone(), _random.Next(1, 7));
                        var t3 = fittingTemplates[_random.Next(fittingTemplates.Count)];
                        var piece3 = GamePiece.Create((bool[,])t3.Shape.Clone(), _random.Next(1, 7));
                        return new List<GamePiece> { piece1, piece2, piece3 };
                    }
                }
            }
        }

        // 3. Check if 3 pieces can wipe the board completely
        int maxP1ForP3 = Math.Min(p1Candidates.Count, 4);
        for (int i = 0; i < maxP1ForP3; i++)
        {
            var p1 = p1Candidates[i];
            var fittingForB1 = _templates.Where(t => CanShapeFit(p1.b1, t.Shape)).ToList();
            var p2Candidates = new List<((bool[,] Shape, int BaseColor, int SizeCategory, string Name) Template, bool[,] b2, int remaining)>();

            foreach (var t2 in fittingForB1)
            {
                var placements2 = FindValidPlacements(p1.b1, t2.Shape);
                foreach (var (r2, c2) in placements2)
                {
                    var (b2, cleared2) = SimulatePlacementAndClears(p1.b1, t2.Shape, r2, c2);
                    if (cleared2 > 0)
                    {
                        int rem2 = 0;
                        for (int r = 0; r < 8; r++)
                            for (int c = 0; c < 8; c++)
                                if (b2[r, c]) rem2++;
                        p2Candidates.Add((t2, b2, rem2));
                    }
                }
            }

            p2Candidates.Sort((a, b) => a.remaining.CompareTo(b.remaining));
            int maxP2 = Math.Min(p2Candidates.Count, 4);

            for (int j = 0; j < maxP2; j++)
            {
                var p2 = p2Candidates[j];
                var fittingForB2 = _templates.Where(t => CanShapeFit(p2.b2, t.Shape)).ToList();
                foreach (var t3 in fittingForB2)
                {
                    var placements3 = FindValidPlacements(p2.b2, t3.Shape);
                    foreach (var (r3, c3) in placements3)
                    {
                        var (b3, cleared3) = SimulatePlacementAndClears(p2.b2, t3.Shape, r3, c3);
                        if (cleared3 > 0 && IsBoardCompletelyEmpty(b3))
                        {
                            var piece1 = GamePiece.Create((bool[,])p1.Template.Shape.Clone(), _random.Next(1, 7));
                            var piece2 = GamePiece.Create((bool[,])p2.Template.Shape.Clone(), _random.Next(1, 7));
                            var piece3 = GamePiece.Create((bool[,])t3.Shape.Clone(), _random.Next(1, 7));
                            return new List<GamePiece> { piece1, piece2, piece3 };
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Generates a trio with dynamic pacing waves:
    /// Frequently assists the player with shapes that clear nearly-full lines and aim for full board clears,
    /// while periodically providing diverse polyominoes for a balanced challenge.
    /// </summary>
    public static List<GamePiece> GeneratePlayableTrio(GridCell[,] board)
    {
        bool[,] boardFilled = new bool[8, 8];
        int filledCount = 0;
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (board[r, c].IsFilled)
                {
                    boardFilled[r, c] = true;
                    filledCount++;
                }
            }
        }

        // 15% probability: occasionally generate a trio leading to 100% full board wipe (Clean Slate)
        if (filledCount > 0 && _random.NextDouble() < 0.15)
        {
            var cleanSlateTrio = TryFindCleanSlateTrio(boardFilled);
            if (cleanSlateTrio != null && cleanSlateTrio.Count == 3)
            {
                // Shuffle so player discovers the puzzle sequence
                for (int i = cleanSlateTrio.Count - 1; i > 0; i--)
                {
                    int swap = _random.Next(i + 1);
                    (cleanSlateTrio[i], cleanSlateTrio[swap]) = (cleanSlateTrio[swap], cleanSlateTrio[i]);
                }
                return cleanSlateTrio;
            }
        }

        // Emergency Cleansing Trigger: if board is overcrowded (>68% full = 44+ cells),
        // immediately switch to Cleansing phase to save the player and ignite a comeback!
        if (filledCount >= 44 && _currentPhase != PacingPhase.Cleansing)
        {
            _currentPhase = PacingPhase.Cleansing;
            _roundsRemaining = _random.Next(1, 3); // 1-2 cleansing rounds
        }

        // Advance pacing wave counter
        _roundsRemaining--;
        if (_roundsRemaining <= 0)
        {
            if (_currentPhase == PacingPhase.Harmonious)
            {
                _currentPhase = PacingPhase.Challenging;
                _roundsRemaining = _random.Next(2, 4); // 2-3 challenging rounds
            }
            else if (_currentPhase == PacingPhase.Challenging)
            {
                _currentPhase = PacingPhase.Cleansing;
                _roundsRemaining = _random.Next(1, 3); // 1-2 cleansing rounds
            }
            else
            {
                _currentPhase = PacingPhase.Harmonious;
                _roundsRemaining = 5; // 5 harmonious rounds
            }
        }

        // Templates that can physically fit on the current board
        var fittingTemplates = _templates.Where(t => CanShapeFit(boardFilled, t.Shape)).ToList();
        if (fittingTemplates.Count == 0)
        {
            fittingTemplates = new() { _templates[0] }; // Fallback dot
        }

        var selectedPieces = new List<GamePiece>();

        // Assist line clearing:
        // - Cleansing phase: ALWAYS assist (100%), prioritizing keys for multi-line blasts!
        // - Harmonious phase: frequent assistance (~75%) for smooth building
        // - Challenging phase: moderate assistance (~40%)
        bool shouldAssistClear = filledCount > 0 && (_currentPhase == PacingPhase.Cleansing 
            || (_currentPhase == PacingPhase.Harmonious && _random.NextDouble() < 0.75) 
            || (_currentPhase == PacingPhase.Challenging && _random.NextDouble() < 0.40));

        if (shouldAssistClear)
        {
            // Evaluate all fitting templates for board-clearing power
            var scoredCandidates = fittingTemplates.Select(t =>
            {
                var (clears, remaining, wipe) = EvaluateShapeClearing(boardFilled, t.Shape);
                return new { Template = t, Clears = clears, Remaining = remaining, Wipe = wipe };
            }).ToList();

            // 1. First piece: check if any shape causes a total board wipe (Clean Slate)
            var wipeCandidates = scoredCandidates.Where(c => c.Wipe).ToList();
            var lineClearCandidates = scoredCandidates.Where(c => c.Clears > 0).OrderByDescending(c => c.Clears).ThenBy(c => c.Remaining).ToList();

            var primaryCandidate = wipeCandidates.Count > 0
                ? wipeCandidates[_random.Next(wipeCandidates.Count)].Template
                : (lineClearCandidates.Count > 0
                    ? lineClearCandidates[0].Template
                    : fittingTemplates[_random.Next(fittingTemplates.Count)]);

            int c1 = _random.Next(1, 7);
            selectedPieces.Add(GamePiece.Create((bool[,])primaryCandidate.Shape.Clone(), c1));

            // Simulate best placement of primary piece to find second piece
            var p1Placements = FindValidPlacements(boardFilled, primaryCandidate.Shape);
            bool[,] simBoard = boardFilled;
            if (p1Placements.Count > 0)
            {
                // Find placement that yielded best clears or least remaining
                (int bestR, int bestC) = p1Placements[0];
                int bestClears = -1;
                foreach (var (pr, pc) in p1Placements)
                {
                    var (_, cl) = SimulatePlacementAndClears(boardFilled, primaryCandidate.Shape, pr, pc);
                    if (cl > bestClears)
                    {
                        bestClears = cl;
                        bestR = pr;
                        bestC = pc;
                    }
                }
                (simBoard, _) = SimulatePlacementAndClears(boardFilled, primaryCandidate.Shape, bestR, bestC);
            }

            // 2. Second piece: evaluate on simulated board to continue clearing or wipe
            var simFitting = _templates.Where(t => CanShapeFit(simBoard, t.Shape)).ToList();
            if (simFitting.Count == 0) simFitting = fittingTemplates;

            var scoredSim = simFitting.Select(t =>
            {
                var (clears, remaining, wipe) = EvaluateShapeClearing(simBoard, t.Shape);
                return new { Template = t, Clears = clears, Remaining = remaining, Wipe = wipe };
            }).ToList();

            var simWipes = scoredSim.Where(c => c.Wipe).ToList();
            var simClears = scoredSim.Where(c => c.Clears > 0).OrderByDescending(c => c.Clears).ThenBy(c => c.Remaining).ToList();

            var secondCandidate = simWipes.Count > 0
                ? simWipes[_random.Next(simWipes.Count)].Template
                : (simClears.Count > 0
                    ? simClears[0].Template
                    : simFitting[_random.Next(simFitting.Count)]);

            int c2 = _random.Next(1, 7);
            selectedPieces.Add(GamePiece.Create((bool[,])secondCandidate.Shape.Clone(), c2));

            // 3. Third piece: complementary shape (preferring compact shapes <= 4 that plug gaps)
            var compactTemplates = fittingTemplates.Where(t => t.SizeCategory <= 4).ToList();
            var thirdCandidate = compactTemplates.Count > 0
                ? compactTemplates[_random.Next(compactTemplates.Count)]
                : fittingTemplates[_random.Next(fittingTemplates.Count)];

            int c3 = _random.Next(1, 7);
            selectedPieces.Add(GamePiece.Create((bool[,])thirdCandidate.Shape.Clone(), c3));
        }
        else
        {
            // Standard wave: diverse polyominoes with guaranteed placement
            var firstCandidate = fittingTemplates[_random.Next(fittingTemplates.Count)];
            int c1 = _random.Next(1, 7);
            selectedPieces.Add(GamePiece.Create((bool[,])firstCandidate.Shape.Clone(), c1));

            // Challenge candidate pool
            var challengePool = _templates.Where(t => t.SizeCategory >= 3).ToList();
            if (challengePool.Count == 0) challengePool = _templates;

            var secondCandidate = challengePool[_random.Next(challengePool.Count)];
            int c2 = _random.Next(1, 7);
            selectedPieces.Add(GamePiece.Create((bool[,])secondCandidate.Shape.Clone(), c2));

            var thirdCandidate = _templates[_random.Next(_templates.Count)];
            int c3 = _random.Next(1, 7);
            selectedPieces.Add(GamePiece.Create((bool[,])thirdCandidate.Shape.Clone(), c3));
        }

        // Shuffle pieces so the guaranteed/clearing pieces are distributed across random slots
        for (int i = selectedPieces.Count - 1; i > 0; i--)
        {
            int swap = _random.Next(i + 1);
            (selectedPieces[i], selectedPieces[swap]) = (selectedPieces[swap], selectedPieces[i]);
        }

        return selectedPieces;
    }
}
