using System;
using System.Collections.Generic;

namespace Minesweeper.Models;

public partial class FieldForUser
{
    public Guid GameId { get; set; }
	public string[,] Field { get; set; } = null!;
}
