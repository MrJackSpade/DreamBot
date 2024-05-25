# DreamBot - Stable Diffusion Discord Bot

## Overview
DreamBot is a Discord bot that integrates with Automatic1111-Forge's Stable Diffusion API to generate images based on user prompts. The bot is currently in alpha and users are encouraged to try it out and report any bugs or questions. DreamBot is written in C# and utilizes a plugin architecture, allowing for additional modules to be developed and installed.

## Setup
1. Download the zip file for Win64 machines. (Or build, if that doesn't exist yet)
2. Modify the example configuration found in `Examples\Configurations` and copy it into the `Configurations\DreamBot` directory.
3. Set the `$.token` property in the configuration to a valid Discord API token.
4. Run the application. It should work on any environment with .Net, including Linux.

> [!WARNING] 
> The bot requires Discord permissions to be set. It does not have its own internal permissions. If you do not restrict access to things like settings, they will be available to anyone that can use that command. The same applies to executing image generation.

## Configuration
All configurations are stored in the `Configurations` subdirectory of the project. Configurations for each plugin are stored in their own subdirectory, where the name of the subdirectory matches the name of the plugin. Main configurations are stored in `Configurations\DreamBot`.

- `$.endpoints.aggressive_optimizations`: Set this property to generate images with ultra-low memory requirements, allowing SDXL image generation on a machine with as low as 4GB of VRAM.
- `$.token`: Set this property to a valid Discord API token for the bot to function.

## Plugins
The following plugins are included with DreamBot:

1. **Database**: Allows for saving all generated images and metadata to an SQL database.
2. **Honeypot**: Sets up a honeypot command that will auto-ban anyone that uses it. Good for catching griefers before they do a lot of damage.
3. **Lolice** (Incomplete): Allows for reporting potentially rule-breaking content directly to admins.
4. **Notifications**: Allows sending notifications to a specified channel when images are generated using prompts that match specific regex expressions.
5. **Star**: Sticks a star emoji on the generated image, for use with starboards.

## Execution
1. If image generation is attempted in a channel that isn't set up, it will say "Channel does not have Default Style set". To allow image generation in the channel, set a default style with `/settings defaultstyle:`.
2. The bot will use whatever Stable Diffusion model is currently loaded in the UI on the server running the Automatic1111-Forge endpoint.
3. The bot supports LORA's either by using the standard syntax or by leveraging the "LORA" parameter.
4. The bot supports pre-configured image aspect ratios, allowing users to select from a defined list of approved image sizes.
5. The bot supports live previews of generated images.

## Development
The following interfaces are available for consumption through plugins:

- `ICommandProvider`: Allows adding additional commands to the bot.
- `IPreGenerationEventHandler`: Fires before an image is generated.
- `IPostGenerationEventHandler`: Fires after an image is generated.
- `IReactionHandler`: Fires when a reaction is added to a generation.

Features planned for future releases:

1. Routing across Automatic1111-Forge instances based on load.
2. Configurable number of generated images (currently capped to 1 per request).
3. Allowing thread creators to set image generation parameters.
4. Allowing the bot to change models as needed.
5. Support for Automatic-1111 non forge
6. Better documentation
