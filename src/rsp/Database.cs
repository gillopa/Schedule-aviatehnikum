using System.Data.SQLite;
using System.Threading.Tasks;
static class DataBase
{
    private static readonly string _dbPath = "tgbot.db";
    private static async Task<bool> UniqueIdExistsAsync(long uniqueId)
    {
        using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
        {
            connection.Open();
            using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Client WHERE unique_id = @uniqueId", connection))
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
        using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
        {
            connection.Open();

            using (var command = new SQLiteCommand("INSERT INTO Client (name, unique_id, mailing) VALUES (@name, @uniqueId, @mailing)", connection))
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
        using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
        {
            connection.Open();

            using (var command = new SQLiteCommand("UPDATE Client SET mailing = @newMailingStatus WHERE unique_id = @uniqueId", connection))
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
    public static async Task<List<long>> GetUsersWithMailingEnabledAsync()
    {
        var users = new List<long>();
        if (string.IsNullOrEmpty(_dbPath)) throw new ArgumentException("Database path cannot be null or empty.", nameof(_dbPath));
        using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
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