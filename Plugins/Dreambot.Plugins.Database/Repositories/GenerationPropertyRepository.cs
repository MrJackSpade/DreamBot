using DreamBot.Database.Models;
using DreamBot.Plugins.Database.Interfaces;
using Microsoft.Data.SqlClient;

namespace DreamBot.Plugins.Database.Repositories
{
    public class GenerationPropertyRepository(string connectionString) : IGenerationPropertyRepository
    {
        private readonly string _connectionString = connectionString;

        public void Insert(IEnumerable<GenerationProperty> generationProperties)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var command = new SqlCommand("INSERT INTO GenerationProperties (GenerationId, Property, Value) VALUES (@GenerationId, @Property, @Value)", connection);

            foreach (var generationProperty in generationProperties)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@GenerationId", generationProperty.GenerationId);
                command.Parameters.AddWithValue("@Property", generationProperty.Property);
                command.Parameters.AddWithValue("@Value", generationProperty.Value);

                command.ExecuteNonQuery();
            }
        }
    }
}