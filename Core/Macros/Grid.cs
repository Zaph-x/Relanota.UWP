namespace Core.Macros
{
    public class Grid : ITextComponent
    {

        public int Width { get; }
        public int Height { get; }
        public int CellSize { get; }
        public string result { get; set; }

        public Grid(int width, int height, int cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize + 1;
        }

        public string WriteComponent()
        {
            for (int i = 0; i <= Height * CellSize; i++)
            {
                for (int j = 0; j <= Width * CellSize; j++)
                {
                    if (j % CellSize == 0 && i % CellSize == 0)
                    {
                        result += '+';
                    }
                    else if (j % CellSize != 0 && i % CellSize == 0)
                    {
                        result += '-';
                    }
                    else if (j % CellSize != 0 && i % CellSize != 0)
                    {
                        result += ' ';
                    }
                    else
                    {
                        result += '|';
                    }
                }
                result += '\n';
            }
            return result;
        }
    }
}