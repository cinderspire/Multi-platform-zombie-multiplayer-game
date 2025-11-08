using UnityEngine;

namespace DeadFrontier.Core.Data
{
    /// <summary>
    /// Extended data presets with additional weapons, perks, and content
    /// </summary>
    public static class ExtendedDataPresets
    {
        #region Additional Weapons

        public static class Weapons
        {
            // PISTOL - Desert Eagle (Heavy Pistol)
            public static readonly DataPresets.WeaponPreset DesertEagle = new DataPresets.WeaponPreset
            {
                weaponName = "Desert Eagle .50",
                weaponType = DeadFrontier.Weapons.WeaponType.Pistol,
                damage = 55,
                fireRate = 0.3f,
                range = 60f,
                magazineSize = 7,
                maxReserveAmmo = 35,
                reloadTime = 2f,
                firingMode = DeadFrontier.Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(5f, 15f),
                baseSpread = 3f,
                movementSpreadMultiplier = 1.8f,
                aimSpreadMultiplier = 0.4f,
                fireSoundNoiseLevel = 45f
            };

            // SMG - Uzi (High ROF)
            public static readonly DataPresets.WeaponPreset Uzi = new DataPresets.WeaponPreset
            {
                weaponName = "Uzi SMG",
                weaponType = DeadFrontier.Weapons.WeaponType.SMG,
                damage = 18,
                fireRate = 0.06f, // Very fast
                range = 35f,
                magazineSize = 32,
                maxReserveAmmo = 192,
                reloadTime = 1.8f,
                firingMode = DeadFrontier.Weapons.FiringMode.Automatic,
                recoilPattern = new Vector2(3f, 8f),
                baseSpread = 5f,
                movementSpreadMultiplier = 1.1f,
                aimSpreadMultiplier = 0.5f,
                fireSoundNoiseLevel = 35f
            };

            // ASSAULT RIFLE - M4A1 (Balanced)
            public static readonly DataPresets.WeaponPreset M4A1 = new DataPresets.WeaponPreset
            {
                weaponName = "M4A1 Carbine",
                weaponType = DeadFrontier.Weapons.WeaponType.AssaultRifle,
                damage = 28,
                fireRate = 0.09f,
                range = 120f,
                magazineSize = 30,
                maxReserveAmmo = 180,
                reloadTime = 2.2f,
                firingMode = DeadFrontier.Weapons.FiringMode.Automatic,
                recoilPattern = new Vector2(2f, 7f),
                baseSpread = 2.5f,
                movementSpreadMultiplier = 1.8f,
                aimSpreadMultiplier = 0.25f,
                fireSoundNoiseLevel = 48f
            };

            // SHOTGUN - Double Barrel (High Burst Damage)
            public static readonly DataPresets.WeaponPreset DoubleBarrel = new DataPresets.WeaponPreset
            {
                weaponName = "Double Barrel Shotgun",
                weaponType = DeadFrontier.Weapons.WeaponType.Shotgun,
                damage = 20, // Per pellet
                fireRate = 0.5f,
                range = 25f,
                magazineSize = 2,
                maxReserveAmmo = 24,
                reloadTime = 0.8f, // Per shell
                firingMode = DeadFrontier.Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(8f, 20f),
                baseSpread = 18f,
                movementSpreadMultiplier = 1.4f,
                aimSpreadMultiplier = 0.8f,
                pelletsPerShot = 10,
                fireSoundNoiseLevel = 70f
            };

            // SHOTGUN - Auto Shotgun (AA-12)
            public static readonly DataPresets.WeaponPreset AutoShotgun = new DataPresets.WeaponPreset
            {
                weaponName = "AA-12 Auto Shotgun",
                weaponType = DeadFrontier.Weapons.WeaponType.Shotgun,
                damage = 12, // Per pellet
                fireRate = 0.25f,
                range = 28f,
                magazineSize = 8,
                maxReserveAmmo = 32,
                reloadTime = 3f,
                firingMode = DeadFrontier.Weapons.FiringMode.Automatic,
                recoilPattern = new Vector2(4f, 12f),
                baseSpread = 16f,
                movementSpreadMultiplier = 1.5f,
                aimSpreadMultiplier = 0.75f,
                pelletsPerShot = 8,
                fireSoundNoiseLevel = 65f
            };

