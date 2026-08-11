using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connString = "Server=localhost;Database=MCFH_DB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True";
        using (var conn = new SqlConnection(connString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM ScrapedFeedbacks WHERE ProjectId = 1 AND SourceId IS NULL";
                int count = cmd.ExecuteNonQuery();
                Console.WriteLine($"Deleted {count} scraped feedbacks.");
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM ImportFiles WHERE ProjectId = 1";
                int count = cmd.ExecuteNonQuery();
                Console.WriteLine($"Deleted {count} import files.");
            }
        }
    }
}
