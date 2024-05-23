using DreamBot.Database.Models;

namespace DreamBot.Plugins.Database.Interfaces
{
    public interface IGenerationDataRepository
    {
        void Insert(IEnumerable<GenerationData> generationDataList);
    }
}