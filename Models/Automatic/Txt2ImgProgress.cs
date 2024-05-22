using Newtonsoft.Json;

namespace DreamBot.Models.Automatic
{
    public class State
    {
        [JsonProperty("interrupted")]
        public bool Interrupted { get; set; }

        [JsonProperty("job")]
        public string Job { get; set; }

        [JsonProperty("job_count")]
        public int JobCount { get; set; }

        [JsonProperty("job_no")]
        public int JobNo { get; set; }

        [JsonProperty("job_timestamp")]
        public string JobTimestamp { get; set; }

        [JsonProperty("sampling_step")]
        public int SamplingStep { get; set; }

        [JsonProperty("sampling_steps")]
        public int SamplingSteps { get; set; }

        [JsonProperty("skipped")]
        public bool Skipped { get; set; }

        [JsonProperty("stopping_generation")]
        public bool StoppingGeneration { get; set; }
    }

    public class Txt2ImgProgress
    {
        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("Exception")]
        public string Exception { get; set; }

        [JsonProperty("completed")]
        public bool Completed { get; set; }

        [JsonProperty("current_image")]
        public string? CurrentImage { get; set; }

        [JsonProperty("images")]
        public string[] Images { get; set; } = [];

        [JsonProperty("eta_relative")]
        public double EtaRelative { get; set; }

        [JsonProperty("info")]
        public Txt2ImgResponseInfo? Info { get; set; } = null;

        [JsonProperty("progress")]
        public double Progress { get; set; }

        [JsonProperty("state")]
        public State State { get; set; } = new State();
    }
}