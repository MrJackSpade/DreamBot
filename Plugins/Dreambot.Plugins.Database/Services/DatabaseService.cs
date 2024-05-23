using DreamBot.Database.Models;
using DreamBot.Plugins.Database.Interfaces;

namespace DreamBot.Plugins.Database.Services
{
    public class DatabaseService
    {
        private readonly IGenerationDataRepository _generationDataRepository;

        private readonly IGenerationPropertyRepository _generationPropertyRepository;

        private readonly IGenerationRepository _generationRepository;

        public DatabaseService(IGenerationDataRepository generationDataRepository, IGenerationPropertyRepository generationPropertyRepository, IGenerationRepository generationRepository)
        {
            _generationDataRepository = generationDataRepository;
            _generationPropertyRepository = generationPropertyRepository;
            _generationRepository = generationRepository;
        }

        public void Insert(GenerationDto generationDto)
        {
            // Insert the Generation object
            var generation = new Generation
            {
                UserId = generationDto.UserId,
                ChannelId = generationDto.ChannelId,
                DateCreated = generationDto.DateCreatedUtc,
                MessageId = generationDto.MessageId
            };

            _generationRepository.Insert(generation);

            // Get the generated ID from the inserted Generation
            var generationId = generation.Id;

            // Insert the GenerationData objects
            var generationDataList = generationDto.GenerationData.Select(data => new GenerationData
            {
                GenerationId = generationId,
                Data = data.Data,
                FileGuid = data.FileGuid
            });

            _generationDataRepository.Insert(generationDataList);

            // Insert the GenerationProperty objects
            var generationProperties = generationDto.GenerationProperties.Select(prop => new GenerationProperty
            {
                GenerationId = generationId,
                Property = prop.Property,
                Value = prop.Value
            });

            _generationPropertyRepository.Insert(generationProperties);
        }
    }
}