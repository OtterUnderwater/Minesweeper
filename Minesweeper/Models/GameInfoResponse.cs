using System;
using System.Collections.Generic;

namespace Minesweeper.Models;

public partial class GameInfoResponse
{
    public Guid GameId { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int MinesCount { get; set; }

    public bool Completed { get; set; }

	public string[,] Field { get; set; } = null!;
}
