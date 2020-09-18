using System.ComponentModel.DataAnnotations;

namespace Core.Macros
{
    public class Grid : TextComponent
    {
        public int Width { get; }
        public int Height { get; }
        public int CellHeight { get; }
        public int CellWidth { get; }

        public Grid(int width, int height, int cellWidth, int cellHeight)
        {
            Width = width;
            Height = height;
            CellHeight = cellHeight + 1;
            CellWidth = cellWidth + 1;
        }

        public override string WriteComponent()
        {
            for (int i = 0; i <= Height * CellHeight; i++)
            {
                for (int j = 0; j <= Width * CellWidth; j++)
                {
                    if (j % CellWidth == 0 && i % CellHeight == 0)
                    {
                        Result += '+';
                    }
                    else if (j % CellWidth != 0 && i % CellHeight == 0)
                    {
                        Result += '-';
                    }
                    else if (j % CellWidth != 0 && i % CellHeight != 0)
                    {
                        Result += ' ';
                    }
                    else
                    {
                        Result += '|';
                    }
                }
                Result += '\n';
            }
            return Result;
        }
    }
}