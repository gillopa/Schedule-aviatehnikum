using System.Data.SQLite;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace Schedule.Repository;
public class ScheduleRepository
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly ILogger<ScheduleRepository> _logger;
    public ScheduleRepository(IConfiguration configuration, ILogger<ScheduleRepository> logger)
    {
        _logger = logger;
        _dbPath = configuration.GetValue<string>("DataBaseAddres") ?? "";
        _connectionString = $"Data Source={_dbPath};Version=3;";
    }
    private async Task<bool> UniqueIdExistsAsync(long uniqueId)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();
            using (var command =
                   new SQLiteCommand("SELECT COUNT(*) FROM Client WHERE unique_id = @uniqueId", connection))
            {
                command.Parameters.AddWithValue("@uniqueId", uniqueId);
                return (long?)await command.ExecuteScalarAsync() > 0;
            }
        }
    }

    public async Task<int> AddNewClientAsync(string name, long uniqueId)
    {
        int? responseCount = null;
        if (await UniqueIdExistsAsync(uniqueId))
            return 0;
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command =
                   new SQLiteCommand(
                       "INSERT INTO Client (name, unique_id, mailing) VALUES (@name, @uniqueId, @mailing)", connection))
            {
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@uniqueId", uniqueId);
                command.Parameters.AddWithValue("@mailing", 0);

                try
                {
                    responseCount = await command.ExecuteNonQueryAsync();
                }
                catch (SQLiteException ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        return responseCount ?? 0;
    }

    public async Task<int> UpdateMailingStatusAsync(long uniqueId, int newMailingStatus)
    {
        int? responseCount = null;
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command =
                   new SQLiteCommand("UPDATE Client SET mailing = @newMailingStatus WHERE unique_id = @uniqueId",
                       connection))
            {
                command.Parameters.AddWithValue("@newMailingStatus", newMailingStatus);
                command.Parameters.AddWithValue("@uniqueId", uniqueId);

                try
                {
                    responseCount = await command.ExecuteNonQueryAsync();
                }
                catch (SQLiteException ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        return responseCount ?? 0;
    }

    public async Task<bool> UpdateGroupStatusAsync(long uniqueId, string group)
    {
        int? responseCount = null;
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command = new SQLiteCommand("UPDATE Client SET `group` = @group WHERE unique_id = @uniqueId", connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@uniqueId", uniqueId);

                try
                {
                    responseCount = await command.ExecuteNonQueryAsync();
                }
                catch (SQLiteException ex)
                {
                    _logger.LogError(ex.Message);
                    return false;
                }
            }
        }

        return responseCount > 0;
    }
    public bool ScheduleExistsForDateAndGroup(string group, DateOnly date)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT COUNT(*) FROM Raspisanie WHERE `group` = @group AND date = @date";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@date", date.ToString("yyyy.MM.dd"));
                try
                {
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
                catch { return false; }
            }
        }
    }
    public bool UpdateScheduleUrl(string group, string id, DateOnly date)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();
            string query = "UPDATE Raspisanie SET id = @id WHERE `group` = @group AND date = @date";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@date", date.ToString("yyyy.MM.dd"));
                try
                {
                    return command.ExecuteNonQuery() > 0;
                }
                catch { return false; }
            }
        }
    }
    public bool AddNewSchedule(string group, string id, DateOnly date)
    {
        if (!ScheduleExistsForDateAndGroup(group, date))
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Raspisanie (`group`, id, date) VALUES (@group, @id, @date)";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@group", group);
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@date", date.ToString("yyyy.MM.dd"));
                    try
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                    catch (SQLiteException ex)
                    {
                        _logger.LogError($"Database exception: {ex.Message}");
                        return false;
                    }
                }
            }
        else
            UpdateScheduleUrl(group, id, date);
        return true;
    }
    public async Task<string> GetScheduleCalls()
    {
        using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query =
                "SELECT TOP 1 id FROM RaspisanieCalls";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                var response = await command.ExecuteScalarAsync();
                if ((string?)response == null)
                    return string.Empty;
                return (string)response;
            }
        }
    }
    public async Task<string> GetSchedileIdByDateAndGroup(string group, DateOnly date)
    {
        using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query =
                "SELECT id FROM Raspisanie WHERE `group` = @group AND `date` = @date";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@date", date.ToString("yyyy.MM.dd"));

                var response = await command.ExecuteScalarAsync();
                if ((string?)response == null)
                    return string.Empty;
                return (string)response;
            }
        }
    }
    public async Task<string> GetSchedileIdByDateAndGroup(string group, string date)
    {
        using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query =
                "SELECT id FROM Raspisanie WHERE `group` = @group AND `date` = @date";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@date", date);

                var response = await command.ExecuteScalarAsync();
                if ((string?)response == null)
                    return string.Empty;
                return (string)response;
            }
        }
    }
    public async Task<List<DateOnly>> GetDatesByDateAndGroup(string group, int daysFromToday)
    {
        List<DateOnly> dates = new();
        using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query =
                "SELECT date FROM Raspisanie WHERE `group` = @group AND date BETWEEN @startDate AND @endDate";
            using (var command = new SQLiteCommand(query, connection))
            {
                var today = DateTime.UtcNow.AddDays(1).Date;
                DateTime startDate = today.AddDays(-daysFromToday);
                DateTime endDate = today;

                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy.MM.dd"));
                command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy.MM.dd"));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string dateStr = reader.GetString(0);
                        if (DateOnly.TryParseExact(dateStr, "yyyy.MM.dd", CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateOnly dateOnly))
                            dates.Add(dateOnly);
                    }
                }
            }
        }

        return dates;
    }

    public async Task<string> GetGroup(long uniqueId)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command =
                   new SQLiteCommand("SELECT `group` FROM Client WHERE unique_id = @uniqueId", connection))
            {
                command.Parameters.AddWithValue("@uniqueId", uniqueId);
                var group = (string?)await command.ExecuteScalarAsync() ?? string.Empty;
                return group;
            }
        }
    }

    public async Task<List<long>> GetUsersWithMailingEnabledAsync(string group)
    {
        var users = new List<long>();
        if (string.IsNullOrEmpty(_dbPath))
            throw new ArgumentException("Database path cannot be null or empty.", nameof(_dbPath));
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command = new SQLiteCommand("SELECT unique_id FROM Client WHERE mailing = 1 AND `group` = @group", connection))
            {
                command.Parameters.AddWithValue("@group", group);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        long uniqueId = reader.GetInt64(0);
                        users.Add(uniqueId);
                    }
                }
            }
        }

        return users;
    }
}