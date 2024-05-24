using Newtonsoft.Json;

namespace DreamBot.Models.Automatic
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AlwaysonScripts
    {
        [JsonProperty("Comments")]
        public Comments Comments { get; set; } = new Comments();

        [JsonProperty("ControlNet")]
        public ControlNet ControlNet { get; set; } = new ControlNet();

        [JsonProperty("DynamicThresholding (CFG-Fix) Integrated")]
        public DynamicThresholdingCFGFixIntegrated DynamicThresholdingCFGFixIntegrated { get; set; } = new DynamicThresholdingCFGFixIntegrated();

        [JsonProperty("Extra options")]
        public ExtraOptions ExtraOptions { get; set; } = new ExtraOptions();

        [JsonProperty("FreeU Integrated")]
        public FreeUIntegrated FreeUIntegrated { get; set; } = new FreeUIntegrated();

        [JsonProperty("HyperTile Integrated")]
        public HyperTileIntegrated HyperTileIntegrated { get; set; } = new HyperTileIntegrated();

        [JsonProperty("Kohya HRFix Integrated")]
        public KohyaHRFixIntegrated KohyaHRFixIntegrated { get; set; } = new KohyaHRFixIntegrated();

        [JsonProperty("LatentModifier Integrated")]
        public LatentModifierIntegrated LatentModifierIntegrated { get; set; } = new LatentModifierIntegrated();

        [JsonProperty("MultiDiffusion Integrated")]
        public MultiDiffusionIntegrated MultiDiffusionIntegrated { get; set; } = new MultiDiffusionIntegrated();

        [JsonProperty("Never OOM Integrated")]
        public NeverOOMIntegrated NeverOOMIntegrated { get; set; } = new NeverOOMIntegrated();

        [JsonProperty("Refiner")]
        public Refiner Refiner { get; set; } = new Refiner();

        [JsonProperty("Seed")]
        public Seed Seed { get; set; } = new Seed();

        [JsonProperty("SelfAttentionGuidance Integrated")]
        public SelfAttentionGuidanceIntegrated SelfAttentionGuidanceIntegrated { get; set; } = new SelfAttentionGuidanceIntegrated();

        [JsonProperty("StyleAlign Integrated")]
        public StyleAlignIntegrated StyleAlignIntegrated { get; set; } = new StyleAlignIntegrated();
    }

    public class APIPayload
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } = [];
    }

    public class Comments
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } = [];
    }

    public class Comments2
    {
    }

    public class ControlNet
    {
        [JsonProperty("args")]
        public List<ControlNetArg> Args { get; set; } = [new ControlNetArg(), new ControlNetArg(), new ControlNetArg()];
    }

    public class ControlNetArg
    {
        [JsonProperty("batch_image_dir")]
        public string BatchImageDir { get; set; } = string.Empty;

        [JsonProperty("batch_input_gallery")]
        public List<object> BatchInputGallery { get; set; } = [];

        [JsonProperty("batch_mask_dir")]
        public string BatchMaskDir { get; set; } = string.Empty;

        [JsonProperty("batch_mask_gallery")]
        public List<object> BatchMaskGallery { get; set; } = [];

        [JsonProperty("control_mode")]
        public string ControlMode { get; set; } = "Balanced";

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("generated_image")]
        public object? GeneratedImage { get; set; }

        [JsonProperty("guidance_end")]
        public int GuidanceEnd { get; set; } = 1;

        [JsonProperty("guidance_start")]
        public int GuidanceStart { get; set; }

        [JsonProperty("hr_option")]
        public string HrOption { get; set; } = "Both";

        [JsonProperty("image")]
        public object? Image { get; set; }

        [JsonProperty("input_mode")]
        public string InputMode { get; set; } = "simple";

        [JsonProperty("mask_image")]
        public object? MaskImage { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; } = "None";

        [JsonProperty("module")]
        public string? Module { get; set; } = "None";

        [JsonProperty("pixel_perfect")]
        public bool PixelPerfect { get; set; }

        [JsonProperty("processor_res")]
        public int ProcessorRes { get; set; } = -1;

        [JsonProperty("resize_mode")]
        public string ResizeMode { get; set; } = "Crop and Resize";

        [JsonProperty("save_detected_map")]
        public bool SaveDetectedMap { get; set; } = true;

        [JsonProperty("threshold_a")]
        public int ThresholdA { get; set; } = -1;

        [JsonProperty("threshold_b")]
        public int ThresholdB { get; set; } = -1;

        [JsonProperty("use_preview_as_input")]
        public bool UsePreviewAsInput { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; } = 1;
    }

    public class DynamicThresholdingCFGFixIntegrated
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } =
        [
            false,
            7,
            1,
            "Constant",
            0,
            "Constant",
            0,
            1,
            "enable",
            "MEAN",
            "AD",
            1
        ];
    }

    public class ExtraOptions
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } = [];
    }

    public class FreeUIntegrated
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } = [
            false,
            1.01m,
            1.02m,
            0.99m,
            0.95m
        ];
    }

    public class HyperTileIntegrated
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } = [
            false,
            256,
            2,
            0,
            false
        ];
    }

    public class KohyaHRFixIntegrated
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } =
        [
            false,
            3,
            2,
            0,
            0.35,
            true,
            "bicubic",
            "bicubic"
        ];
    }

    public class LatentModifierIntegrated
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } =
        [
            false,
            0,
            "anisotropic",
            0,
            "reinhard",
            100,
            0,
            "subtract",
            0,
            0,
            "gaussian",
            "add",
            0,
            100,
            127,
            0,
            "hard_clamp",
            5,
            0,
            "None",
            "None"
        ];
    }

    public class MultiDiffusionIntegrated
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } =
        [
            false,
            "MultiDiffusion",
            768,
            768,
            64,
            4
        ];
    }

    public class NeverOOMIntegrated
    {
        [JsonProperty("args")]
        public List<bool> Args { get; set; } =
        [
            true,
            true
        ];
    }

    public class OverrideSettings
    {
    }

    public class Refiner
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } =
        [
            false,
            "",
            0.8m
        ];
    }

    public class Seed
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } =
        [
            -1,
            false,
            -1,
            0,
            0,
            0
        ];
    }

    public class SelfAttentionGuidanceIntegrated
    {
        [JsonProperty("args")]
        public List<object> Args { get; set; } =
        [
            false,
            0.5,
            2
        ];
    }

    public class StyleAlignIntegrated
    {
        [JsonProperty("args")]
        public List<bool> Args { get; set; } = [false];
    }

    public class TextToImageRequest
    {
        [JsonProperty("alwayson_scripts")]
        public AlwaysonScripts AlwaysonScripts { get; set; } = new AlwaysonScripts();

        [JsonProperty("batch_size")]
        public int BatchSize { get; set; } = 1;

        [JsonProperty("cfg_scale")]
        public decimal CfgScale { get; set; } = 7;

        [JsonProperty("comments")]
        public object Comments { get; set; } = new object();

        [JsonProperty("denoising_strength")]
        public decimal DenoisingStrength { get; set; } = 0.75m;

        [JsonProperty("disable_extra_networks")]
        public bool DisableExtraNetworks { get; set; }

        [JsonProperty("do_not_save_grid")]
        public bool DoNotSaveGrid { get; set; }

        [JsonProperty("do_not_save_samples")]
        public bool DoNotSaveSamples { get; set; }

        [JsonProperty("enable_hr")]
        public bool EnableHr { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; } = 512;

        [JsonProperty("hr_negative_prompt")]
        public string HrNegativePrompt { get; set; } = string.Empty;

        [JsonProperty("hr_prompt")]
        public string HrPrompt { get; set; } = string.Empty;

        [JsonProperty("hr_resize_x")]
        public int HrResizeX { get; set; }

        [JsonProperty("hr_resize_y")]
        public int HrResizeY { get; set; }

        [JsonProperty("hr_scale")]
        public int HrScale { get; set; } = 2;

        [JsonProperty("hr_second_pass_steps")]
        public int HrSecondPassSteps { get; set; }

        [JsonProperty("hr_upscaler")]
        public string HrUpscaler { get; set; } = "Latent";

        [JsonProperty("negative_prompt")]
        public string NegativePrompt { get; set; } = string.Empty;

        [JsonProperty("n_iter")]
        public int NIter { get; set; } = 1;

        [JsonProperty("override_settings")]
        public OverrideSettings OverrideSettings { get; set; } = new OverrideSettings();

        [JsonProperty("override_settings_restore_afterwards")]
        public bool OverrideSettingsRestoreAfterwards { get; set; } = true;

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("restore_faces")]
        public bool RestoreFaces { get; set; }

        [JsonProperty("sampler_name")]
        public string SamplerName { get; set; } = "Euler a";

        [JsonProperty("s_churn")]
        public int SChurn { get; set; }

        [JsonProperty("script_args")]
        public List<object> ScriptArgs { get; set; } = [];

        [JsonProperty("script_name")]
        public object? ScriptName { get; set; }

        [JsonProperty("seed")]
        public long Seed { get; set; } = -1;

        [JsonProperty("seed_enable_extras")]
        public bool SeedEnableExtras { get; set; } = true;

        [JsonProperty("seed_resize_from_h")]
        public int SeedResizeFromH { get; set; } = -1;

        [JsonProperty("seed_resize_from_w")]
        public int SeedResizeFromW { get; set; } = -1;

        [JsonProperty("s_min_uncond")]
        public int SMinUncond { get; set; }

        [JsonProperty("s_noise")]
        public int SNoise { get; set; } = 1;

        [JsonProperty("steps")]
        public int Steps { get; set; } = 30;

        [JsonProperty("s_tmax")]
        public int? STmax { get; set; }

        [JsonProperty("s_tmin")]
        public int STmin { get; set; }

        [JsonProperty("styles")]
        public List<object> Styles { get; set; } = [];

        [JsonProperty("subseed")]
        public long Subseed { get; set; } = -1;

        [JsonProperty("subseed_strength")]
        public int SubseedStrength { get; set; }

        [JsonProperty("tiling")]
        public bool Tiling { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; } = 512;
    }
}