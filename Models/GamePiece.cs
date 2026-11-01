namespace BlockBlastLite.Models;

public class GamePiece
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int ColorIndex { get; set; } = 1; // 1: Emerald/Teal, 2: Indigo/Blue, 3: Purple/Violet, 4: Amber/Orange, 5: Rose/Pink, 6: Cyan/Sky
    public bool[,] Shape { get; set; } = new bool[1, 1] { { true } };
    public int Rows => Shape.GetLength(0);
    public int Cols => Shape.GetLength(1);
    public bool IsUsed { get; set; } = false;

    public int BlockCount
    {
        get
        {
            int count = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (Shape[r, c]) count++;
                }
            }
            return count;
        }
    }

    public static GamePiece Create(bool[,] shape, int colorIndex)
    {
        return new GamePiece
        {
            Shape = shape,
            ColorIndex = colorIndex,
            IsUsed = false
        };
    }
}
