using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minesweeper.Models;
using Minesweeper.OtherModels;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace Minesweeper.Controllers
{
	[ApiController]
	public class MinesweeperController : ControllerBase
	{
		SapContext dbContext = new SapContext();
		Random rand = new Random();
		string[,] field;
		GameInfoResponse needGame = new GameInfoResponse();
		ErrorResponse error = new ErrorResponse();

		[HttpPost]
		[Route("new")]
		public IActionResult PostNew([FromBody] NewGameRequest parameters)
		{
			GameInfoResponse gameInfo = new GameInfoResponse();
			if (parameters != null)
			{
				if (parameters.Height >= 2 && parameters.Height <= 30 && parameters.Width >= 2 && parameters.Width <= 30)
				{
					if (parameters.MinesCount <= (parameters.Height * parameters.Width - 1))
					{
						gameInfo.GameId = Guid.NewGuid();
						gameInfo.Width = parameters.Width;
						gameInfo.Height = parameters.Height;
						gameInfo.MinesCount = parameters.MinesCount;
						gameInfo.Completed = false; //игра не завершена
						gameInfo.Field = new string[gameInfo.Height, gameInfo.Width];
						string[,] fieldUser = new string[gameInfo.Height, gameInfo.Width];
						for (int i = 0; i < gameInfo.Field.GetLength(0); i++)
						{
							for (int j = 0; j < gameInfo.Field.GetLength(1); j++)
							{
								gameInfo.Field[i, j] = " ";
							}
						}
						int numCellsToFill = parameters.MinesCount;
						while (numCellsToFill > 0)
						{
							int randomRow = rand.Next(fieldUser.GetLength(0));
							int randomCol = rand.Next(fieldUser.GetLength(1));
							if (fieldUser[randomRow, randomCol] != "M")
							{
								fieldUser[randomRow, randomCol] = "M";
								numCellsToFill--;
							}
						}
						for (int i = 0; i < fieldUser.GetLength(0); i++)
						{
							for (int j = 0; j < fieldUser.GetLength(1); j++)
							{
								int countM = 0;
								if (fieldUser[i, j] != "M")
								{
									//подсчет соседей
									for (int deltaI = -1; deltaI <= 1; deltaI++)
									{
										for (int deltaJ = -1; deltaJ <= 1; deltaJ++)
										{
											int shiftI = i + deltaI;
											int shiftJ = j + deltaJ;
											if (shiftI >= 0 && shiftI < fieldUser.GetLength(0) && shiftJ >= 0 && shiftJ < fieldUser.GetLength(1))
											{
												countM += fieldUser[shiftI, shiftJ] == "M" ? 1 : 0;
											}
										}
									}
									fieldUser[i, j] = $"{countM}";
								}
							}
						}
						//сохранение
						FieldForUser fieldForUser = new FieldForUser();
						fieldForUser.GameId = gameInfo.GameId;
						fieldForUser.Field = fieldUser;
						dbContext.GameInfoResponses.Add(gameInfo);
						dbContext.FieldForUsers.Add(fieldForUser);
						dbContext.SaveChanges();
						return Ok(Serialize(gameInfo));
					}
					else
					{
						error.Error = $"Количество мин должно быть меньше {parameters.Height * parameters.Width - 1}";
						return BadRequest(error);
					}
				}
				else
				{
					error.Error = "Ширина и высота должны быть в диапазоне от 2 до 30";
					return BadRequest(error);
				}
			}
			else
			{
				error.Error = "Произошла непредвиденная ошибка";
				return BadRequest(error);
			}
		}

		[HttpPost]
		[Route("turn")]
		public IActionResult PostTurn([FromBody] GameTurnRequest parameters)
		{
			List<GameInfoResponse> games = dbContext.GameInfoResponses.AsNoTracking().ToList();
			List<FieldForUser> fieldForUser = dbContext.FieldForUsers.AsNoTracking().ToList();
			needGame = games.FirstOrDefault(it => it.GameId == parameters.GameId);
			field = fieldForUser.FirstOrDefault(it => it.GameId == parameters.GameId).Field;
			if (needGame != null)
			{
				int row = parameters.Row;
				int col = parameters.Col;
				if (needGame.Field[row, col] == " ")
				{
					// Игра закончена -> пользователь указал на мину
					if (field[row, col] == "M")
					{
						for (int i = 0; i < field.GetLength(0); i++)
						{
							for (int j = 0; j < field.GetLength(1); j++)
							{
								if (field[i, j] == "M")
								{
									field[i, j] = "X";
								}
							}
						}
						needGame.Field = field;
						needGame.Completed = true;
						//сохранение изменений
						SaveChangesDb(needGame);
						return Ok(Serialize(needGame));
					}
					else
					{
						if (field[row, col] == "0")
						{
							for (int i = 0; i < field.GetLength(0); i++)
							{
								for (int j = 0; j < field.GetLength(1); j++)
								{
									if (field[i, j] == "0")
									{
										//открываем соседние ячейки с 0
										for (int deltaI = -1; deltaI <= 1; deltaI++)
										{
											for (int deltaJ = -1; deltaJ <= 1; deltaJ++)
											{
												int shiftI = i + deltaI;
												int shiftJ = j + deltaJ;
												if (shiftI >= 0 && shiftI < field.GetLength(0) && shiftJ >= 0 && shiftJ < field.GetLength(1))
												{
													needGame.Field[shiftI, shiftJ] = field[shiftI, shiftJ];
												}
											}
										}
									}
								}
							}
							// Игра закончена -> пользователь открыл все ячейки, не занятые минами
							needGame.Completed = CheckAllCellsNotOpened(needGame.Field);
							if (needGame.Completed)
							{
								needGame.Field = field;
							}
							SaveChangesDb(needGame);
							return Ok(Serialize(needGame));
						}
						else
						{
							needGame.Field[row, col] = field[row, col];
							needGame.Completed = CheckAllCellsNotOpened(needGame.Field);
							if (needGame.Completed)
							{
								needGame.Field = field;
							}
							SaveChangesDb(needGame);
							return Ok(Serialize(needGame));
						}
					}
				}
				else
				{
					error.Error = "Некорректный ход";
					return BadRequest(error);
				}
			}
			else
			{
				error.Error = "Произошла непредвиденная ошибка";
				return BadRequest(error);
			}
		}

		private string Serialize<T>(T objectSerialize)
		{
			var InfoJson = JsonConvert.SerializeObject(objectSerialize);
			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
			responseMessage.Content = new StringContent(InfoJson);
			responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return InfoJson;
		}

		private bool CheckAllCellsNotOpened(string[,] field)
		{
			int count = 0;
			foreach (var cell in field)
			{
				if (cell == " ")
				{
					count++;
				}
			}
			return needGame.MinesCount == count ? true : false;
		}

		private void SaveChangesDb(GameInfoResponse game)
		{
			dbContext.GameInfoResponses.Remove(game);
			dbContext.SaveChanges();
			dbContext.GameInfoResponses.Add(game);
			dbContext.SaveChanges();
		}

		private void OpenAdjacentCells(int row, int col)
		{
			if (row < 0 || col < 0 || row >= field.GetLength(0) || col >= field.GetLength(1) || needGame.Field[row, col] != " ")
			{
				return;
			}
			needGame.Field[row, col] = field[row, col];

			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					int newRow = row + i;
					int newCol = col + j;
					if (newRow >= 0 && newRow < field.GetLength(0) && newCol >= 0 && newCol < field.GetLength(1))
					{
						OpenAdjacentCells(newRow, newCol);
					}
				}
			}
		}

	}
}
