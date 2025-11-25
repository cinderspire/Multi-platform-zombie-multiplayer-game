using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Data
{
    /// <summary>
    /// Complete game data presets for all weapons, zombies, items, perks, etc.
    /// Use these values when creating ScriptableObject assets in Unity Editor.
    /// </summary>
    public static class CompleteDataPresets
    {
        #region Weapons - Complete Arsenal

        public static class WeaponPresets
        {
            // === PISTOLS ===
            public static readonly WeaponData M1911 = new WeaponData
            {
                id = "wpn_pistol_m1911",
                name = "M1911 Pistol",
                description = "Classic .45 ACP semi-automatic pistol. Reliable and hard-hitting.",
                type = "Pistol",
                damage = 35f,
                fireRate = 0.2f,
                range = 50f,
                magazineSize = 8,
                maxAmmo = 64,
                reloadTime = 1.8f,
                accuracy = 0.85f,
                recoil = new Vector2(2f, 8f),
                unlockLevel = 1,
                cost = 0,
                rarity = "Common"
            };

            public static readonly WeaponData Glock17 = new WeaponData
            {
                id = "wpn_pistol_glock17",
                name = "Glock 17",
                description = "High-capacity 9mm pistol. Fast and accurate.",
                type = "Pistol",
                damage = 25f,
                fireRate = 0.12f,
                range = 45f,
                magazineSize = 17,
                maxAmmo = 102,
                reloadTime = 1.5f,
                accuracy = 0.88f,
                recoil = new Vector2(1.5f, 6f),
                unlockLevel = 5,
                cost = 2500,
                rarity = "Common"
            };

            public static readonly WeaponData DesertEagle = new WeaponData
            {
                id = "wpn_pistol_deagle",
                name = "Desert Eagle",
                description = "Powerful .50 AE magnum pistol. Devastating at close range.",
                type = "Pistol",
                damage = 75f,
                fireRate = 0.4f,
                range = 60f,
                magazineSize = 7,
                maxAmmo = 35,
                reloadTime = 2.2f,
                accuracy = 0.75f,
                recoil = new Vector2(5f, 15f),
                unlockLevel = 25,
                cost = 15000,
                rarity = "Rare"
            };

            public static readonly WeaponData M9Beretta = new WeaponData
            {
                id = "wpn_pistol_m9",
                name = "M9 Beretta",
                description = "Standard military sidearm. Well-balanced performance.",
                type = "Pistol",
                damage = 28f,
                fireRate = 0.15f,
                range = 48f,
                magazineSize = 15,
                maxAmmo = 90,
                reloadTime = 1.6f,
                accuracy = 0.87f,
                recoil = new Vector2(1.8f, 7f),
                unlockLevel = 3,
                cost = 1500,
                rarity = "Common"
            };

            // === ASSAULT RIFLES ===
            public static readonly WeaponData AK47 = new WeaponData
            {
                id = "wpn_ar_ak47",
                name = "AK-47",
                description = "Legendary assault rifle. High damage with moderate recoil.",
                type = "AssaultRifle",
                damage = 35f,
                fireRate = 0.1f,
                range = 120f,
                magazineSize = 30,
                maxAmmo = 210,
                reloadTime = 2.5f,
                accuracy = 0.78f,
                recoil = new Vector2(3f, 12f),
                unlockLevel = 10,
                cost = 8000,
                rarity = "Uncommon"
            };

            public static readonly WeaponData M4A1 = new WeaponData
            {
                id = "wpn_ar_m4a1",
                name = "M4A1 Carbine",
                description = "Versatile assault rifle. Excellent accuracy and handling.",
                type = "AssaultRifle",
                damage = 30f,
                fireRate = 0.085f,
                range = 130f,
                magazineSize = 30,
                maxAmmo = 210,
                reloadTime = 2.3f,
                accuracy = 0.85f,
                recoil = new Vector2(2.5f, 9f),
                unlockLevel = 15,
                cost = 12000,
                rarity = "Uncommon"
            };

            public static readonly WeaponData SCAR_H = new WeaponData
            {
                id = "wpn_ar_scarh",
                name = "SCAR-H",
                description = "Battle rifle firing 7.62mm. Devastating power at range.",
                type = "AssaultRifle",
                damage = 45f,
                fireRate = 0.12f,
                range = 150f,
                magazineSize = 20,
                maxAmmo = 140,
                reloadTime = 2.8f,
                accuracy = 0.82f,
                recoil = new Vector2(4f, 14f),
                unlockLevel = 35,
                cost = 25000,
                rarity = "Rare"
            };

            public static readonly WeaponData AUG = new WeaponData
            {
                id = "wpn_ar_aug",
                name = "Steyr AUG",
                description = "Bullpup assault rifle with integrated optic.",
                type = "AssaultRifle",
                damage = 32f,
                fireRate = 0.09f,
                range = 125f,
                magazineSize = 30,
                maxAmmo = 180,
                reloadTime = 2.4f,
                accuracy = 0.88f,
                recoil = new Vector2(2.2f, 8f),
                unlockLevel = 20,
                cost = 15000,
                rarity = "Uncommon"
            };

            // === SMGS ===
            public static readonly WeaponData MP5 = new WeaponData
            {
                id = "wpn_smg_mp5",
                name = "MP5",
                description = "Iconic submachine gun. Accurate and controllable.",
                type = "SMG",
                damage = 22f,
                fireRate = 0.07f,
                range = 50f,
                magazineSize = 30,
                maxAmmo = 180,
                reloadTime = 2.0f,
                accuracy = 0.82f,
                recoil = new Vector2(1.5f, 5f),
                unlockLevel = 8,
                cost = 6000,
                rarity = "Common"
            };

            public static readonly WeaponData UMP45 = new WeaponData
            {
                id = "wpn_smg_ump45",
                name = "UMP-45",
                description = ".45 ACP submachine gun. Hard-hitting for its class.",
                type = "SMG",
                damage = 30f,
                fireRate = 0.09f,
                range = 45f,
                magazineSize = 25,
                maxAmmo = 150,
                reloadTime = 2.2f,
                accuracy = 0.78f,
                recoil = new Vector2(2f, 7f),
                unlockLevel = 12,
                cost = 8500,
                rarity = "Uncommon"
            };

            public static readonly WeaponData P90 = new WeaponData
            {
                id = "wpn_smg_p90",
                name = "P90",
                description = "High-capacity PDW with armor-piercing rounds.",
                type = "SMG",
                damage = 20f,
                fireRate = 0.055f,
                range = 55f,
                magazineSize = 50,
                maxAmmo = 200,
                reloadTime = 2.5f,
                accuracy = 0.80f,
                recoil = new Vector2(1.2f, 4f),
                unlockLevel = 30,
                cost = 20000,
                rarity = "Rare"
            };

            // === SHOTGUNS ===
            public static readonly WeaponData Remington870 = new WeaponData
            {
                id = "wpn_sg_remington870",
                name = "Remington 870",
                description = "Pump-action shotgun. Devastating at close range.",
                type = "Shotgun",
                damage = 18f, // Per pellet, 8 pellets = 144 max
                fireRate = 0.9f,
                range = 25f,
                magazineSize = 8,
                maxAmmo = 32,
                reloadTime = 0.5f, // Per shell
                accuracy = 0.65f,
                recoil = new Vector2(6f, 18f),
                pelletsPerShot = 8,
                unlockLevel = 6,
                cost = 4500,
                rarity = "Common"
            };

            public static readonly WeaponData SPAS12 = new WeaponData
            {
                id = "wpn_sg_spas12",
                name = "SPAS-12",
                description = "Semi-automatic combat shotgun. High fire rate.",
                type = "Shotgun",
                damage = 15f,
                fireRate = 0.4f,
                range = 22f,
                magazineSize = 8,
                maxAmmo = 40,
                reloadTime = 0.6f,
                accuracy = 0.60f,
                recoil = new Vector2(5f, 15f),
                pelletsPerShot = 8,
                unlockLevel = 18,
                cost = 14000,
                rarity = "Uncommon"
            };

            public static readonly WeaponData AA12 = new WeaponData
            {
                id = "wpn_sg_aa12",
                name = "AA-12",
                description = "Fully automatic shotgun. Pure destruction.",
                type = "Shotgun",
                damage = 12f,
                fireRate = 0.2f,
                range = 20f,
                magazineSize = 20,
                maxAmmo = 60,
                reloadTime = 3.0f,
                accuracy = 0.55f,
                recoil = new Vector2(4f, 12f),
                pelletsPerShot = 8,
                unlockLevel = 45,
                cost = 35000,
                rarity = "Epic"
            };

            // === SNIPER RIFLES ===
            public static readonly WeaponData M24 = new WeaponData
            {
                id = "wpn_sniper_m24",
                name = "M24 SWS",
                description = "Bolt-action sniper rifle. Precision at extreme range.",
                type = "SniperRifle",
                damage = 95f,
                fireRate = 1.5f,
                range = 300f,
                magazineSize = 5,
                maxAmmo = 30,
                reloadTime = 3.5f,
                accuracy = 0.98f,
                recoil = new Vector2(8f, 20f),
                unlockLevel = 22,
                cost = 18000,
                rarity = "Rare"
            };

            public static readonly WeaponData Barrett = new WeaponData
            {
                id = "wpn_sniper_barrett",
                name = "Barrett M82",
                description = "Anti-materiel rifle. Extreme power, penetrates cover.",
                type = "SniperRifle",
                damage = 150f,
                fireRate = 2.0f,
                range = 400f,
                magazineSize = 10,
                maxAmmo = 40,
                reloadTime = 4.0f,
                accuracy = 0.95f,
                recoil = new Vector2(12f, 25f),
                unlockLevel = 50,
                cost = 50000,
                rarity = "Legendary"
            };

            public static readonly WeaponData SVD = new WeaponData
            {
                id = "wpn_sniper_svd",
                name = "SVD Dragunov",
                description = "Semi-automatic marksman rifle. Quick follow-up shots.",
                type = "SniperRifle",
                damage = 65f,
                fireRate = 0.8f,
                range = 250f,
                magazineSize = 10,
                maxAmmo = 50,
                reloadTime = 3.0f,
                accuracy = 0.92f,
                recoil = new Vector2(6f, 16f),
                unlockLevel = 28,
                cost = 22000,
                rarity = "Rare"
            };

            // === LMGs ===
            public static readonly WeaponData M249 = new WeaponData
            {
                id = "wpn_lmg_m249",
                name = "M249 SAW",
                description = "Squad automatic weapon. Sustained suppressive fire.",
                type = "LMG",
                damage = 28f,
                fireRate = 0.075f,
                range = 100f,
                magazineSize = 100,
                maxAmmo = 300,
                reloadTime = 5.5f,
                accuracy = 0.70f,
                recoil = new Vector2(2.5f, 8f),
                unlockLevel = 32,
                cost = 28000,
                rarity = "Rare"
            };

            public static readonly WeaponData PKM = new WeaponData
            {
                id = "wpn_lmg_pkm",
                name = "PKM",
                description = "General-purpose machine gun. High damage sustained fire.",
                type = "LMG",
                damage = 38f,
                fireRate = 0.09f,
                range = 120f,
                magazineSize = 100,
                maxAmmo = 200,
                reloadTime = 6.0f,
                accuracy = 0.68f,
                recoil = new Vector2(3f, 10f),
                unlockLevel = 40,
                cost = 35000,
                rarity = "Epic"
            };

            // === MELEE ===
            public static readonly WeaponData Knife = new WeaponData
            {
                id = "wpn_melee_knife",
                name = "Combat Knife",
                description = "Standard issue combat knife. Fast and silent.",
                type = "Melee",
                damage = 50f,
                fireRate = 0.6f,
                range = 2f,
                unlockLevel = 1,
                cost = 0,
                rarity = "Common"
            };

            public static readonly WeaponData Machete = new WeaponData
            {
                id = "wpn_melee_machete",
                name = "Machete",
                description = "Large blade for cleaving through enemies.",
                type = "Melee",
                damage = 75f,
                fireRate = 0.8f,
                range = 2.5f,
                unlockLevel = 10,
                cost = 5000,
                rarity = "Uncommon"
            };

            public static readonly WeaponData FireAxe = new WeaponData
            {
                id = "wpn_melee_fireaxe",
                name = "Fire Axe",
                description = "Heavy axe. Slow but devastating damage.",
                type = "Melee",
                damage = 120f,
                fireRate = 1.2f,
                range = 2.8f,
                unlockLevel = 20,
                cost = 12000,
                rarity = "Rare"
            };

            public static readonly WeaponData Katana = new WeaponData
            {
                id = "wpn_melee_katana",
                name = "Katana",
                description = "Japanese sword. Fast, lethal, and stylish.",
                type = "Melee",
                damage = 90f,
                fireRate = 0.5f,
                range = 3f,
                unlockLevel = 35,
                cost = 25000,
                rarity = "Epic"
            };
        }

        #endregion

        #region Zombies - Complete Types

        public static class ZombiePresets
        {
            public static readonly ZombieData Walker = new ZombieData
            {
                id = "zombie_walker",
                name = "Walker",
                description = "Standard shambling zombie. Slow but dangerous in groups.",
                category = "Common",
                health = 100f,
                damage = 15f,
                moveSpeed = 2f,
                attackRange = 2f,
                attackCooldown = 1.5f,
                detectionRange = 25f,
                hearingRange = 40f,
                xpReward = 10,
                currencyReward = 5
            };

            public static readonly ZombieData Runner = new ZombieData
            {
                id = "zombie_runner",
                name = "Runner",
                description = "Fast and aggressive infected. Charges at survivors.",
                category = "Common",
                health = 60f,
                damage = 12f,
                moveSpeed = 7f,
                attackRange = 1.5f,
                attackCooldown = 1f,
                detectionRange = 35f,
                hearingRange = 50f,
                xpReward = 15,
                currencyReward = 8
            };

            public static readonly ZombieData Tank = new ZombieData
            {
                id = "zombie_tank",
                name = "Tank",
                description = "Massive mutated zombie. Extremely tough and powerful.",
                category = "Special",
                health = 800f,
                damage = 50f,
                moveSpeed = 1.5f,
                attackRange = 3f,
                attackCooldown = 2f,
                detectionRange = 20f,
                hearingRange = 35f,
                xpReward = 100,
                currencyReward = 50,
                isBoss = false
            };

            public static readonly ZombieData Exploder = new ZombieData
            {
                id = "zombie_exploder",
                name = "Exploder",
                description = "Bloated zombie that explodes on death or proximity.",
                category = "Special",
                health = 50f,
                damage = 25f,
                moveSpeed = 3f,
                attackRange = 5f,
                attackCooldown = 0f,
                detectionRange = 30f,
                hearingRange = 45f,
                xpReward = 25,
                currencyReward = 15,
                explodesOnDeath = true,
                explosionDamage = 100f,
                explosionRadius = 8f
            };

            public static readonly ZombieData Screamer = new ZombieData
            {
                id = "zombie_screamer",
                name = "Screamer",
                description = "Alerts nearby zombies with piercing screams.",
                category = "Special",
                health = 80f,
                damage = 10f,
                moveSpeed = 4f,
                attackRange = 2f,
                attackCooldown = 3f,
                detectionRange = 50f,
                hearingRange = 70f,
                xpReward = 30,
                currencyReward = 20,
                canCallHorde = true,
                callRange = 80f
            };

            public static readonly ZombieData Spitter = new ZombieData
            {
                id = "zombie_spitter",
                name = "Spitter",
                description = "Ranged zombie that spits acidic projectiles.",
                category = "Special",
                health = 70f,
                damage = 20f,
                moveSpeed = 3f,
                attackRange = 20f,
                attackCooldown = 2.5f,
                detectionRange = 40f,
                hearingRange = 50f,
                xpReward = 35,
                currencyReward = 18,
                isRanged = true
            };

            public static readonly ZombieData Stalker = new ZombieData
            {
                id = "zombie_stalker",
                name = "Stalker",
                description = "Stealthy zombie that ambushes from shadows.",
                category = "Special",
                health = 90f,
                damage = 35f,
                moveSpeed = 6f,
                attackRange = 2f,
                attackCooldown = 1.2f,
                detectionRange = 45f,
                hearingRange = 60f,
                xpReward = 40,
                currencyReward = 25,
                canCloak = true
            };

            public static readonly ZombieData Brute = new ZombieData
            {
                id = "zombie_brute",
                name = "Brute",
                description = "Armored zombie with high damage resistance.",
                category = "Elite",
                health = 400f,
                damage = 40f,
                moveSpeed = 2.5f,
                attackRange = 2.5f,
                attackCooldown = 1.8f,
                detectionRange = 25f,
                hearingRange = 40f,
                xpReward = 75,
                currencyReward = 40,
                armorValue = 50f
            };

            public static readonly ZombieData Crawler = new ZombieData
            {
                id = "zombie_crawler",
                name = "Crawler",
                description = "Legless zombie that moves along the ground.",
                category = "Common",
                health = 40f,
                damage = 18f,
                moveSpeed = 3.5f,
                attackRange = 1.5f,
                attackCooldown = 1f,
                detectionRange = 15f,
                hearingRange = 30f,
                xpReward = 8,
                currencyReward = 4
            };

            // BOSS ZOMBIES
            public static readonly ZombieData Abomination = new ZombieData
            {
                id = "zombie_boss_abomination",
                name = "The Abomination",
                description = "Massive mutated horror. Requires a team to defeat.",
                category = "Boss",
                health = 5000f,
                damage = 80f,
                moveSpeed = 2f,
                attackRange = 4f,
                attackCooldown = 1.5f,
                detectionRange = 60f,
                hearingRange = 100f,
                xpReward = 500,
                currencyReward = 250,
                isBoss = true
            };

            public static readonly ZombieData Behemoth = new ZombieData
            {
                id = "zombie_boss_behemoth",
                name = "Behemoth",
                description = "Towering giant that crushes everything in its path.",
                category = "Boss",
                health = 8000f,
                damage = 120f,
                moveSpeed = 1f,
                attackRange = 5f,
                attackCooldown = 2.5f,
                detectionRange = 40f,
                hearingRange = 80f,
                xpReward = 750,
                currencyReward = 400,
                isBoss = true
            };

            public static readonly ZombieData Matriarch = new ZombieData
            {
                id = "zombie_boss_matriarch",
                name = "The Matriarch",
                description = "Spawns endless waves of infected. Kill her to stop the horde.",
                category = "Boss",
                health = 3000f,
                damage = 30f,
                moveSpeed = 3f,
                attackRange = 3f,
                attackCooldown = 2f,
                detectionRange = 50f,
                hearingRange = 90f,
                xpReward = 600,
                currencyReward = 300,
                isBoss = true,
                canSpawnMinions = true
            };
        }

        #endregion

        #region Items - Complete Inventory

        public static class ItemPresets
        {
            // MEDICAL
            public static readonly ItemData Bandage = new ItemData
            {
                id = "item_medical_bandage",
                name = "Bandage",
                description = "Basic medical supplies. Heals 25 HP over 5 seconds.",
                category = "Medical",
                rarity = "Common",
                stackSize = 5,
                weight = 0.1f,
                sellValue = 25,
                healAmount = 25f,
                healDuration = 5f
            };

            public static readonly ItemData FirstAidKit = new ItemData
            {
                id = "item_medical_firstaid",
                name = "First Aid Kit",
                description = "Complete medical kit. Heals 75 HP instantly.",
                category = "Medical",
                rarity = "Uncommon",
                stackSize = 3,
                weight = 0.5f,
                sellValue = 100,
                healAmount = 75f,
                healDuration = 0f
            };

            public static readonly ItemData MedKit = new ItemData
            {
                id = "item_medical_medkit",
                name = "Medical Kit",
                description = "Advanced medical supplies. Fully restores health.",
                category = "Medical",
                rarity = "Rare",
                stackSize = 2,
                weight = 1f,
                sellValue = 250,
                healAmount = 100f,
                healDuration = 0f
            };

            public static readonly ItemData Adrenaline = new ItemData
            {
                id = "item_medical_adrenaline",
                name = "Adrenaline Shot",
                description = "Emergency stimulant. Heals 50 HP and boosts speed.",
                category = "Medical",
                rarity = "Rare",
                stackSize = 3,
                weight = 0.2f,
                sellValue = 150,
                healAmount = 50f,
                buffType = "Speed",
                buffDuration = 15f
            };

            public static readonly ItemData Painkillers = new ItemData
            {
                id = "item_medical_painkillers",
                name = "Painkillers",
                description = "Reduces damage taken by 25% for 60 seconds.",
                category = "Medical",
                rarity = "Uncommon",
                stackSize = 5,
                weight = 0.1f,
                sellValue = 80,
                buffType = "DamageReduction",
                buffDuration = 60f,
                buffValue = 0.25f
            };

            // AMMO
            public static readonly ItemData PistolAmmo = new ItemData
            {
                id = "item_ammo_pistol",
                name = "9mm Ammo",
                description = "Standard pistol ammunition.",
                category = "Ammo",
                rarity = "Common",
                stackSize = 60,
                weight = 0.02f,
                sellValue = 1
            };

            public static readonly ItemData RifleAmmo = new ItemData
            {
                id = "item_ammo_rifle",
                name = "5.56mm Ammo",
                description = "Standard rifle ammunition.",
                category = "Ammo",
                rarity = "Common",
                stackSize = 60,
                weight = 0.03f,
                sellValue = 2
            };

            public static readonly ItemData ShotgunAmmo = new ItemData
            {
                id = "item_ammo_shotgun",
                name = "12 Gauge Shells",
                description = "Shotgun ammunition.",
                category = "Ammo",
                rarity = "Common",
                stackSize = 24,
                weight = 0.05f,
                sellValue = 3
            };

            public static readonly ItemData SniperAmmo = new ItemData
            {
                id = "item_ammo_sniper",
                name = "7.62mm Ammo",
                description = "High-caliber sniper ammunition.",
                category = "Ammo",
                rarity = "Uncommon",
                stackSize = 30,
                weight = 0.04f,
                sellValue = 5
            };

            public static readonly ItemData HeavyAmmo = new ItemData
            {
                id = "item_ammo_heavy",
                name = "Heavy Ammo",
                description = "Ammunition for LMGs and heavy weapons.",
                category = "Ammo",
                rarity = "Uncommon",
                stackSize = 100,
                weight = 0.03f,
                sellValue = 3
            };

            // VALUABLES
            public static readonly ItemData CashStack = new ItemData
            {
                id = "item_valuable_cash",
                name = "Cash Stack",
                description = "A bundle of cash. Can be sold or used.",
                category = "Valuable",
                rarity = "Common",
                stackSize = 10,
                weight = 0.1f,
                sellValue = 100
            };

            public static readonly ItemData GoldWatch = new ItemData
            {
                id = "item_valuable_goldwatch",
                name = "Gold Watch",
                description = "Expensive timepiece. Highly valuable.",
                category = "Valuable",
                rarity = "Rare",
                stackSize = 1,
                weight = 0.2f,
                sellValue = 500
            };

            public static readonly ItemData Diamond = new ItemData
            {
                id = "item_valuable_diamond",
                name = "Diamond",
                description = "Precious gemstone. Extremely valuable.",
                category = "Valuable",
                rarity = "Epic",
                stackSize = 1,
                weight = 0.1f,
                sellValue = 2000
            };

            public static readonly ItemData GoldBar = new ItemData
            {
                id = "item_valuable_goldbar",
                name = "Gold Bar",
                description = "Pure gold ingot. Worth a fortune.",
                category = "Valuable",
                rarity = "Legendary",
                stackSize = 1,
                weight = 2f,
                sellValue = 5000
            };

            public static readonly ItemData AntiqueCoin = new ItemData
            {
                id = "item_valuable_antiquecoin",
                name = "Antique Coin",
                description = "Rare collector's coin.",
                category = "Valuable",
                rarity = "Rare",
                stackSize = 5,
                weight = 0.05f,
                sellValue = 350
            };

            // CONSUMABLES
            public static readonly ItemData EnergyDrink = new ItemData
            {
                id = "item_consumable_energydrink",
                name = "Energy Drink",
                description = "Restores stamina and boosts regen for 30s.",
                category = "Consumable",
                rarity = "Common",
                stackSize = 5,
                weight = 0.3f,
                sellValue = 30,
                buffType = "StaminaRegen",
                buffDuration = 30f
            };

            public static readonly ItemData Rations = new ItemData
            {
                id = "item_consumable_rations",
                name = "Military Rations",
                description = "MRE pack. Slowly restores health over time.",
                category = "Consumable",
                rarity = "Uncommon",
                stackSize = 3,
                weight = 0.5f,
                sellValue = 50,
                healAmount = 40f,
                healDuration = 60f
            };

            // CRAFTING MATERIALS
            public static readonly ItemData ScrapMetal = new ItemData
            {
                id = "item_material_scrapmetal",
                name = "Scrap Metal",
                description = "Salvaged metal. Used for crafting.",
                category = "Material",
                rarity = "Common",
                stackSize = 50,
                weight = 0.3f,
                sellValue = 5
            };

            public static readonly ItemData ElectronicParts = new ItemData
            {
                id = "item_material_electronics",
                name = "Electronic Parts",
                description = "Salvaged electronics. Used for advanced crafting.",
                category = "Material",
                rarity = "Uncommon",
                stackSize = 30,
                weight = 0.2f,
                sellValue = 15
            };

            public static readonly ItemData Chemicals = new ItemData
            {
                id = "item_material_chemicals",
                name = "Chemical Compound",
                description = "Chemical substances. Used for medical crafting.",
                category = "Material",
                rarity = "Uncommon",
                stackSize = 20,
                weight = 0.4f,
                sellValue = 20
            };

            public static readonly ItemData Gunpowder = new ItemData
            {
                id = "item_material_gunpowder",
                name = "Gunpowder",
                description = "Explosive compound. Used for ammo crafting.",
                category = "Material",
                rarity = "Common",
                stackSize = 40,
                weight = 0.1f,
                sellValue = 8
            };

            public static readonly ItemData Cloth = new ItemData
            {
                id = "item_material_cloth",
                name = "Cloth",
                description = "Fabric material. Used for medical items.",
                category = "Material",
                rarity = "Common",
                stackSize = 50,
                weight = 0.1f,
                sellValue = 3
            };

            // KEYS & KEYCARDS
            public static readonly ItemData KeycardBlue = new ItemData
            {
                id = "item_key_blue",
                name = "Blue Keycard",
                description = "Access card for low-security areas.",
                category = "Key",
                rarity = "Uncommon",
                stackSize = 1,
                weight = 0.05f,
                sellValue = 200
            };

            public static readonly ItemData KeycardRed = new ItemData
            {
                id = "item_key_red",
                name = "Red Keycard",
                description = "Access card for high-security areas.",
                category = "Key",
                rarity = "Rare",
                stackSize = 1,
                weight = 0.05f,
                sellValue = 500
            };

            public static readonly ItemData KeycardBlack = new ItemData
            {
                id = "item_key_black",
                name = "Black Keycard",
                description = "Access card for top-secret areas.",
                category = "Key",
                rarity = "Epic",
                stackSize = 1,
                weight = 0.05f,
                sellValue = 1500
            };

            // GRENADES
            public static readonly ItemData FragGrenade = new ItemData
            {
                id = "item_grenade_frag",
                name = "Frag Grenade",
                description = "Explosive grenade. High damage in radius.",
                category = "Throwable",
                rarity = "Uncommon",
                stackSize = 4,
                weight = 0.4f,
                sellValue = 150,
                damage = 150f,
                radius = 8f
            };

            public static readonly ItemData Flashbang = new ItemData
            {
                id = "item_grenade_flash",
                name = "Flashbang",
                description = "Blinds and deafens enemies temporarily.",
                category = "Throwable",
                rarity = "Uncommon",
                stackSize = 4,
                weight = 0.3f,
                sellValue = 100,
                buffType = "Blind",
                buffDuration = 5f,
                radius = 10f
            };

            public static readonly ItemData SmokeGrenade = new ItemData
            {
                id = "item_grenade_smoke",
                name = "Smoke Grenade",
                description = "Creates smoke cover for escape or stealth.",
                category = "Throwable",
                rarity = "Common",
                stackSize = 4,
                weight = 0.35f,
                sellValue = 75,
                buffDuration = 15f,
                radius = 12f
            };

            public static readonly ItemData MolotovCocktail = new ItemData
            {
                id = "item_grenade_molotov",
                name = "Molotov Cocktail",
                description = "Incendiary weapon. Creates fire area.",
                category = "Throwable",
                rarity = "Common",
                stackSize = 4,
                weight = 0.5f,
                sellValue = 60,
                damage = 30f,
                buffType = "Fire",
                buffDuration = 10f,
                radius = 6f
            };
        }

        #endregion

        #region Perks - Complete List

        public static class PerkPresets
        {
            // COMBAT PERKS
            public static readonly PerkData QuickDraw = new PerkData
            {
                id = "perk_combat_quickdraw",
                name = "Quick Draw",
                description = "Swap weapons 30% faster.",
                category = "Combat",
                tier = "Basic",
                slotCost = 1,
                effectType = "WeaponSwapSpeed",
                effectValue = 0.30f
            };

            public static readonly PerkData SteadyAim = new PerkData
            {
                id = "perk_combat_steadyaim",
                name = "Steady Aim",
                description = "Reduced weapon sway when aiming.",
                category = "Combat",
                tier = "Basic",
                slotCost = 1,
                effectType = "AimStability",
                effectValue = 0.40f
            };

            public static readonly PerkData DeadEye = new PerkData
            {
                id = "perk_combat_deadeye",
                name = "Dead Eye",
                description = "Headshots deal 25% more damage.",
                category = "Combat",
                tier = "Advanced",
                slotCost = 2,
                effectType = "HeadshotDamage",
                effectValue = 0.25f
            };

            public static readonly PerkData SpeedLoader = new PerkData
            {
                id = "perk_combat_speedloader",
                name = "Speed Loader",
                description = "Reload weapons 25% faster.",
                category = "Combat",
                tier = "Basic",
                slotCost = 1,
                effectType = "ReloadSpeed",
                effectValue = 0.25f
            };

            public static readonly PerkData ExtendedMags = new PerkData
            {
                id = "perk_combat_extendedmags",
                name = "Extended Magazines",
                description = "All weapons have 30% larger magazines.",
                category = "Combat",
                tier = "Advanced",
                slotCost = 2,
                effectType = "MagazineSize",
                effectValue = 0.30f
            };

            public static readonly PerkData ArmorPiercing = new PerkData
            {
                id = "perk_combat_armorpiercing",
                name = "Armor Piercing",
                description = "Bullets penetrate armor 20% better.",
                category = "Combat",
                tier = "Expert",
                slotCost = 3,
                effectType = "ArmorPenetration",
                effectValue = 0.20f
            };

            // SURVIVAL PERKS
            public static readonly PerkData ThickSkin = new PerkData
            {
                id = "perk_survival_thickskin",
                name = "Thick Skin",
                description = "Take 15% less damage from all sources.",
                category = "Survival",
                tier = "Advanced",
                slotCost = 2,
                effectType = "DamageReduction",
                effectValue = 0.15f
            };

            public static readonly PerkData FastHealing = new PerkData
            {
                id = "perk_survival_fasthealing",
                name = "Fast Healing",
                description = "Medical items heal 30% more.",
                category = "Survival",
                tier = "Basic",
                slotCost = 1,
                effectType = "HealingReceived",
                effectValue = 0.30f
            };

            public static readonly PerkData SecondWind = new PerkData
            {
                id = "perk_survival_secondwind",
                name = "Second Wind",
                description = "When health drops below 25%, gain a burst of speed.",
                category = "Survival",
                tier = "Expert",
                slotCost = 2,
                effectType = "LowHealthSpeed",
                effectValue = 0.35f,
                condition = "HealthBelow25"
            };

            public static readonly PerkData LastStand = new PerkData
            {
                id = "perk_survival_laststand",
                name = "Last Stand",
                description = "When downed, use your pistol to defend yourself.",
                category = "Survival",
                tier = "Expert",
                slotCost = 3,
                effectType = "DownedCombat",
                effectValue = 1f
            };

            // MOVEMENT PERKS
            public static readonly PerkData Marathon = new PerkData
            {
                id = "perk_movement_marathon",
                name = "Marathon",
                description = "Sprint 20% faster and longer.",
                category = "Movement",
                tier = "Basic",
                slotCost = 1,
                effectType = "SprintSpeed",
                effectValue = 0.20f
            };

            public static readonly PerkData NinjaFeet = new PerkData
            {
                id = "perk_movement_ninjafeet",
                name = "Ninja Feet",
                description = "Make 50% less noise when moving.",
                category = "Movement",
                tier = "Advanced",
                slotCost = 2,
                effectType = "NoiseReduction",
                effectValue = 0.50f
            };

            public static readonly PerkData Parkour = new PerkData
            {
                id = "perk_movement_parkour",
                name = "Parkour",
                description = "Climb and vault obstacles 40% faster.",
                category = "Movement",
                tier = "Basic",
                slotCost = 1,
                effectType = "ClimbSpeed",
                effectValue = 0.40f
            };

            // ECONOMY PERKS
            public static readonly PerkData Scavenger = new PerkData
            {
                id = "perk_economy_scavenger",
                name = "Scavenger",
                description = "Find 20% more loot from containers.",
                category = "Economy",
                tier = "Basic",
                slotCost = 1,
                effectType = "LootChance",
                effectValue = 0.20f
            };

            public static readonly PerkData LuckyFind = new PerkData
            {
                id = "perk_economy_luckyfind",
                name = "Lucky Find",
                description = "Increased chance to find rare items.",
                category = "Economy",
                tier = "Advanced",
                slotCost = 2,
                effectType = "RareLootChance",
                effectValue = 0.15f
            };

            public static readonly PerkData PackMule = new PerkData
            {
                id = "perk_economy_packmule",
                name = "Pack Mule",
                description = "Carry 30% more weight.",
                category = "Economy",
                tier = "Basic",
                slotCost = 1,
                effectType = "CarryCapacity",
                effectValue = 0.30f
            };

            // SUPPORT PERKS
            public static readonly PerkData TeamMedic = new PerkData
            {
                id = "perk_support_teammedic",
                name = "Team Medic",
                description = "Revive teammates 40% faster.",
                category = "Support",
                tier = "Basic",
                slotCost = 1,
                effectType = "ReviveSpeed",
                effectValue = 0.40f
            };

            public static readonly PerkData SharedVision = new PerkData
            {
                id = "perk_support_sharedvision",
                name = "Shared Vision",
                description = "Spotted enemies are visible to all teammates.",
                category = "Support",
                tier = "Advanced",
                slotCost = 2,
                effectType = "TeamSpotting",
                effectValue = 1f
            };
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public struct WeaponData
        {
            public string id;
            public string name;
            public string description;
            public string type;
            public float damage;
            public float fireRate;
            public float range;
            public int magazineSize;
            public int maxAmmo;
            public float reloadTime;
            public float accuracy;
            public Vector2 recoil;
            public int pelletsPerShot;
            public int unlockLevel;
            public int cost;
            public string rarity;
        }

        [System.Serializable]
        public struct ZombieData
        {
            public string id;
            public string name;
            public string description;
            public string category;
            public float health;
            public float damage;
            public float moveSpeed;
            public float attackRange;
            public float attackCooldown;
            public float detectionRange;
            public float hearingRange;
            public int xpReward;
            public int currencyReward;
            public bool isBoss;
            public bool explodesOnDeath;
            public float explosionDamage;
            public float explosionRadius;
            public bool canCallHorde;
            public float callRange;
            public bool isRanged;
            public bool canCloak;
            public float armorValue;
            public bool canSpawnMinions;
        }

        [System.Serializable]
        public struct ItemData
        {
            public string id;
            public string name;
            public string description;
            public string category;
            public string rarity;
            public int stackSize;
            public float weight;
            public int sellValue;
            public float healAmount;
            public float healDuration;
            public string buffType;
            public float buffDuration;
            public float buffValue;
            public float damage;
            public float radius;
        }

        [System.Serializable]
        public struct PerkData
        {
            public string id;
            public string name;
            public string description;
            public string category;
            public string tier;
            public int slotCost;
            public string effectType;
            public float effectValue;
            public string condition;
        }

        #endregion
    }
}
