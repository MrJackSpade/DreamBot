using Newtonsoft.Json;

namespace DreamBot.Models.Automatic
{
    public class ExtraGenerationParams
    {
    }

    public class Txt2ImgResponseInfo
    {
        [JsonProperty("all_negative_prompts")]
        public List<string> AllNegativePrompts { get; set; } = [];

        [JsonProperty("all_prompts")]
        public List<string> AllPrompts { get; set; } = [];

        [JsonProperty("all_seeds")]
        public List<long> AllSeeds { get; set; } = [];

        [JsonProperty("all_subseeds")]
        public List<long> AllSubseeds { get; set; } = [];

        [JsonProperty("batch_size")]
        public int BatchSize { get; set; }

        [JsonProperty("cfg_scale")]
        public double CfgScale { get; set; }

        [JsonProperty("clip_skip")]
        public int ClipSkip { get; set; }

        [JsonProperty("denoising_strength")]
        public decimal DenoisingStrength { get; set; }

        [JsonProperty("extra_generation_params")]
        public ExtraGenerationParams ExtraGenerationParams { get; set; } = new ExtraGenerationParams();

        [JsonProperty("face_restoration_model")]
        public string? FaceRestorationModel { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("index_of_first_image")]
        public int IndexOfFirstImage { get; set; }

        [JsonProperty("infotexts")]
        public List<string> Infotexts { get; set; } = [];

        [JsonProperty("is_using_inpainting_conditioning")]
        public bool IsUsingInpaintingConditioning { get; set; }

        [JsonProperty("job_timestamp")]
        public string? JobTimestamp { get; set; }

        [JsonProperty("negative_prompt")]
        public string? NegativePrompt { get; set; }

        [JsonProperty("prompt")]
        public string? Prompt { get; set; }

        [JsonProperty("restore_faces")]
        public bool RestoreFaces { get; set; }

        [JsonProperty("sampler_name")]
        public string? SamplerName { get; set; }

        [JsonProperty("sd_model_hash")]
        public string? SdModelHash { get; set; }

        [JsonProperty("sd_model_name")]
        public string? SdModelName { get; set; }

        [JsonProperty("sd_vae_hash")]
        public string? SdVaeHash { get; set; }

        [JsonProperty("sd_vae_name")]
        public string? SdVaeName { get; set; }

        [JsonProperty("seed")]
        public long Seed { get; set; }

        [JsonProperty("seed_resize_from_h")]
        public int SeedResizeFromH { get; set; }

        [JsonProperty("seed_resize_from_w")]
        public int SeedResizeFromW { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; }

        [JsonProperty("styles")]
        public List<object> Styles { get; set; } = [];

        [JsonProperty("subseed")]
        public long Subseed { get; set; }

        [JsonProperty("subseed_strength")]
        public double SubseedStrength { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("width")]
        public int Width { get; set; }
    }
}