using DreamBot.Database.Models;
using DreamBot.Plugins.Database.Interfaces;
using Microsoft.Data.SqlClient;

namespace DreamBot.Plugins.Database.Repositories
{
    public class GenerationRepository(string connectionString) : IGenerationRepository
    {
        private readonly string _connectionString = connectionString;

        public void Insert(Generation generation)
        {
            if (generation.MessageId == 0)
            {
                throw new ArgumentException("MessageId can not be zero", nameof(generation.MessageId));
            }

            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var command = new SqlCommand("INSERT INTO Generations (UserId, ChannelId, MessageId, DateCreatedUtc) VALUES (@UserId, @ChannelId,@MessageId, @DateCreatedUtc); SELECT SCOPE_IDENTITY();", connection);

            command.Parameters.AddWithValue("@UserId", unchecked((long)generation.UserId));
            command.Parameters.AddWithValue("@ChannelId", unchecked((long)generation.ChannelId));
            command.Parameters.AddWithValue("@MessageId", unchecked((long)generation.MessageId));

            command.Parameters.AddWithValue("@DateCreatedUtc", generation.DateCreated);

            generation.Id = Convert.ToInt64(command.ExecuteScalar());
        }
    }
}