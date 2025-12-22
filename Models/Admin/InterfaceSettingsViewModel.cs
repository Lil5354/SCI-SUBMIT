namespace SciSubmit.Models.Admin
{
    public class InterfaceSettingsViewModel
    {
        public string? HeroTitle { get; set; }
        public string? HeroSubtitle { get; set; }
        public string? ConferenceDate { get; set; }
        public string? ConferenceLocation { get; set; }
        public string? HeroBackgroundColor { get; set; }
        public string? HeroBackgroundImage { get; set; }
        
        // Animation Settings
        public bool EnableAnimations { get; set; } = true;
        public string AnimationSpeed { get; set; } = "normal"; // slow, normal, fast
        public bool EnableParticles { get; set; } = true;
        public string ParticleDensity { get; set; } = "medium"; // low, medium, high
        public bool EnableLightStreaks { get; set; } = true;
        public string LightIntensity { get; set; } = "medium"; // low, medium, high
        
        // Gradient Color - Single color for overlay
        public string? GradientColor { get; set; } // Single gradient overlay color
        
        // Display Options
        public bool ShowStatistics { get; set; } = true;
        public bool ShowCountdown { get; set; } = true;
        public bool ShowProgressSteps { get; set; } = true;
        public bool ShowQuickActions { get; set; } = true;
        public bool ShowRecentSubmissions { get; set; } = true;
    }
}

