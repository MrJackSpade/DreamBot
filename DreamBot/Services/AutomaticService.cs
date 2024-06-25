using DreamBot.Extensions;
using DreamBot.Models.Automatic;
using DreamBot.Shared.Interfaces;
using DreamBot.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace DreamBot.Services
{
	internal class AutomaticService : IAutomaticService
	{
		private readonly HttpClient _httpClient;

		private readonly Thread _processingThread;

		private readonly AutomaticServiceSettings _settings;

		private readonly ConcurrentQueue<TextToImageTask> _taskQueue = new();

		public AutomaticService(AutomaticServiceSettings settings)
		{
			this._settings = settings;
			this._httpClient = new HttpClient()
			{
				Timeout = TimeSpan.FromMinutes(30)
			};

			this._processingThread = new Thread(async () => await this.Process());

			this._processingThread.Start();
		}

		private string GetLorasUrl => $"http://{this._settings.Host}:{this._settings.Port}/sdapi/v1/loras";

		private string InterruptUrl => $"http://{this._settings.Host}:{this._settings.Port}/sdapi/v1/interrupt";

		private string ProgressUrl => $"http://{this._settings.Host}:{this._settings.Port}/sdapi/v1/progress";

		private string Txt2ImgUrl => $"http://{this._settings.Host}:{this._settings.Port}/sdapi/v1/txt2img";

		public async Task<Lora[]> GetLoras()
		{
			return await _httpClient.GetJson<Lora[]>(GetLorasUrl);
		}

		public async Task<Lora[]> GetLorasAsync()
		{
			return await _httpClient.GetJson<Lora[]>(GetLorasUrl);
		}

		public TextToImageTaskQueueResult Txt2Image(TextToImageRequest settings, CancellationToken cancellationToken)
		{
			settings.AlwaysonScripts.NeverOOMIntegrated.Args =
			[
				this._settings.AggressiveOptimizations,
				this._settings.AggressiveOptimizations
			];

			TextToImageTask toReturn = new(settings, cancellationToken);

			this._taskQueue.Enqueue(toReturn);

			return new TextToImageTaskQueueResult(toReturn, this._taskQueue.Count, DateTime.Now);
		}

		private async Task Interrupt() => _ = await this._httpClient.PostAsync(this.InterruptUrl, new StringContent(""));

		private async Task Process()
		{
			do
			{
				if (this._taskQueue.TryDequeue(out TextToImageTask? task))
				{
					try
					{
						await this.WaitForReady();

						Task<TextToImageResponse> job = this._httpClient.PostJson<TextToImageResponse>(this.Txt2ImgUrl, task.Request, task.CancellationToken);

						do
						{
							await Task.Delay(1000);

							if (task.CancellationToken.IsCancellationRequested)
							{
								await this.Interrupt();
								break;
							}

							if (job.IsCompleted)
							{
								TextToImageResponse response = await job;

								TextToImageProgress progress = new()
								{
									Active = false,
									Completed = true,
									CurrentImage = response.Images[0],
									Images = response.Images,
									EtaRelative = 0,
									Progress = 1,
									Info = JsonConvert.DeserializeObject<TextToImageResponseInfo>(response.Info)
								};

								await task.SetProgress(progress);

								break;
							}
							else
							{
								try
								{
									TextToImageProgress progress = await this._httpClient.GetJson<TextToImageProgress>(this.ProgressUrl);

									await task.SetProgress(progress);
								}
								catch (Exception ex)
								{
									Console.WriteLine(ex.ToString());
								}
							}
						} while (true);
					}
					catch (TaskCanceledException)
					{
						Console.WriteLine("Task was cancelled");
					}
					catch (Exception ex)
					{
						await task.SetProgress(new TextToImageProgress()
						{
							Active = false,
							Completed = true,
							Exception = ex.Message
						});

						Console.WriteLine(ex);
					}
				}

				await Task.Delay(100);
			} while (true);
		}

		private async Task WaitForReady()
		{
			do
			{
				TextToImageProgress progress = await this._httpClient.GetJson<TextToImageProgress>(this.ProgressUrl);

				if (progress.State.JobCount == 0)
				{
					return;
				}
			} while (true);
		}
	}
}