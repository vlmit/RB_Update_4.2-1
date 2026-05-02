using Newtonsoft.Json;

namespace Tessa.Extensions.Default.Console.PackageMobileClientApp
{
    public class MobileClientConfig
    {
        #region Properties

        [JsonProperty(PropertyName = "release")]
        public string? Release { get; set; }

        [JsonProperty(PropertyName = "platform_version")]
        public string? PlatformVersion { get; set; }

        [JsonProperty(PropertyName = "bundle_version")]
        public string? BundleVersion { get; set; }

        [JsonProperty(PropertyName = "min_support_native_version")]
        public string? MinSupportNativeVersion { get; set; }

        [JsonProperty(PropertyName = "bundle_strategy")]
        public string? BundleStrategy { get; set; }

        [JsonProperty(PropertyName = "public_key")]
        public string? PublicKey { get; set; }

        #endregion
    }
}
