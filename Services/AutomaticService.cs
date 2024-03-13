using DreamBot.Extensions;
using DreamBot.Models.Automatic;
using DreamBot.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace DreamBot.Services
{
    internal class AutomaticService
    {
        private readonly HttpClient _httpClient;

        private readonly Thread _processingThread;

        private readonly AutomaticServiceSettings _settings;

        private readonly ConcurrentQueue<Txt2ImgTask> _taskQueue = new();

        public AutomaticService(AutomaticServiceSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(30)
            };

            _processingThread = new Thread(async () => await this.Process());

            _processingThread.Start();
        }

        private string InterruptUrl => $"http://{_settings.Host}:{_settings.Port}/sdapi/v1/interrupt";

        private string ProgressUrl => $"http://{_settings.Host}:{_settings.Port}/sdapi/v1/progress";

        private string Txt2ImgUrl => $"http://{_settings.Host}:{_settings.Port}/sdapi/v1/txt2img";

        public QueueTxt2ImgTaskResult Txt2Image(Txt2Img settings, CancellationToken cancellationToken)
        {
            settings.AlwaysonScripts.NeverOOMIntegrated.Args =
            [
                _settings.AggressiveOptimizations,
                    _settings.AggressiveOptimizations
            ];

            Txt2ImgTask toReturn = new(settings, cancellationToken);

            _taskQueue.Enqueue(toReturn);

            return new QueueTxt2ImgTaskResult(toReturn, _taskQueue.Count, DateTime.Now);
        }

        private async Task Interrupt()
        {
            _ = await _httpClient.PostAsync(InterruptUrl, new StringContent(""));
        }

        private async Task Process()
        {
            do
            {
                if (_taskQueue.TryDequeue(out Txt2ImgTask? task))
                {
                    try
                    {
                        await this.WaitForReady();

                        Task<Txt2ImgResponse> job = _httpClient.PostJson<Txt2ImgResponse>(Txt2ImgUrl, task.Request, task.CancellationToken);

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
                                Txt2ImgResponse response = await job;

                                Txt2ImgProgress progress = new()
                                {
                                    Active = false,
                                    Completed = true,
                                    CurrentImage = response.Images[0],
                                    EtaRelative = 0,
                                    Progress = 1,
                                    Info = JsonConvert.DeserializeObject<Txt2ImgResponseInfo>(response.Info)
                                };

                                task.SetProgress(progress);

                                break;
                            }
                            else
                            {
                                try
                                {
                                    Txt2ImgProgress progress = await _httpClient.GetJson<Txt2ImgProgress>(ProgressUrl);

                                    task.SetProgress(progress);
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
                }

                await Task.Delay(100);
            } while (true);
        }

        private async Task WaitForReady()
        {
            do
            {
                Txt2ImgProgress progress = await _httpClient.GetJson<Txt2ImgProgress>(ProgressUrl);

                if (progress.State.JobCount == 0)
                {
                    return;
                }
            } while (true);
        }
    }
}