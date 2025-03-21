/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Furnace Go Brrr", "VisEntities", "1.1.0")]
    [Description("Speeds up smelting in ovens.")]
    public class FurnaceGoBrrr : RustPlugin
    {
        #region Fields

        private static FurnaceGoBrrr _plugin;
        private static Configuration _config;
        private CustomSmelterManager _customSmelterManager = new CustomSmelterManager();

        private const int CAPPED_SMELTING_SPEED = 20;
        private static Dictionary<BaseOven, int> _vanillaOvenSmeltingSpeeds = new Dictionary<BaseOven, int>();

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Ovens")]
            public List<OvenConfig> Ovens { get; set; }
        }

        private class OvenConfig
        {
            [JsonProperty("Prefab Short Names")]
            public List<string> PrefabShortNames { get; set; }

            [JsonProperty("Default Profile")]
            public string DefaultProfile { get; set; }

            [JsonProperty("Smelting Profiles")]
            public Dictionary<string, SmeltingProfileConfig> SmeltingProfiles { get; set; }
        }

        private class SmeltingProfileConfig
        {
            [JsonProperty("Priority")]
            public int Priority { get; set; }

            [JsonProperty("Smelting Speed")]
            public int SmeltingSpeed { get; set; }

            [JsonProperty("Burnable")]
            public BurnableConfig Burnable { get; set; }

            [JsonProperty("Cookables")]
            public List<CookableConfig> Cookables { get; set; }

            [JsonIgnore]
            public string Permission { get; set; }
        }

        private class BurnableConfig
        {
            [JsonProperty("Fuel Consumption Rate")]
            public int FuelConsumptionRate { get; set; }

            [JsonProperty("Enable Byproduct Creation")]
            public bool EnableByproductCreation { get; set; }

            [JsonProperty("Byproduct Creation Rate Per Unit Fuel")]
            public int ByproductCreationRatePerUnitFuel { get; set; }

            [JsonProperty("Byproduct Creation Chance")]
            public int ByproductCreationChance { get; set; }
        }

        private class CookableConfig
        {
            [JsonProperty("Raw Item Short Name")]
            public string RawItemShortName { get; set; }

            [JsonProperty("Units Smelted Per Cooking Cycle")]
            public int UnitsSmeltedPerCookingCycle { get; set; }

            [JsonProperty("Amount Produced Per Unit Cooked")]
            public int AmountProducedPerUnitCooked { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            ValidateAndCapSmeltingSpeeds();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            if (string.Compare(_config.Version, "1.1.0") < 0)
            {
                foreach (OvenConfig ovenConfig in _config.Ovens)
                {
                    foreach (var kvp in ovenConfig.SmeltingProfiles)
                    {
                        SmeltingProfileConfig profile = kvp.Value;

                        if (profile.Priority == 0)
                        {
                            if (kvp.Key.Equals("vip", StringComparison.OrdinalIgnoreCase))
                                profile.Priority = 1;
                            else
                                profile.Priority = 0;
                        }
                    }
                }
            }

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                Ovens = new List<OvenConfig>
                {
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "furnace",
                            "legacy_furnace"
                        },
                        DefaultProfile = "default",
                        SmeltingProfiles = new Dictionary<string, SmeltingProfileConfig>
                        {
                            {
                                "default", new SmeltingProfileConfig
                                {
                                    Priority = 0,
                                    SmeltingSpeed = 3,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "sulfur.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "hq.metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            },
                            {
                                "vip", new SmeltingProfileConfig
                                {
                                    Priority = 1,
                                    SmeltingSpeed = 3,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "sulfur.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "hq.metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "electricfurnace.deployed",
                        },
                        DefaultProfile = "default",
                        SmeltingProfiles = new Dictionary<string, SmeltingProfileConfig>
                        {
                            {
                                "default", new SmeltingProfileConfig
                                {
                                    Priority = 0,
                                    SmeltingSpeed = 10,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 0,
                                        EnableByproductCreation = false,
                                        ByproductCreationRatePerUnitFuel = 0,
                                        ByproductCreationChance = 0
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "sulfur.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "hq.metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            },
                            {
                                "vip", new SmeltingProfileConfig
                                {
                                    Priority = 1,
                                    SmeltingSpeed = 10,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 0,
                                        EnableByproductCreation = false,
                                        ByproductCreationRatePerUnitFuel = 0,
                                        ByproductCreationChance = 0
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "sulfur.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "hq.metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "furnace.large"
                        },
                        DefaultProfile = "default",
                        SmeltingProfiles = new Dictionary<string, SmeltingProfileConfig>
                        {
                            {
                                "default", new SmeltingProfileConfig
                                {
                                    Priority = 0,
                                    SmeltingSpeed = 15,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "sulfur.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "hq.metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            },
                            {
                                "vip", new SmeltingProfileConfig
                                {
                                    Priority = 1,
                                    SmeltingSpeed = 15,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "sulfur.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "hq.metal.ore",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "refinery_small_deployed"
                        },
                        DefaultProfile = "default",
                        SmeltingProfiles = new Dictionary<string, SmeltingProfileConfig>
                        {
                            {
                                "default", new SmeltingProfileConfig
                                {
                                    Priority = 0,
                                    SmeltingSpeed = 3,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "crude.oil",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 3
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            },
                            {
                                "vip", new SmeltingProfileConfig
                                {
                                    Priority = 1,
                                    SmeltingSpeed = 3,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "crude.oil",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 3
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "campfire",
                            "skull_fire_pit",
                            "hobobarrel.deployed"
                        },
                        DefaultProfile = "default",
                        SmeltingProfiles = new Dictionary<string, SmeltingProfileConfig>
                        {
                            {
                                "default", new SmeltingProfileConfig
                                {
                                    Priority = 0,
                                    SmeltingSpeed = 2,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "bearmeat",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "chicken.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "deermeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "fish.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "horsemeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "humanmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "meat.boar",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "wolfmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            },
                            {
                                "vip", new SmeltingProfileConfig
                                {
                                    Priority = 1,
                                    SmeltingSpeed = 2,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "bearmeat",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "chicken.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "deermeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "fish.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "horsemeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "humanmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "meat.boar",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "wolfmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "bbq.deployed"
                        },
                        DefaultProfile = "default",
                        SmeltingProfiles = new Dictionary<string, SmeltingProfileConfig>
                        {
                            {
                                "default", new SmeltingProfileConfig
                                {
                                    Priority = 0,
                                    SmeltingSpeed = 8,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "bearmeat",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "chicken.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "deermeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "fish.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "horsemeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "humanmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "meat.boar",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "wolfmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            },
                            {
                                "vip", new SmeltingProfileConfig
                                {
                                    Priority = 1,
                                    SmeltingSpeed = 8,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = true,
                                        ByproductCreationRatePerUnitFuel = 1,
                                        ByproductCreationChance = 25
                                    },
                                    Cookables = new List<CookableConfig>
                                    {
                                        new CookableConfig
                                        {
                                            RawItemShortName = "bearmeat",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "chicken.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "deermeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "fish.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "horsemeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "humanmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "meat.boar",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "wolfmeat.raw",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 1
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.beans.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 15
                                        },
                                        new CookableConfig
                                        {
                                            RawItemShortName = "can.tuna.empty",
                                            UnitsSmeltedPerCookingCycle = 1,
                                            AmountProducedPerUnitCooked = 10
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "tunalight.deployed",
                            "lantern.deployed",
                            "chineselantern.deployed",
                            "chineselantern_white.deployed",
                            "jackolantern.happy",
                            "jackolantern.angry",
                        },
                        DefaultProfile = "default",
                        SmeltingProfiles = new Dictionary<string, SmeltingProfileConfig>
                        {
                            {
                                "default", new SmeltingProfileConfig
                                {
                                    Priority = 0,
                                    SmeltingSpeed = 1,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = false,
                                        ByproductCreationRatePerUnitFuel = 0,
                                        ByproductCreationChance = 0
                                    },
                                    Cookables = new List<CookableConfig>()
                                }
                            },
                            {
                                "vip", new SmeltingProfileConfig
                                {
                                    Priority = 1,
                                    SmeltingSpeed = 1,
                                    Burnable = new BurnableConfig
                                    {
                                        FuelConsumptionRate = 1,
                                        EnableByproductCreation = false,
                                        ByproductCreationRatePerUnitFuel = 0,
                                        ByproductCreationChance = 0
                                    },
                                    Cookables = new List<CookableConfig>()
                                }
                            }
                        }
                    }
                }
            };
        }

        private void ValidateAndCapSmeltingSpeeds()
        {
            foreach (OvenConfig ovenConfig in _config.Ovens)
            {
                foreach (var profile in ovenConfig.SmeltingProfiles)
                {
                    if (profile.Value.SmeltingSpeed > CAPPED_SMELTING_SPEED)
                    {
                        PrintWarning($"Smelting speed for '{string.Join(", ", ovenConfig.PrefabShortNames)}' in profile '{profile.Key}' exceeds the maximum allowed value of {CAPPED_SMELTING_SPEED}. " +
                                     $"To prevent performance issues, the speed has been capped to {CAPPED_SMELTING_SPEED}. " +
                                     $"Consider adjusting other parameters to achieve desired results.");

                        profile.Value.SmeltingSpeed = CAPPED_SMELTING_SPEED;
                    }
                }
            }
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            InitializeSmeltingProfiles();
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            CoroutineUtil.StopAllCoroutines();
            _customSmelterManager.Unload();
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            CoroutineUtil.StartCoroutine(Guid.NewGuid().ToString(), InitializeOvensOnStartupCoroutine());
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null)
                return;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            BaseOven oven = gameObject.ToBaseEntity() as BaseOven;
            if (oven == null)
                return;

            OvenConfig ovenConfig = GetOvenConfig(oven);
            if (ovenConfig == null)
                return;

            SmeltingProfileConfig smeltingProfile = GetSmeltingProfile(player, ovenConfig);
            if (smeltingProfile == null)
                return;

            CustomSmelterComponent customSmelter = _customSmelterManager.GetOrAddCustomSmelterToOven(oven, smeltingProfile);        
        }

        private object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            if (oven == null || player == null)
                return null;

            if (oven.needsBuildingPrivilegeToUse && !player.CanBuild())
                return null;

            if (!oven.IsOn())
                return null;

            CustomSmelterComponent customSmelter = _customSmelterManager.GetCustomSmelterForOven(oven);
            if (customSmelter == null)
                return null;

            customSmelter.StopCooking();
            return true;
        }

        private object OnOvenStart(BaseOven oven)
        {
            if (oven == null)
                return null;

            BasePlayer player = FindPlayerById(oven.OwnerID);
            if (player == null)
                return null;

            OvenConfig ovenConfig = GetOvenConfig(oven);
            if (ovenConfig == null)
                return null;

            SmeltingProfileConfig smeltingProfile = GetSmeltingProfile(player, ovenConfig);
            if (smeltingProfile == null)
                return null;
   
            CustomSmelterComponent customSmelter = _customSmelterManager.GetOrAddCustomSmelterToOven(oven, smeltingProfile);
            if (customSmelter == null)
                return null;
     
            customSmelter.StartCooking();
            return true;
        }

        #endregion Oxide Hooks

        #region Custom Smelter Component

        private class CustomSmelterComponent : FacepunchBehaviour
        {
            #region Fields

            private BaseOven _oven;
            private CustomSmelterManager _manager;
            private SmeltingProfileConfig _smeltingProfile;

            #endregion Fields

            #region Initialization and Quitting

            public static CustomSmelterComponent Install(BaseOven oven, CustomSmelterManager manager, SmeltingProfileConfig smeltingProfile)
            {
                CustomSmelterComponent customSmelter = oven.gameObject.AddComponent<CustomSmelterComponent>();
                customSmelter.Initialize(manager, smeltingProfile);
                return customSmelter;
            }

            public CustomSmelterComponent Initialize(CustomSmelterManager manager, SmeltingProfileConfig smeltingProfile)
            {
                _oven = GetComponent<BaseOven>();
                _manager = manager;
                _smeltingProfile = smeltingProfile;

                if (!_vanillaOvenSmeltingSpeeds.ContainsKey(_oven))
                    _vanillaOvenSmeltingSpeeds[_oven] = _oven.smeltSpeed;

                _oven.smeltSpeed = _smeltingProfile.SmeltingSpeed;
                return this;
            }

            public static CustomSmelterComponent GetComponent(GameObject gameObject)
            {
                return gameObject.GetComponent<CustomSmelterComponent>();
            }

            public void DestroySelf()
            {
                DestroyImmediate(this);
            }

            #endregion Initialization and Quitting

            #region Component Lifecycle

            private void OnDestroy()
            {
                StopCooking();
                if (_vanillaOvenSmeltingSpeeds.TryGetValue(_oven, out int originalSmeltingSpeed))
                {
                    _oven.smeltSpeed = originalSmeltingSpeed;
                    _vanillaOvenSmeltingSpeeds.Remove(_oven);
                }
                _manager.HandleOvenKilled(_oven);
            }

            #endregion Component Lifecycle

            #region Cooking Logic

            public void StartCooking()
            {
                if (_oven.FindBurnable() == null && !_oven.CanRunWithNoFuel)
                    return;

                _oven.inventory.temperature = _oven.cookingTemperature;
                _oven.UpdateAttachmentTemperature();
                _oven.InvokeRepeating(new Action(Cook), 0.5f, 0.5f);
                _oven.SetFlag(BaseEntity.Flags.On, true, false, true);
            }

            public void StopCooking()
            {
                _oven.UpdateAttachmentTemperature();
                if (_oven.inventory != null)
                {
                    _oven.inventory.temperature = 15f;
                    foreach (Item item in _oven.inventory.itemList)
                    {
                        if (item.HasFlag(global::Item.Flag.OnFire))
                        {
                            item.SetFlag(global::Item.Flag.OnFire, false);
                            item.MarkDirty();
                        }
                        else if (item.HasFlag(global::Item.Flag.Cooking))
                        {
                            item.SetFlag(global::Item.Flag.Cooking, false);
                            item.MarkDirty();
                        }
                    }
                }

                _oven.CancelInvoke(new Action(Cook));
                _oven.SetFlag(BaseEntity.Flags.On, false, false, true);
            }

            private void Cook()
            {
                Item burnable = _oven.FindBurnable();
                if (burnable == null && !_oven.CanRunWithNoFuel)
                {
                    StopCooking();
                    return;
                }

                foreach (Item ovenItem in _oven.inventory.itemList)
                {
                    if (ovenItem.position >= _oven._inputSlotIndex && ovenItem.position < _oven._inputSlotIndex + _oven.inputSlots && !ovenItem.HasFlag(global::Item.Flag.Cooking))
                    {
                        ovenItem.SetFlag(global::Item.Flag.Cooking, true);
                        ovenItem.MarkDirty();
                    }
                }

                IncreaseCookTime(0.5f * _smeltingProfile.SmeltingSpeed);
           
                BaseEntity slot = _oven.GetSlot(BaseEntity.Slot.FireMod);
                if (slot)
                {
                    slot.SendMessage("Cook", 0.5f, SendMessageOptions.DontRequireReceiver);
                }

                if (burnable != null)
                {
                    ItemModBurnable itemModBurnable = burnable.info.ItemModBurnable;
                    burnable.fuel -= 0.5f * (_oven.cookingTemperature / 200f);
                    if (!burnable.HasFlag(global::Item.Flag.OnFire))
                    {
                        burnable.SetFlag(global::Item.Flag.OnFire, true);
                        burnable.MarkDirty();
                    }
                    if (burnable.fuel <= 0f)
                    {
                        ConsumeFuel(burnable, itemModBurnable);
                    }
                }

                Interface.CallHook("OnOvenCooked", _oven, burnable, slot);
            }

            private void ConsumeFuel(Item fuel, ItemModBurnable burnable)
            {
                if (Interface.CallHook("OnFuelConsume", _oven, fuel, burnable) != null)
                    return;

                if (_oven.allowByproductCreation && _smeltingProfile.Burnable.EnableByproductCreation
                    && burnable.byproductItem != null && ChanceSucceeded(_smeltingProfile.Burnable.ByproductCreationChance))
                {
                    Item item = ItemManager.Create(burnable.byproductItem, burnable.byproductAmount * GetCharcoalRate(), 0UL);
                    if (!item.MoveToContainer(_oven.inventory, -1, true, false, null, true))
                    {
                        OvenFull();
                        item.Drop(_oven.inventory.dropPosition, _oven.inventory.dropVelocity, default(Quaternion));
                    }
                }
                if (fuel.amount <= GetFuelRate())
                {
                    fuel.Remove(0f);
                    return;
                }
                int fuelRate = GetFuelRate();
                fuel.UseItem(fuelRate);
                fuel.fuel = burnable.fuelAmount;
                fuel.MarkDirty();
                Interface.CallHook("OnFuelConsumed", _oven, fuel, burnable);
            }

            private int GetFuelRate()
            {
                return _smeltingProfile.Burnable.FuelConsumptionRate;
            }

            private int GetCharcoalRate()
            {
                return _smeltingProfile.Burnable.ByproductCreationRatePerUnitFuel;
            }

            private void IncreaseCookTime(float smeltingSpeed)
            {
                List<Item> itemsCurrentlyBeingCooked = Facepunch.Pool.Get<List<Item>>();
                foreach (Item item in _oven.inventory.itemList)
                {
                    if (item.HasFlag(global::Item.Flag.Cooking))
                    {
                        itemsCurrentlyBeingCooked.Add(item);
                    }
                }

                float cookingTimePerItem = smeltingSpeed / (float)itemsCurrentlyBeingCooked.Count;
                foreach (Item itemBeingCooked in itemsCurrentlyBeingCooked)
                {
                    CycleCooking(itemBeingCooked, cookingTimePerItem);
                }

                Facepunch.Pool.FreeUnmanaged(ref itemsCurrentlyBeingCooked);
            }
            
            private void CycleCooking(Item item, float cookingTime)
            {
                ItemModCookable itemModCookable = item.info.ItemModCookable;

                if (!itemModCookable.CanBeCookedByAtTemperature(item.temperature) || item.cookTimeLeft < 0f)
                {
                    if (itemModCookable.setCookingFlag && item.HasFlag(global::Item.Flag.Cooking))
                    {
                        item.SetFlag(global::Item.Flag.Cooking, false);
                        item.MarkDirty();
                    }
                    return;
                }

                if (itemModCookable.setCookingFlag && !item.HasFlag(global::Item.Flag.Cooking))
                {
                    item.SetFlag(global::Item.Flag.Cooking, true);
                    item.MarkDirty();
                }

                item.cookTimeLeft -= cookingTime;
                if (item.cookTimeLeft > 0f)
                {
                    item.MarkDirty();
                    return;
                }

                float num = item.cookTimeLeft * -1f;

                int unitsSmeltedPerCycle = 1;
                foreach (CookableConfig cookable in _smeltingProfile.Cookables)
                {
                    if (cookable.RawItemShortName == item.info.shortname)
                    {
                        unitsSmeltedPerCycle = cookable.UnitsSmeltedPerCookingCycle;
                        break;
                    }
                }

                int num2 = (1 + Mathf.FloorToInt(num / itemModCookable.cookTime)) * unitsSmeltedPerCycle;

                item.cookTimeLeft = itemModCookable.cookTime - num % itemModCookable.cookTime;
                
                num2 = Mathf.Min(num2, item.amount);
                if (item.amount > num2)
                {
                    item.amount -= num2;
                    item.MarkDirty();
                }
                else
                {
                    item.Remove(0f);
                }

                if (itemModCookable.becomeOnCooked != null)
                {
                    int amountOfBecome = 1;
                    foreach (var cookable in _smeltingProfile.Cookables)
                    {
                        if (cookable.RawItemShortName == item.info.shortname)
                        {
                            amountOfBecome = cookable.AmountProducedPerUnitCooked;
                            break;
                        }
                    }

                    Item item2 = ItemManager.Create(itemModCookable.becomeOnCooked, amountOfBecome * num2, 0UL);
                    if (item2 != null && !item2.MoveToContainer(item.parent, -1, true, false, null, true) && !item2.MoveToContainer(item.parent, -1, true, false, null, true))
                    {
                        item2.Drop(item.parent.dropPosition, item.parent.dropVelocity, default(Quaternion));
                        if (item.parent.entityOwner && _oven != null)
                        {
                            OvenFull();
                        }
                    }
                }

            }

            private void OvenFull()
            {
                StopCooking();
            }

            #endregion Cooking Logic

            #region Smelting Profile

            public void UpdateSmeltingProfile(SmeltingProfileConfig newProfile)
            {
                _smeltingProfile = newProfile;
                _oven.smeltSpeed = newProfile.SmeltingSpeed;
            }

            #endregion Smelting Profile
        }

        #endregion Custom Smelter Component

        #region Custom Smelter Manager

        private class CustomSmelterManager
        {
            private Dictionary<BaseOven, CustomSmelterComponent> _ovenSmelters = new Dictionary<BaseOven, CustomSmelterComponent>();

            public CustomSmelterComponent GetOrAddCustomSmelterToOven(BaseOven oven, SmeltingProfileConfig smeltingProfile)
            {
                if (_ovenSmelters.TryGetValue(oven, out CustomSmelterComponent existingSmelter))
                {
                    existingSmelter.UpdateSmeltingProfile(smeltingProfile);
                    return existingSmelter;
                }

                CustomSmelterComponent newSmelter = CustomSmelterComponent.Install(oven, this, smeltingProfile);
                _ovenSmelters[oven] = newSmelter;

                return newSmelter;
            }

            public void HandleOvenKilled(BaseOven oven)
            {
                if (_ovenSmelters.ContainsKey(oven))
                {
                    _ovenSmelters.Remove(oven);
                }
            }

            public void Unload()
            {
                foreach (var pair in _ovenSmelters.ToArray())
                {
                    BaseOven oven = pair.Key;
                    CustomSmelterComponent customSmelter = pair.Value;

                    if (customSmelter != null)
                    {
                        bool wasOn = oven.IsOn();
                        customSmelter.DestroySelf();
                        if (wasOn && oven != null)
                        {
                            oven.StartCooking();
                        }
                    }
                }

                _ovenSmelters.Clear();
            }

            public void DestroyCustomSmelterForOven(BaseOven oven)
            {
                if (_ovenSmelters.TryGetValue(oven, out CustomSmelterComponent customSmelter) && customSmelter != null)
                {
                    customSmelter.DestroySelf();
                    _ovenSmelters.Remove(oven);
                }
            }

            public CustomSmelterComponent GetCustomSmelterForOven(BaseOven oven)
            {
                if (_ovenSmelters.TryGetValue(oven, out CustomSmelterComponent customSmelter))
                    return customSmelter;

                return null;
            }

            public bool OvenHasCustomSmelter(BaseOven oven)
            {
                return _ovenSmelters.ContainsKey(oven);
            }

            public IEnumerable<CustomSmelterComponent> GetAllCustomSmelters()
            {
                return _ovenSmelters.Values;
            }
        }

        #endregion Custom Smelter Manager

        #region Smelting Profile Initialization and Retrieval

        private void InitializeSmeltingProfiles()
        {
            foreach (OvenConfig ovenConfig in _config.Ovens)
            {
                foreach (var profile in ovenConfig.SmeltingProfiles)
                {
                    string permissionSuffix = profile.Key;
                    SmeltingProfileConfig smeltingProfile = profile.Value;

                    string permission = PermissionUtil.AddPermission(permissionSuffix);
                    smeltingProfile.Permission = permission;
                }
            }
        }

        private SmeltingProfileConfig GetSmeltingProfile(BasePlayer player, OvenConfig ovenConfig)
        {
            SmeltingProfileConfig bestProfile = null;
            int highestPriority = int.MinValue;

            foreach (var kvp in ovenConfig.SmeltingProfiles)
            {
                SmeltingProfileConfig profile = kvp.Value;

                if (PermissionUtil.HasPermission(player, profile.Permission))
                {
                    if (profile.Priority > highestPriority)
                    {
                        highestPriority = profile.Priority;
                        bestProfile = profile;
                    }
                }
            }

            if (bestProfile != null)
                return bestProfile;

            if (ovenConfig.SmeltingProfiles.TryGetValue(ovenConfig.DefaultProfile, out SmeltingProfileConfig defaultProfile))
                return defaultProfile;

            return null;
        }

        #endregion Smelting Profile Initialization and Retrieval

        #region Oven Initialization

        private IEnumerator InitializeOvensOnStartupCoroutine()
        {
            foreach (BaseOven oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
            {
                if (oven != null)
                {
                    BasePlayer player = FindPlayerById(oven.OwnerID);
                    if (player == null)
                        continue;

                    OvenConfig ovenConfig = GetOvenConfig(oven);
                    if (ovenConfig != null)
                    {
                        SmeltingProfileConfig smeltingProfile = GetSmeltingProfile(player, ovenConfig);
                        if (smeltingProfile != null)
                        {
                            CustomSmelterComponent customSmelter = _customSmelterManager.GetOrAddCustomSmelterToOven(oven, smeltingProfile);
                            if (customSmelter != null)
                            {
                                if (oven.IsOn())
                                {
                                    oven.StopCooking();
                                    customSmelter.StartCooking();
                                }
                            }
                        }
                    }
                }

                yield return null;
            }
        }

        #endregion Oven Initialization

        #region Helper Classes

        public static class CoroutineUtil
        {
            private static readonly Dictionary<string, Coroutine> _activeCoroutines = new Dictionary<string, Coroutine>();

            public static Coroutine StartCoroutine(string baseCoroutineName, IEnumerator coroutineFunction, string uniqueSuffix = null)
            {
                string coroutineName;

                if (uniqueSuffix != null)
                    coroutineName = baseCoroutineName + "_" + uniqueSuffix;
                else
                    coroutineName = baseCoroutineName;

                StopCoroutine(coroutineName);

                Coroutine coroutine = ServerMgr.Instance.StartCoroutine(coroutineFunction);
                _activeCoroutines[coroutineName] = coroutine;
                return coroutine;
            }

            public static void StopCoroutine(string baseCoroutineName, string uniqueSuffix = null)
            {
                string coroutineName;

                if (uniqueSuffix != null)
                    coroutineName = baseCoroutineName + "_" + uniqueSuffix;
                else
                    coroutineName = baseCoroutineName;

                if (_activeCoroutines.TryGetValue(coroutineName, out Coroutine coroutine))
                {
                    if (coroutine != null)
                        ServerMgr.Instance.StopCoroutine(coroutine);

                    _activeCoroutines.Remove(coroutineName);
                }
            }

            public static void StopAllCoroutines()
            {
                foreach (string coroutineName in _activeCoroutines.Keys.ToArray())
                {
                    StopCoroutine(coroutineName);
                }
            }
        }

        #endregion Helper Classes

        #region Helper Functions

        public static BasePlayer FindPlayerById(ulong playerId)
        {
            return RelationshipManager.FindByID(playerId);
        }

        private OvenConfig GetOvenConfig(BaseOven oven)
        {
            foreach (OvenConfig ovenConfig in _config.Ovens)
            {
                if (ovenConfig.PrefabShortNames.Contains(oven.ShortPrefabName))
                {
                    return ovenConfig;
                }
            }
            return null;
        }

        private static bool ChanceSucceeded(int percent)
        {
            if (percent <= 0)
                return false;

            if (percent >= 100)
                return true;

            float roll = Random.Range(0f, 100f);
            return roll < percent;
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            private static readonly List<string> _permissions = new List<string>
            {
                
            };

            public static string AddPermission(string suffix)
            {
                string permission = ConstructPermission(suffix);
                if (!_permissions.Contains(permission))
                {
                    _permissions.Add(permission);
                }
                return permission;
            }

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static string ConstructPermission(string suffix)
            {
                return string.Join(".", nameof(FurnaceGoBrrr), suffix).ToLower();
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions
    }
}