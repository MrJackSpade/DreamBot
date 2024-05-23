using DreamBot.Database.Models;

namespace DreamBot.Plugins.Database.Interfaces
{
    public interface IGenerationRepository
    {
        void Insert(Generation generation);
    }
}