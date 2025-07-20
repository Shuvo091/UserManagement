namespace CohesionX.UserManagement.Modules.Users.Application.DTOs;

public class EloHistoryDto
{
	public int OldElo { get; set; }
	public int NewElo { get; set; }
	public string Reason { get; set; } = default!;
	public string ComparisonId { get; set; } = default!;
	public string JobId { get; set; } = default!;
	public string Outcome { get; set; } = default!;
	public string ComparisonType { get; set; } = default!;
	public int KFactorUsed { get; set; }
	public DateTime ChangedAt { get; set; }
}

public class EloHistoryResponse
{
	public Guid UserId { get; set; }
	public int CurrentElo { get; set; }
	public int PeakElo { get; set; }
	public int InitialElo { get; set; }
	public int GamesPlayed { get; set; }
	public List<EloEntryDto> EloHistory { get; set; } = new();
	public EloTrendsDto Trends { get; set; } = new();
}

public class EloEntryDto
{
	public DateTime Date { get; set; }
	public int OldElo { get; set; }
	public int NewElo { get; set; }
	public string Change => NewElo >= OldElo ? $"+{NewElo - OldElo}" : $"-{OldElo - NewElo}";
	public Guid Opponent { get; set; }
	public string Outcome { get; set; } = string.Empty; // "win", "loss", etc.
	public string JobId { get; set; } = default!;
}

public class EloTrendsDto
{
	public string Last7Days { get; set; } = "0";
	public string Last30Days { get; set; } = "0";
	public double WinRate { get; set; } // 0.67
	public double AverageOpponentElo { get; set; }
}