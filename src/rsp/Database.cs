using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

static class DataBase
{
    private static readonly string _dbPath = "tgbot.db";
    private static readonly string _connectionString = $"Data Source={_dbPath};Version=3;";

    private static async Task<bool> UniqueIdExistsAsync(long uniqueId)
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

    public static async Task<int> AddNewClientAsync(string name, long uniqueId)
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
                    Console.WriteLine(ex.Message);
                }
            }
        }

        return responseCount ?? 0;
    }

    public static async Task<int> UpdateMailingStatusAsync(long uniqueId, int newMailingStatus)
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
                    Console.WriteLine(ex.Message);
                }
            }
        }

        return responseCount ?? 0;
    }

    public static async Task<bool> UpdateGroupStatusAsync(long uniqueId, string group)
    {
        int? responseCount = null;
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command = new SQLiteCommand("UPDATE Client SET group = @group WHERE unique_id = @uniqueId",
                       connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@uniqueId", uniqueId);

                try
                {
                    responseCount = await command.ExecuteNonQueryAsync();
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }

        return responseCount > 0;
    }

    public static bool AddNewSchedule(string group, string link, DateOnly date)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            string query = "INSERT INTO Raspisanie (group, link, date) VALUES (@group, @link, @date)";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@link", link);
                command.Parameters.AddWithValue("@date", date.ToString("yyyy.MM.dd"));
                try
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine($"Database exception: {ex.Message}");
                    return false;
                }
            }
        }
    }
    public static async Task<long> GetScheduleCalls()
    {
        using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query =
                "SELECT TOP 1 link FROM RaspisanieCalls";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                var response = await command.ExecuteScalarAsync();
                if ((long?)response == null)
                    return 0;
                return (long)response;
            }
        }
    }
    public static async Task<long> GetSchedileIdByDateAndGroup(string group, DateOnly date)
    {
        using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query =
                "SELECT link FROM Raspisanie WHERE `group` = @group AND `date` = @date";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@group", group);
                command.Parameters.AddWithValue("@date", date.ToString("yyyy.MM.dd"));

                var response = await command.ExecuteScalarAsync();
                if ((long?)response == null)
                    return 0;
                return (long)response;
            }
        }
    }

    public static async Task<List<DateOnly>> GetDatesByDateAndGroup(string group, int daysFromToday)
    {
        List<DateOnly> dates = new();
        using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query =
                "SELECT date FROM Raspisanie WHERE `group` = @group AND date BETWEEN @startDate AND @endDate";
            using (var command = new SQLiteCommand(query, connection))
            {
                DateTime today = DateTime.UtcNow.AddHours(5).Date;
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

    public static async Task<string> GetGroup(long uniqueId)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command =
                   new SQLiteCommand("SELECT group FROM Client WHERE unique_id = @uniqueId", connection))
            {
                var group = (string?)command.ExecuteScalar() ?? string.Empty;
                return group;
            }
        }
    }

    public static async Task<List<long>> GetUsersWithMailingEnabledAsync()
    {
        var users = new List<long>();
        if (string.IsNullOrEmpty(_dbPath))
            throw new ArgumentException("Database path cannot be null or empty.", nameof(_dbPath));
        using (var connection = new SQLiteConnection(_connectionString))
        {
            connection.Open();

            using (var command = new SQLiteCommand("SELECT unique_id FROM Client WHERE mailing = 1", connection))
            {
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