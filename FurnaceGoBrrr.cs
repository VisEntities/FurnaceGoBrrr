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
    [Info("Furnace Go Brrr", "VisEntities", "1.0.0")]
    [Description(" ")]
    public class FurnaceGoBrrr : RustPlugin
    {
        #region Fields

        private static FurnaceGoBrrr _plugin;
        private static Configuration _config;
        private CustomSmelterManager _customSmelterManager;

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

            [JsonProperty("Smelting Speed")]
            public int SmeltingSpeed { get; set; }

            [JsonProperty("Burnable")]
            public BurnableConfig Burnable { get; set; }

            [JsonProperty("Cookables")]
            public List<CookableConfig> Cookables { get; set; }
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

            [JsonProperty("Amount Produced Per Unit Cooked")]
            public int AmountProducedPerUnitCooked { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

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
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "sulfur.ore",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "hq.metal.ore",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.beans.empty",
                                AmountProducedPerUnitCooked = 15
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.tuna.empty",
                                AmountProducedPerUnitCooked = 10
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "electricfurnace.deployed",
                        },
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
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "sulfur.ore",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "hq.metal.ore",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.beans.empty",
                                AmountProducedPerUnitCooked = 15
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.tuna.empty",
                                AmountProducedPerUnitCooked = 10
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "furnace.large"
                        },
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
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "sulfur.ore",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "hq.metal.ore",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.beans.empty",
                                AmountProducedPerUnitCooked = 15
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.tuna.empty",
                                AmountProducedPerUnitCooked = 10
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "refinery_small_deployed"
                        },
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
                                AmountProducedPerUnitCooked = 3
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.beans.empty",
                                AmountProducedPerUnitCooked = 15
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.tuna.empty",
                                AmountProducedPerUnitCooked = 10
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
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "chicken.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "deermeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "fish.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "horsemeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "humanmeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "meat.boar",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "wolfmeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.beans.empty",
                                AmountProducedPerUnitCooked = 15
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.tuna.empty",
                                AmountProducedPerUnitCooked = 10
                            }
                        }
                    },
                    new OvenConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "bbq.deployed"
                        },
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
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "chicken.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "deermeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "fish.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "horsemeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "humanmeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "meat.boar",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "wolfmeat.raw",
                                AmountProducedPerUnitCooked = 1
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.beans.empty",
                                AmountProducedPerUnitCooked = 15
                            },
                            new CookableConfig
                            {
                                RawItemShortName = "can.tuna.empty",
                                AmountProducedPerUnitCooked = 10
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
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            _customSmelterManager = new CustomSmelterManager();
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
            CoroutineUtil.StartCoroutine(Guid.NewGuid().ToString(), InitializeAllOvensCoroutine());
        }

        private void OnEntitySpawned(BaseOven oven)
        {
            if (oven == null)
                return;

            OvenConfig ovenConfig = GetOvenConfig(oven);
            if (ovenConfig == null)
                return;

            CustomSmelterComponent customSmelter = _customSmelterManager.GetOrAddCustomSmelterToOven(oven, ovenConfig);        
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

            OvenConfig ovenConfig = GetOvenConfig(oven);
            if (ovenConfig == null)
                return null;

            CustomSmelterComponent customSmelter = _customSmelterManager.GetOrAddCustomSmelterToOven(oven, ovenConfig);
            if (customSmelter != null)
            {
                customSmelter.StartCooking();
            }

            return true;
        }

        #endregion Oxide Hooks

        #region Custom Smelter Component

        private class CustomSmelterComponent : FacepunchBehaviour
        {
            #region Fields

            private BaseOven _oven;
            private CustomSmelterManager _customSmelterManager;
            private OvenConfig _ovenConfig;

            #endregion Fields

            #region Component Management

            public static CustomSmelterComponent InstallComponent(BaseOven oven, CustomSmelterManager customSmelterManager, OvenConfig ovenConfig)
            {
                CustomSmelterComponent component = oven.gameObject.AddComponent<CustomSmelterComponent>();
                component.InitializeComponent(customSmelterManager, ovenConfig);
                return component;
            }

            public CustomSmelterComponent InitializeComponent(CustomSmelterManager customSmelterManager, OvenConfig ovenConfig)
            {
                _oven = GetComponent<BaseOven>();
                _customSmelterManager = customSmelterManager;
                _ovenConfig = ovenConfig;

                return this;
            }

            public static CustomSmelterComponent GetComponent(GameObject gameObject)
            {
                return gameObject.GetComponent<CustomSmelterComponent>();
            }

            public void DestroyComponent()
            {
                DestroyImmediate(this);
            }

            #endregion Component Management

            #region Component Lifecycle

            private void OnDestroy()
            {
                StopCooking();
                _customSmelterManager.HandleOvenKilled(_oven);
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

                IncreaseCookTime(0.5f * _ovenConfig.SmeltingSpeed);
           
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

                if (_oven.allowByproductCreation && _ovenConfig.Burnable.EnableByproductCreation
                    && burnable.byproductItem != null && ChanceSucceeded(_ovenConfig.Burnable.ByproductCreationChance))
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
                return _ovenConfig.Burnable.FuelConsumptionRate;
            }

            private int GetCharcoalRate()
            {
                return _ovenConfig.Burnable.ByproductCreationRatePerUnitFuel;
            }

            private void IncreaseCookTime(float smeltingSpeed)
            {
                List<Item> itemsCurrentlyBeingCooked = Facepunch.Pool.GetList<Item>();
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

                Facepunch.Pool.FreeList(ref itemsCurrentlyBeingCooked);
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
                int num2 = 1 + Mathf.FloorToInt(num / itemModCookable.cookTime);
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
                    int amountOfBecome = itemModCookable.amountOfBecome;
                    foreach (var cookable in _ovenConfig.Cookables)
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
        }

        #endregion Custom Smelter Component

        #region Custom Smelter Manager

        private class CustomSmelterManager
        {
            private Dictionary<BaseOven, CustomSmelterComponent> _ovenSmelters = new Dictionary<BaseOven, CustomSmelterComponent>();

            public CustomSmelterComponent GetOrAddCustomSmelterToOven(BaseOven oven, OvenConfig ovenConfig)
            {
                if (_ovenSmelters.TryGetValue(oven, out CustomSmelterComponent existingSmelter))
                    return existingSmelter;

                CustomSmelterComponent newSmelter = CustomSmelterComponent.InstallComponent(oven, this, ovenConfig);
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
                        customSmelter.DestroyComponent();
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
                    customSmelter.DestroyComponent();
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

        #region Ovens Initialization

        private IEnumerator InitializeAllOvensCoroutine()
        {
            foreach (BaseOven oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
            {
                if (oven != null)
                {
                    OvenConfig ovenConfig = GetOvenConfig(oven);
                    if (ovenConfig != null)
                    {
                        CustomSmelterComponent customSmelter = _customSmelterManager.GetOrAddCustomSmelterToOven(oven, ovenConfig);
                        if (customSmelter != null)
                        {
                            if (oven.IsOn())
                                oven.StopCooking();

                            customSmelter.StartCooking();
                        }
                    }
                }

                yield return null;
            }
        }

        #endregion Ovens Initialization

        #region Helper Classes

        public static class CoroutineUtil
        {
            private static readonly Dictionary<string, Coroutine> _activeCoroutines = new Dictionary<string, Coroutine>();

            public static void StartCoroutine(string coroutineName, IEnumerator coroutineFunction)
            {
                StopCoroutine(coroutineName);

                Coroutine coroutine = ServerMgr.Instance.StartCoroutine(coroutineFunction);
                _activeCoroutines[coroutineName] = coroutine;
            }

            public static void StopCoroutine(string coroutineName)
            {
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

        private OvenConfig GetOvenConfig(BaseOven oven)
        {
            foreach (var config in _config.Ovens)
            {
                if (config.PrefabShortNames.Contains(oven.ShortPrefabName))
                {
                    return config;
                }
            }
            return null;
        }

        private static bool ChanceSucceeded(int probability)
        {
            return Random.Range(0, 100) < probability;
        }

        #endregion Helper Functions
    }
}