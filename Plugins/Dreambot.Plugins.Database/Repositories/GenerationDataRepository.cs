using DreamBot.Database.Models;
using DreamBot.Plugins.Database.Interfaces;
using Microsoft.Data.SqlClient;

namespace DreamBot.Plugins.Database.Repositories
{
    public class GenerationDataRepository(string connectionString) : IGenerationDataRepository
    {
        private readonly string _connectionString = connectionString;

        public void Insert(IEnumerable<GenerationData> generationDataList)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand("INSERT INTO GenerationData (GenerationId, Data, FileGuid) VALUES (@GenerationId, @Data, @FileGuid)", connection);
            foreach (var generationData in generationDataList)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@GenerationId", generationData.GenerationId);
                command.Parameters.AddWithValue("@Data", generationData.Data);
                command.Parameters.AddWithValue("@FileGuid", generationData.FileGuid);

                command.ExecuteNonQuery();
            }
        }
    }
}