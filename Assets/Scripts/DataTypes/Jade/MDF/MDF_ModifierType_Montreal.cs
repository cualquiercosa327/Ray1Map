﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R1Engine.Jade {
	// TODO: Use similar system to AI_Links - this array is probably different for other games
	public enum MDF_ModifierType_Montreal : int {
		None = -1,
		GEO_ModifierSnap = 0,
		GEO_ModifierOnduleTonCorps = 1,
		GAO_ModifierExplode = 2,
		GAO_ModifierLegLink = 3,
		GEO_ModifierMorphing = 4,
		GAO_ModifierSemiLookAt = 5,
		GAO_ModifierShadow = 6,
		GAO_ModifierSpecialLookAt = 7,
		GEN_ModifierSound = 8,
		GAO_ModifierXMEN = 9,
		GAO_ModifierXMEC = 10,
		SPG_Modifier = 11,
		GEO_ModifierSymetrie = 12,
		GAO_ModifierROTR = 13,
		GAO_ModifierSNAKE = 14,
		DARE_ModifierSound = 15,
		GEN_ModifierSoundFx = 16,
		PROTEX_Modifier = 17,
		GAO_ModifierSoftBody = 18,
		GAO_ModifierSpring = 19,
		GAO_ModifierRope = 20,
		WATER3D_Modifier = 21,
		GAO_ModifierRotC = 22,
		GAO_ModifierBeamGen = 23,
		Disturber_Modifier = 24,
		GAO_ModifierAnimIK = 25,
		GAO_ModifierAnimatedScale = 26,
		GAO_ModifierWind = 27,
		GAO_ModifierSfx = 28,
		GEO_ModifierBridge = 29,
		GEO_ModifierMeshToParticles = 30,
		GAO_ModifierVirtualAnim = 31,
		Halo_Modifier = 32,
		GAO_ModifierTree = 33,
		GAO_ModifierPlant = 34,
		GAO_ModifierVoiceManager = 35,

		// Introduced after PoP:SoT, found in NCIS
		GAO_ModifierSectorisationElement = 36,
		GAO_ModifierAnimatedMaterial = 37,
		GAO_ModifierRotationPaste = 38,
		Ambiance_Modifier = 39,
		AmbianceLinker_Modifier = 40,
		AmbiancePocket_Modifier = 41,
		GAO_ModifierPendula = 42,
		GAO_ModifierTranslationPaste = 43,
		GAO_ModifierAnimatedPAG = 44,
		GAO_ModifierAnimatedGAO = 45,
		GAO_ModifierEyeTrail = 46, // Found in Spree
		GAO_ModifierCharacterFX = 47,

		GAO_ModifierMotionBlur = 51,
		GAO_ModifierAlphaFade = 52,
		GAO_ModifierAlphaOccluder = 53,

		SPG2_Modifier = 55,
		FakeHDR_Modifier = 56,
		DARE_ModifierSoundVolumetric = 57,
		ProjectiveShadowProjector_Modifier = 58,
		ProjectiveShadowCaster_Modifier = 59,
		MUSIC_CONTAINER_ModifierSound = 60,

		Reflector_Modifier = 62,
		Fur_Modifier = 63,
		Outline_Modifier = 64,
		FaceFx_Modifier = 65,
	}
}
