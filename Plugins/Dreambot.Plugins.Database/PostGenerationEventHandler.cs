using Dreambot.Plugins.EventResults;
using DreamBot.Database.Models;
using DreamBot.Plugins.Database.Repositories;

using DreamBot.Plugins.Database.Services;
using DreamBot.Plugins.EventArgs;
using DreamBot.Plugins.Interfaces;
using DreamBot.Shared.Extensions;
using DreamBot.Shared.Models;

namespace DreamBot.Plugins.Database
{
    public class PostGenerationEventHandler : IPostGenerationEventHandler
    {
        private Configuration _configuration;

        private DatabaseService _databaseService;

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _configuration = args.LoadConfiguration<Configuration>();

            _databaseService = new DatabaseService(
                new GenerationDataRepository(_configuration.DatabaseConnectionString),
                new GenerationPropertyRepository(_configuration.DatabaseConnectionString),
                new GenerationRepository(_configuration.DatabaseConnectionString)
            );

            return InitializationResult.SuccessAsync();
        }

        public Task OnPostGeneration(PostGenerationEventArgs args)
        {
            GenerationDto generationDto = new()
            {
                ChannelId = args.Channel.Id,
                DateCreatedUtc = args.DateCreated,
                UserId = args.User.Id,
                MessageId = args.Message.Id,
            };

            if (args.Images.Length > 1)
            {
                throw new NotImplementedException();
            }

            foreach (GeneratedImage image in args.Images)
            {
                generationDto.GenerationData.Add(new GenerationData()
                {
                    Data = image.Data.FromBase64(),
                    FileGuid = image.FileName
                });
            }

            generationDto.AddProperty("Prompt", args.GenerationParameters.Prompt);
            generationDto.AddProperty("NegativePrompt", args.GenerationParameters.NegativePrompt);
            generationDto.AddProperty("Seed", args.GenerationParameters.Seed);
            generationDto.AddProperty("Width", args.GenerationParameters.Width);
            generationDto.AddProperty("Height", args.GenerationParameters.Height);
            generationDto.AddProperty("Steps", args.GenerationParameters.Steps);
            generationDto.AddProperty("SamplerName", args.GenerationParameters.SamplerName);

            _databaseService.Insert(generationDto);

            return Task.CompletedTask;
        }
    }
}