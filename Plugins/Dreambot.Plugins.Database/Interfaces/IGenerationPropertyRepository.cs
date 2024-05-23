using DreamBot.Database.Models;

namespace DreamBot.Plugins.Database.Interfaces
{
    public interface IGenerationPropertyRepository
    {
        void Insert(IEnumerable<GenerationProperty> generationProperties);
    }
}