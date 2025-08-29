using ModConfigMenu;
using ModConfigMenu.Objects;
using System.Collections.Generic;

namespace MoreProjects.Mcm
{
    internal class McmConfiguration : McmConfigurationBase
    {

        public McmConfiguration(ModConfig config, Logger logger) : base (config, logger) { }

        public override void Configure()
        {
            ModConfigMenuAPI.RegisterModConfig("More Projects", new List<ConfigValue>()
            {
                CreateRestartMessage(),
                CreateConfigProperty(nameof(ModConfig.ProjectCountMultiplier),
                    @"The multiplier for the number of project increases per Magnum upgrade.
                    Example: If an upgrade gives +3 projects, 2 would changes this to 6 projects.",
                    1,100),
            }, OnSave);
        }
     
    }
}
