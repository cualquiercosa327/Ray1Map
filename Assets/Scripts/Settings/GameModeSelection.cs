﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace R1Engine
{
    /// <summary>
    /// The available game modes to select from
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameModeSelection
    {
        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PS1, Game.R1_Rayman1, "Rayman 1 (PS1 - US)", typeof(R1_PS1_Manager))]
        RaymanPS1US,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PS1, Game.R1_Rayman1, "Rayman 1 (PS1 - EU)", typeof(R1_PS1_Manager))]
        RaymanPS1EU,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PS1, Game.R1_Rayman1, "Rayman 1 (PS1 - EU Demo)", typeof(R1_PS1EUDemo_Manager))]
        RaymanPS1EUDemo,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PS1_JP, Game.R1_Rayman1, "Rayman 1 (PS1 - JP)", typeof(R1_PS1JP_Manager))]
        RaymanPS1Japan,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PS1_JPDemoVol3, Game.R1_Rayman1, "Rayman 1 (PS1 - JP Demo Vol3)", typeof(R1_PS1JPDemoVol3_Manager))]
        RaymanPS1DemoVol3Japan,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PS1_JPDemoVol6, Game.R1_Rayman1, "Rayman 1 (PS1 - JP Demo Vol6)", typeof(R1_PS1JPDemoVol6_Manager))]
        RaymanPS1DemoVol6Japan,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_Saturn, Game.R1_Rayman1, "Rayman 1 (Saturn - US)", typeof(R1_Satun_Manager))]
        RaymanSaturnUS,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_Saturn, Game.R1_Rayman1, "Rayman 1 (Saturn - Prototype)", typeof(R1_Satun_Manager))]
        RaymanSaturnProto,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_Saturn, Game.R1_Rayman1, "Rayman 1 (Saturn - EU)", typeof(R1_Satun_Manager))]
        RaymanSaturnEU,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_Saturn, Game.R1_Rayman1, "Rayman 1 (Saturn - JP)", typeof(R1_Satun_Manager))]
        RaymanSaturnJP,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC, Game.R1_Rayman1, "Rayman 1 (PC - 1.00)", typeof(R1_PC_Manager))]
        RaymanPC_1_00,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC, Game.R1_Rayman1, "Rayman 1 (PC - 1.10)", typeof(R1_PC_Manager))]
        RaymanPC_1_10,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC, Game.R1_Rayman1, "Rayman 1 (PC - 1.12)", typeof(R1_PC_Manager))]
        RaymanPC_1_12,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC, Game.R1_Rayman1, "Rayman 1 (PC - 1.20)", typeof(R1_PC_Manager))]
        RaymanPC_1_20,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC, Game.R1_Rayman1, "Rayman 1 (PC - 1.21 JP)", typeof(R1_PC_Manager))]
        RaymanPC_1_21_JP,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC, Game.R1_Rayman1, "Rayman 1 (PC - 1.21)", typeof(R1_PC_Manager))]
        RaymanPC_1_21,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC_Kit, Game.R1_Designer, "Rayman Gold (PC - Demo)", typeof(R1_Kit_Manager))]
        RaymanGoldPCDemo,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC_Kit, Game.R1_Designer, "Rayman Designer (PC)", typeof(R1_Kit_Manager))]
        RaymanDesignerPC,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC_Kit, Game.R1_Mapper, "Rayman Mapper (PC)", typeof(R1_Mapper_Manager))]
        MapperPC,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC_Kit, Game.R1_ByHisFans, "Rayman by his Fans (PC)", typeof(R1_Kit_Manager))]
        RaymanByHisFansPC,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC_Kit, Game.R1_60Levels, "Rayman 60 Levels (PC)", typeof(R1_Kit_Manager))]
        Rayman60LevelsPC,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC_Edu, Game.R1_Educational, "Rayman Educational (PC)", typeof(R1_PCEdu_Manager))]
        RaymanEducationalPC,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PS1_Edu, Game.R1_Educational, "Rayman Educational (PS1)", typeof(R1_PS1Edu_Manager))]
        RaymanEducationalPS1,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PC_Edu, Game.R1_Quiz, "Rayman Quiz (PC)", typeof(R1_PCEdu_Manager))]
        RaymanQuizPC,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PocketPC, Game.R1_Rayman1, "Rayman Ultimate (Pocket PC)", typeof(R1_PocketPC_Manager))]
        RaymanPocketPC,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_PocketPC, Game.R1_Rayman1, "Rayman Classic (Mobile)", typeof(R1_Mobile_Manager))]
        RaymanClassicMobile,

        [GameMode(MajorEngineVersion.Rayman1_Jaguar, EngineVersion.R1Jaguar, Game.R1_Rayman1, "Rayman 1 (Jaguar)", typeof(R1Jaguar_Manager))]
        RaymanJaguar,

        [GameMode(MajorEngineVersion.Rayman1_Jaguar, EngineVersion.R1Jaguar_Proto, Game.R1_Rayman1, "Rayman 1 (Jaguar - Prototype)", typeof(R1Jaguar_Proto_Manager))]
        RaymanJaguarPrototype,

        [GameMode(MajorEngineVersion.Rayman1_Jaguar, EngineVersion.R1Jaguar_Demo, Game.R1_Rayman1, "Rayman 1 (Jaguar - Demo)", typeof(R1Jaguar_Demo_Manager))]
        RaymanJaguarDemo,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_GBA, Game.R1_Rayman1, "Rayman Advance (GBA - EU)", typeof(R1_GBA_Manager))]
        RaymanAdvanceGBAEU,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_GBA, Game.R1_Rayman1, "Rayman Advance (GBA - US)", typeof(R1_GBA_Manager))]
        RaymanAdvanceGBAUS,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_GBA, Game.R1_Rayman1, "Rayman Advance (GBA - EU Beta)", typeof(R1_GBA_Manager))]
        RaymanAdvanceGBAEUBeta,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R1_DSi, Game.R1_Rayman1, "Rayman 1 (DSi)", typeof(R1_DSi_Manager))]
        RaymanDSi,

        [GameMode(MajorEngineVersion.Rayman1, EngineVersion.R2_PS1, Game.R1_Rayman2, "Rayman 2 (PS1 - Demo)", typeof(R1_PS1R2_Manager))]
        Rayman2PS1Demo,

        [GameMode(MajorEngineVersion.SNES, EngineVersion.SNES, Game.SNES_Prototype, "Rayman (SNES - Prototype)", typeof(SNES_Prototype_Manager))]
        RaymanSNES,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_R3, Game.GBA_Rayman3, "Rayman 3 (GBA - EU)", typeof(GBA_R3_Manager))]
        Rayman3GBAEU,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_R3, Game.GBA_Rayman3, "Rayman 3 (GBA - US)", typeof(GBA_R3_Manager))]
        Rayman3GBAUS,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_R3, Game.GBA_Rayman3, "Rayman 3 (GBA - EU Beta)", typeof(GBA_R3_Manager))]
        Rayman3GBAEUBeta,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_R3_Proto, Game.GBA_Rayman3, "Rayman 3 (GBA - US Prototype)", typeof(GBA_R3Proto_Manager))]
        Rayman3GBAUSPrototype,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_R3, Game.GBA_Rayman3, "Rayman 3 (N-Gage)", typeof(GBA_R3NGage_Manager))]
        Rayman3NGage,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_R3, Game.GBA_Rayman3, "Rayman Raving Rabbids (GBA)", typeof(GBA_RRR_Manager))]
        RaymanRavingRabbidsGBA,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_PrinceOfPersia, Game.GBA_PrinceOfPersiaTheSandsOfTime, "Prince of Persia: The Sands of Time (GBA - EU)", typeof(GBA_PoPSoT_Manager))]
        PrinceOfPersiaGBAEU,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_PrinceOfPersia, Game.GBA_PrinceOfPersiaTheSandsOfTime, "Prince of Persia: The Sands of Time (GBA - US)", typeof(GBA_PoPSoT_Manager))]
        PrinceOfPersiaGBAUS,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_Sabrina, Game.GBA_SabrinaTheTeenageWitchPotionCommotion, "Sabrina - The Teenage Witch - Potion Commotion (GBA - EU)", typeof(GBA_Sabrina_Manager))]
        SabrinaTheTeenageWitchPotionCommotionGBAEU,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_Sabrina, Game.GBA_SabrinaTheTeenageWitchPotionCommotion, "Sabrina - The Teenage Witch - Potion Commotion (GBA - US)", typeof(GBA_Sabrina_Manager))]
        SabrinaTheTeenageWitchPotionCommotionGBAUS,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_StarWars, Game.GBA_StarWarsTrilogyApprenticeOfTheForce, "Star Wars Trilogy - Apprentice of the Force (GBA - EU)", typeof(GBA_StarWarsApprentice_Manager))]
        StarWarsTrilogyApprenticeOfTheForceGBAEU,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_StarWars, Game.GBA_StarWarsTrilogyApprenticeOfTheForce, "Star Wars Trilogy - Apprentice of the Force (GBA - US)", typeof(GBA_StarWarsApprentice_Manager))]
        StarWarsTrilogyApprenticeOfTheForceGBAUS,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_BatmanVengeance, Game.GBA_BatmanVengeance, "Batman Vengeance (GBA - EU)", typeof(GBA_BatmanVengeance_Manager))]
        BatmanVengeanceGBAEU,

        [GameMode(MajorEngineVersion.GBA, EngineVersion.GBA_BatmanVengeance, Game.GBA_BatmanVengeance, "Batman Vengeance (GBA - US)", typeof(GBA_BatmanVengeance_Manager))]
        BatmanVengeanceGBAUS,
    }
}