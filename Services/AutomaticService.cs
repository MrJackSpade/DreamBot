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

        private string InterruptUrl => $"http://{this._settings.Host}:{this._settings.Port}/sdapi/v1/interrupt";

        private string ProgressUrl => $"http://{this._settings.Host}:{this._settings.Port}/sdapi/v1/progress";

        private string Txt2ImgUrl => $"http://{this._settings.Host}:{this._settings.Port}/sdapi/v1/txt2img";

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

        public QueueTxt2ImgTaskResult Txt2Image(Txt2Img settings, CancellationToken cancellationToken)
        {
            settings.AlwaysonScripts.NeverOOMIntegrated.Args =
            [
                this._settings.AggressiveOptimizations,
                this._settings.AggressiveOptimizations
            ];

            Txt2ImgTask toReturn = new(settings, cancellationToken);

            this._taskQueue.Enqueue(toReturn);

            return new QueueTxt2ImgTaskResult(toReturn, this._taskQueue.Count, DateTime.Now);
        }

        private async Task Interrupt() => _ = await this._httpClient.PostAsync(this.InterruptUrl, new StringContent(""));

        private async Task Process()
        {
            do
            {
                if (this._taskQueue.TryDequeue(out Txt2ImgTask? task))
                {
                    try
                    {
                        await this.WaitForReady();

                        Task<Txt2ImgResponse> job = this._httpClient.PostJson<Txt2ImgResponse>(this.Txt2ImgUrl, task.Request, task.CancellationToken);

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
                                    Txt2ImgProgress progress = await this._httpClient.GetJson<Txt2ImgProgress>(this.ProgressUrl);

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
                Txt2ImgProgress progress = await this._httpClient.GetJson<Txt2ImgProgress>(this.ProgressUrl);

                if (progress.State.JobCount == 0)
                {
                    return;
                }
            } while (true);
        }
    }
}