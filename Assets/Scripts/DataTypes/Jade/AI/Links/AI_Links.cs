﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R1Engine.Jade {
	public abstract class AI_Links {

		public AI_FunctionDef[] FunctionDefs { get; protected set; }

		public AI_Link[] Categories { get; protected set; }
		public AI_Link[] Types { get; protected set; }
		public AI_Link[] Keywords { get; protected set; }
		public AI_Link[] Functions { get; protected set; }
		public AI_Link[] Fields { get; protected set; }

		public Dictionary<uint, AI_Link> Links { get; protected set; }
		public Dictionary<uint, AI_FunctionDef> CompiledFunctions { get; protected set; }
		private void CreateDictionaries() {
			Links = new Dictionary<uint, AI_Link>();
			foreach (var l in Categories) Links[l.Key] = l;
			foreach (var l in Types) Links[l.Key] = l;
			foreach (var l in Keywords) Links[l.Key] = l;
			foreach (var l in Functions) Links[l.Key] = l;
			foreach (var l in Fields) Links[l.Key] = l;
			CompiledFunctions = new Dictionary<uint, AI_FunctionDef>();
			foreach(var l in FunctionDefs) CompiledFunctions[l.Key] = l;
		}

		protected void Init() {
			InitFunctionDefs();
			InitCategories();
			InitTypes();
			InitKeywords();
			InitFunctions();
			InitFields();
		}
		protected abstract void InitFunctionDefs();
		protected abstract void InitCategories();
		protected abstract void InitTypes();
		protected abstract void InitKeywords();
		protected abstract void InitFunctions();
		protected abstract void InitFields();


		public static AI_Links GetAILinks(GameSettings settings) {
			AI_Links links = null;
			switch (settings.GameModeSelection) {
				case GameModeSelection.RaymanRavingRabbidsPC:
				case GameModeSelection.RaymanRavingRabbidsPCDemo:
				case GameModeSelection.RaymanRavingRabbidsPS2:
				case GameModeSelection.RaymanRavingRabbidsWii:
				case GameModeSelection.RaymanRavingRabbidsWiiJP:
					links = new AI_Links_RRR_Wii();
					break;
				case GameModeSelection.RaymanRavingRabbidsXbox360:
					links = new AI_Links_RRR_Xbox360();
					break;
				case GameModeSelection.BeyondGoodAndEvilGC:
					links = new AI_Links_BGE_GC();
					break;
				case GameModeSelection.BeyondGoodAndEvilPC:
				case GameModeSelection.BeyondGoodAndEvilXbox:
				case GameModeSelection.BeyondGoodAndEvilPS2:
					links = new AI_Links_BGE_PC();
					break;
				case GameModeSelection.BeyondGoodAndEvilXbox360:
					links = new AI_Links_BGE_HD_Xbox360();
					break;
				case GameModeSelection.BeyondGoodAndEvilPS3:
					links = new AI_Links_BGE_HD_PS3();
					break;
				case GameModeSelection.BeyondGoodAndEvilPCDemo:
					links = new AI_Links_BGE_PCDemo();
					break;
				case GameModeSelection.BeyondGoodAndEvilPS2_20030814:
					links = new AI_Links_BGE_PS2_20030814();
					break;
				case GameModeSelection.BeyondGoodAndEvilPS2_20030805:
					links = new AI_Links_BGE_PS2_20030805();
					break;
				case GameModeSelection.KingKongPCGamersEdition:
					links = new AI_Links_KingKong_PCGamersEdition();
					break;
				case GameModeSelection.KingKongXbox360:
					links = new AI_Links_KingKong_Xbox360();
					break;
				case GameModeSelection.KingKongPC:
					links = new AI_Links_KingKong_PC();
					break;
				case GameModeSelection.KingKongGC:
				case GameModeSelection.KingKongPS2:
				case GameModeSelection.KingKongXbox:
					links = new AI_Links_KingKong_GC();
					break;
				case GameModeSelection.KingKongPSP:
					links = new AI_Links_KingKong_PSP();
					break;
				case GameModeSelection.RaymanRavingRabbids2Wii:
				case GameModeSelection.RaymanRavingRabbids2PC:
					links = new AI_Links_RRR2_Wii();
					break;
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimePS2_20030819:
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimePS2_20030723:
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimeXbox_20030723:
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimePS2:
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimeGC:
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimePC:
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimePCLimitedDemo:
				case GameModeSelection.PrinceOfPersiaTheSandsOfTimeXbox:
				case GameModeSelection.PrinceOfPersiaWarriorWithinPC:
					links = new AI_Links_PoP_SoT_PS2_Proto();
					break;
				case GameModeSelection.HorsezPS2:
					links = new AI_Links_Horsez_PS2();
					break;
				case GameModeSelection.Horsez2Wii:
					links = new AI_Links_Horsez2_Wii();
					break;
			}
			if (links != null) {
				links.Init();
				links.CreateDictionaries();
			}
			return links;
		}
	}
}