            // SNIPER - Semi-Auto DMR
            public static readonly DataPresets.WeaponPreset DMR = new DataPresets.WeaponPreset
            {
                weaponName = "M14 DMR",
                weaponType = DeadFrontier.Weapons.WeaponType.SniperRifle,
                damage = 60,
                fireRate = 0.5f,
                range = 180f,
                magazineSize = 10,
                maxReserveAmmo = 60,
                reloadTime = 2.5f,
                firingMode = DeadFrontier.Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(5f, 12f),
                baseSpread = 1f,
                movementSpreadMultiplier = 3f,
                aimSpreadMultiplier = 0.15f,
                fireSoundNoiseLevel = 68f
            };

            // LMG - Light Machine Gun
            public static readonly DataPresets.WeaponPreset LMG = new DataPresets.WeaponPreset
            {
                weaponName = "M249 LMG",
                weaponType = DeadFrontier.Weapons.WeaponType.LMG,
                damage = 26,
                fireRate = 0.1f,
                range = 90f,
                magazineSize = 100,
                maxReserveAmmo = 300,
                reloadTime = 4.5f,
                firingMode = DeadFrontier.Weapons.FiringMode.Automatic,
                recoilPattern = new Vector2(4f, 11f),
                baseSpread = 4f,
                movementSpreadMultiplier = 3f,
                aimSpreadMultiplier = 0.4f,
                fireSoundNoiseLevel = 55f
            };

            // Revolver - High Damage Single Action
            public static readonly DataPresets.WeaponPreset Revolver = new DataPresets.WeaponPreset
            {
                weaponName = ".44 Magnum Revolver",
                weaponType = DeadFrontier.Weapons.WeaponType.Pistol,
                damage = 70,
                fireRate = 0.4f,
                range = 55f,
                magazineSize = 6,
                maxReserveAmmo = 36,
                reloadTime = 2.5f,
                firingMode = DeadFrontier.Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(6f, 18f),
                baseSpread = 2f,
                movementSpreadMultiplier = 1.6f,
                aimSpreadMultiplier = 0.3f,
                fireSoundNoiseLevel = 50f
            };

            // Burst Rifle - 3-Round Burst
            public static readonly DataPresets.WeaponPreset BurstRifle = new DataPresets.WeaponPreset
            {
                weaponName = "M16A4 Burst Rifle",
                weaponType = DeadFrontier.Weapons.WeaponType.AssaultRifle,
                damage = 32,
                fireRate = 0.12f,
                range = 130f,
                magazineSize = 30,
                maxReserveAmmo = 180,
                reloadTime = 2.3f,
                firingMode = DeadFrontier.Weapons.FiringMode.Burst,
                recoilPattern = new Vector2(2.5f, 8f),
                baseSpread = 2f,
                movementSpreadMultiplier = 1.9f,
                aimSpreadMultiplier = 0.2f,
                fireSoundNoiseLevel = 50f
            };

            // Crossbow - Silent Weapon
            public static readonly DataPresets.WeaponPreset Crossbow = new DataPresets.WeaponPreset
            {
                weaponName = "Tactical Crossbow",
                weaponType = DeadFrontier.Weapons.WeaponType.Special,
                damage = 100,
                fireRate = 1.5f,
                range = 80f,
                magazineSize = 1,
                maxReserveAmmo = 30,
                reloadTime = 2f,
                firingMode = DeadFrontier.Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(0f, 2f),
                baseSpread = 0.5f,
                movementSpreadMultiplier = 2f,
                aimSpreadMultiplier = 0.1f,
                fireSoundNoiseLevel = 5f // Very quiet
            };
        }

        #endregion

        #region Perk Presets

        public static class Perks
        {
            // COMBAT PERKS
            public static readonly PerkPreset StoppingPower = new PerkPreset
            {
                perkName = "Stopping Power",
                description = "Increases weapon damage by 25%",
                category = Player.Perks.PerkCategory.Combat,
                rarity = Player.Perks.PerkRarity.Rare,
                levelRequired = 10,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.WeaponDamage,
                        value = 1.25f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset Juggernaut = new PerkPreset
            {
                perkName = "Juggernaut",
                description = "Increases max health by 50",
                category = Player.Perks.PerkCategory.Survival,
                rarity = Player.Perks.PerkRarity.Epic,
                levelRequired = 15,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.MaxHealth,
                        value = 50f,
                        isMultiplicative = false
                    }
                }
            };

            public static readonly PerkPreset SleightOfHand = new PerkPreset
            {
                perkName = "Sleight of Hand",
                description = "50% faster reload speed",
                category = Player.Perks.PerkCategory.Combat,
                rarity = Player.Perks.PerkRarity.Uncommon,
                levelRequired = 5,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.ReloadSpeed,
                        value = 1.5f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset Marathon = new PerkPreset
            {
                perkName = "Marathon",
                description = "Unlimited sprint duration",
                category = Player.Perks.PerkCategory.Movement,
                rarity = Player.Perks.PerkRarity.Rare,
                levelRequired = 12,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.StaminaMax,
                        value = 999f,
                        isMultiplicative = false
                    }
                }
            };

            public static readonly PerkPreset Lightweight = new PerkPreset
            {
                perkName = "Lightweight",
                description = "Move 15% faster",
                category = Player.Perks.PerkCategory.Movement,
                rarity = Player.Perks.PerkRarity.Common,
                levelRequired = 3,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.MovementSpeed,
                        value = 1.15f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset SteadyAim = new PerkPreset
            {
                perkName = "Steady Aim",
                description = "40% less hip-fire spread",
                category = Player.Perks.PerkCategory.Combat,
                rarity = Player.Perks.PerkRarity.Uncommon,
                levelRequired = 8,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.RecoilReduction,
                        value = 0.6f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset Scavenger = new PerkPreset
            {
                perkName = "Scavenger",
                description = "Increased ammo from pickups",
                category = Player.Perks.PerkCategory.Utility,
                rarity = Player.Perks.PerkRarity.Common,
                levelRequired = 4,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.AmmoCapacity,
                        value = 1.5f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset Commando = new PerkPreset
            {
                perkName = "Commando",
                description = "Increased melee range and damage",
                category = Player.Perks.PerkCategory.Combat,
                rarity = Player.Perks.PerkRarity.Uncommon,
                levelRequired = 7,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.MeleeRange,
                        value = 1.5f,
                        isMultiplicative = true
                    },
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.MeleeDamage,
                        value = 1.3f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset QuickRevive = new PerkPreset
            {
                perkName = "Quick Revive",
                description = "Revive teammates 50% faster",
                category = Player.Perks.PerkCategory.Support,
                rarity = Player.Perks.PerkRarity.Rare,
                levelRequired = 14,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.ReviveSpeed,
                        value = 1.5f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset SilentMovement = new PerkPreset
            {
                perkName = "Silent Movement",
                description = "Footsteps attract zombies 60% less",
                category = Player.Perks.PerkCategory.Utility,
                rarity = Player.Perks.PerkRarity.Epic,
                levelRequired = 18,
                statModifiers = new[]
                {
                    new Player.Perks.PerkStat
                    {
                        statType = Player.Perks.StatType.NoiseReduction,
                        value = 0.4f,
                        isMultiplicative = true
                    }
                }
            };

            public static readonly PerkPreset Hardline = new PerkPreset
            {
                perkName = "Hardline",
                description = "20% bonus XP from all sources",
                category = Player.Perks.PerkCategory.Utility,
                rarity = Player.Perks.PerkRarity.Legendary,
                levelRequired = 25,
                statModifiers = new Player.Perks.PerkStat[] { } // Special implementation needed
            };

            public static readonly PerkPreset LastStand = new PerkPreset
            {
                perkName = "Last Stand",
                description = "Survive with 1 HP when taking fatal damage (once per match)",
                category = Player.Perks.PerkCategory.Survival,
                rarity = Player.Perks.PerkRarity.Legendary,
                levelRequired = 30,
                statModifiers = new Player.Perks.PerkStat[] { } // Special implementation needed
            };
        }

        #endregion

        #region Challenge Presets

        public static class Challenges
        {
            // DAILY CHALLENGES
            public static readonly ChallengePreset KillZombiesDaily = new ChallengePreset
            {
                challengeName = "Exterminator",
                description = "Kill 50 zombies",
                challengeType = Gameplay.Challenges.ChallengeType.Daily,
                difficulty = Gameplay.Challenges.ChallengeDifficulty.Easy,
                objectives = new[]
                {
                    new Gameplay.Challenges.ChallengeObjective
                    {
                        objectiveType = Gameplay.Challenges.ChallengeObjectiveType.KillZombies,
                        description = "Kill zombies",
                        targetValue = 50
                    }
                },
                xpReward = 100,
                currencyReward = 50
            };

            public static readonly ChallengePreset HeadshotsDaily = new ChallengePreset
            {
                challengeName = "Marksman",
                description = "Get 20 headshots",
                challengeType = Gameplay.Challenges.ChallengeType.Daily,
                difficulty = Gameplay.Challenges.ChallengeDifficulty.Medium,
                objectives = new[]
                {
                    new Gameplay.Challenges.ChallengeObjective
                    {
                        objectiveType = Gameplay.Challenges.ChallengeObjectiveType.GetHeadshots,
                        description = "Headshot kills",
                        targetValue = 20
                    }
                },
                xpReward = 150,
                currencyReward = 75
            };

            public static readonly ChallengePreset ExtractDaily = new ChallengePreset
            {
                challengeName = "Survivor",
                description = "Successfully extract 3 times",
                challengeType = Gameplay.Challenges.ChallengeType.Daily,
                difficulty = Gameplay.Challenges.ChallengeDifficulty.Medium,
                objectives = new[]
                {
                    new Gameplay.Challenges.ChallengeObjective
                    {
                        objectiveType = Gameplay.Challenges.ChallengeObjectiveType.SuccessfulExtractions,
                        description = "Successful extractions",
                        targetValue = 3
                    }
                },
                xpReward = 200,
                currencyReward = 100
            };

            // WEEKLY CHALLENGES
            public static readonly ChallengePreset MassExtermination = new ChallengePreset
            {
                challengeName = "Mass Extermination",
                description = "Kill 500 zombies",
                challengeType = Gameplay.Challenges.ChallengeType.Weekly,
                difficulty = Gameplay.Challenges.ChallengeDifficulty.Hard,
                objectives = new[]
                {
                    new Gameplay.Challenges.ChallengeObjective
                    {
                        objectiveType = Gameplay.Challenges.ChallengeObjectiveType.KillZombies,
                        description = "Kill zombies",
                        targetValue = 500
                    }
                },
                xpReward = 1000,
                currencyReward = 500,
                battlePassXPReward = 500
            };

            public static readonly ChallengePreset WeaponMaster = new ChallengePreset
            {
                challengeName = "Weapon Master",
                description = "Get 10 kills with 5 different weapon types",
                challengeType = Gameplay.Challenges.ChallengeType.Weekly,
                difficulty = Gameplay.Challenges.ChallengeDifficulty.Hard,
                objectives = new[]
                {
                    new Gameplay.Challenges.ChallengeObjective
                    {
                        objectiveType = Gameplay.Challenges.ChallengeObjectiveType.GetKillsWithWeaponType,
                        description = "Weapon type variety",
                        targetValue = 5
                    }
                },
                xpReward = 800,
                currencyReward = 400,
                battlePassXPReward = 400
            };
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public struct PerkPreset
        {
            public string perkName;
            public string description;
            public Player.Perks.PerkCategory category;
            public Player.Perks.PerkRarity rarity;
            public int levelRequired;
            public Player.Perks.PerkStat[] statModifiers;
        }

        [System.Serializable]
        public struct ChallengePreset
        {
            public string challengeName;
            public string description;
            public Gameplay.Challenges.ChallengeType challengeType;
            public Gameplay.Challenges.ChallengeDifficulty difficulty;
            public Gameplay.Challenges.ChallengeObjective[] objectives;
            public int xpReward;
            public int currencyReward;
            public int battlePassXPReward;
        }

        #endregion
    }
}
