﻿using System;
using BinarySerializer;

namespace Ray1Map.Jade {
	// Found in GEO_p_CreateFromBuffer
	public class GEO_GeometricObject : GRO_GraphicRenderObject {
		public uint Code_00 { get; set; }
		public uint Version { get; set; }
		public uint VerticesCount { get; set; }
		public int HasMRM { get; set; }
		public int HasReorderBuffer { get; set; }
		public uint ColorsCount { get; set; }
		public uint UVsCount { get; set; }
		public uint ElementsCount { get; set; }
		public uint EditorFlags { get; set; }
		public uint MRM_ObjectAdditionalInfoPointer { get; set; }

		public uint Code_01 { get; set; }
		public GEO_GeometricObject_Ponderation ObjectPonderation { get; set; }
		public COL_OK3 OK3_Boxes { get; set; }
		// (other stuff here)
		public Jade_Vector[] Vertices { get; set; }
		public Jade_Vector[] Normals { get; set; }
		public GEO_GeometricObject_MRM_ObjectAdditionalInfo MRM_ObjectAdditionalInfo { get; set; }
		public Jade_Color[] Colors { get; set; }
		public UV[] UVs { get; set; }
		public GEO_GeometricObjectElement[] Elements { get; set; }
		public uint StripFlag { get; set; }

		public GEO_GeometricObject_MRM_Levels MRM_Levels { get; set; }

		public uint SpritesElementsCount { get; set; }
		public uint Version2_EndCode { get; set; }

		// Montreal
		public GEO_ObjFlags Flags { get; set; }
		public GEO_ObjFlags_PoPSoT Flags_SoT { get; set; }
		public uint Montreal_Editor_UInt_0 { get; set; }
		public uint Montreal_Editor_UInt_1 { get; set; }
		public uint Montreal_Editor_UInt_2 { get; set; }
		public uint Montreal_Editor_UInt_3 { get; set; }
		public uint Montreal_Editor_UInt_4 { get; set; }
		public uint Montreal_Editor_UInt_5 { get; set; }
		public uint Montreal_Flags2 { get; set; }
		public int Montreal_HasColors { get; set; }
		public int Montreal_HasNormals { get; set; } // Boolean

		// CPP
		public GEO_CPP_VertexBuffer CPP_VertexBuffer { get; set; }
		public ushort CPP_StitchMatricesCount { get; set; }
		public GEO_CPP_StitchMatrix[] CPP_StitchMatrices { get; set; }

		public bool Montreal_IsOptimized(GameSettings s) {
			switch (s.Platform) {
				case Platform.PS2:
					return (Montreal_Flags2 & 1) == 1;
				case Platform.GC:
					return (Montreal_Flags2 & 2) == 2;
				case Platform.Xbox:
				case Platform.PS3:
					return (Montreal_Flags2 & 4) == 4;
				case Platform.PC:
					if (s.EngineVersionTree.HasParent(EngineVersion.Jade_PoP_WW_20040920)) {
						return (Montreal_Flags2 & 8) == 8;
					} else {
						return (Montreal_Flags2 & 4) == 4;
					}
				case Platform.iOS:
					return (Montreal_Flags2 & 0x10) == 0x10;
				case Platform.PSP:
					return (Montreal_Flags2 & 0x20) == 0x20;
				case Platform.Wii:
					if (s.EngineVersionTree.HasParent(EngineVersion.Jade_TMNT)) {
						return (Montreal_Flags2 & 0x40) == 0x40;
					} else {
						return (Montreal_Flags2 & 2) == 2;
					}
				default:
					return false;
			}
		}
		public bool Montreal_HasUnoptimizedData(GameSettings s) {
			LOA_Loader Loader = Context.GetStoredObject<LOA_Loader>(Jade_BaseManager.LoaderKey);
			return (!(Montreal_IsOptimized(s) && Loader.IsBinaryData))
				|| ((s.Platform == Platform.GC || s.Platform == Platform.PC || s.Platform == Platform.iOS || s.Platform == Platform.Wii))
				|| (s.Platform == Platform.Xbox && (s.EngineVersion == EngineVersion.Jade_PoP_SoT_20030723))
				|| (s.Platform == Platform.PS3 && s.EngineVersion == EngineVersion.Jade_PoP_SoT);
		}
		public bool Montreal_FilledUnoptimizedData { get; set; } = false;
		public bool Montreal_WasOptimized { get; set; } = false;

		public Jade_Key OptimizedGeoObjectKey_PS2 { get; set; }
		public Jade_Key OptimizedGeoObjectKey_GC { get; set; }
		public Jade_Key OptimizedGeoObjectKey_PC { get; set; }
		public uint Montreal_Editor_UInt_9 { get; set; }
		public GEO_GeoObject_PS2 OptimizedGeoObject_PS2 { get; set; }
		public GEO_GeoObject_GC OptimizedGeoObject_GC { get; set; }
		public GEO_GeoObject_PC OptimizedGeoObject_PC { get; set; }

		public override void SerializeImpl(SerializerObject s) {
			LOA_Loader Loader = Context.GetStoredObject<LOA_Loader>(Jade_BaseManager.LoaderKey);

			if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_Montreal)) {
				if (ObjectVersion >= 4) {
					if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_PoP_WW_20040920)) {
						Flags = s.Serialize<GEO_ObjFlags>(Flags, name: nameof(Flags));
					} else {
						Flags_SoT = s.Serialize<GEO_ObjFlags_PoPSoT>(Flags_SoT, name: nameof(Flags_SoT));
					}
				}
				if (ObjectVersion == 6 && !Loader.IsBinaryData) {
					Montreal_Editor_UInt_0 = s.Serialize<uint>(Montreal_Editor_UInt_0, name: nameof(Montreal_Editor_UInt_0));
					Montreal_Editor_UInt_1 = s.Serialize<uint>(Montreal_Editor_UInt_1, name: nameof(Montreal_Editor_UInt_1));
					Montreal_Editor_UInt_2 = s.Serialize<uint>(Montreal_Editor_UInt_2, name: nameof(Montreal_Editor_UInt_2));
					Montreal_Editor_UInt_3 = s.Serialize<uint>(Montreal_Editor_UInt_3, name: nameof(Montreal_Editor_UInt_3));
					Montreal_Editor_UInt_4 = s.Serialize<uint>(Montreal_Editor_UInt_4, name: nameof(Montreal_Editor_UInt_4));
					Montreal_Editor_UInt_5 = s.Serialize<uint>(Montreal_Editor_UInt_5, name: nameof(Montreal_Editor_UInt_5));
				}
				if (ObjectVersion >= 7) Montreal_Flags2 = s.Serialize<uint>(Montreal_Flags2, name: nameof(Montreal_Flags2));
				if (!s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_CPP)) {
					if (Montreal_HasUnoptimizedData(s.GetR1Settings())
						|| (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_PoP_T2T) && s.GetR1Settings().Platform == Platform.Xbox)) {
						VerticesCount = s.Serialize<uint>(VerticesCount, name: nameof(VerticesCount));
						Code_00 = VerticesCount;
					}
				}
			} else {
				Code_00 = s.Serialize<uint>(Code_00, name: nameof(Code_00));
				if (Code_00 == (uint)Jade_Code.Code2002) {
					Version = s.Serialize<uint>(Version, name: nameof(Version));
					VerticesCount = s.Serialize<uint>(VerticesCount, name: nameof(VerticesCount));
					HasMRM = s.Serialize<int>(HasMRM, name: nameof(HasMRM));
					if (HasMRM != 0) HasReorderBuffer = s.Serialize<int>(HasReorderBuffer, name: nameof(HasReorderBuffer));
				} else {
					VerticesCount = Code_00;
					Version = 0;
				}
			}
			if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_Montpellier) || Montreal_HasUnoptimizedData(s.GetR1Settings())) {
				Montreal_FilledUnoptimizedData = true;
				if (ObjectVersion >= 15 && s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_CPP)) {
					CPP_VertexBuffer = s.SerializeObject<GEO_CPP_VertexBuffer>(CPP_VertexBuffer, name: nameof(CPP_VertexBuffer));
					ElementsCount = s.Serialize<uint>(ElementsCount, name: nameof(ElementsCount));
					Code_01 = s.Serialize<uint>(Code_01, name: nameof(Code_01));
					if ((Code_01 & (uint)Jade_Code.Code2002) == (uint)Jade_Code.Code2002) {
						ObjectPonderation = s.SerializeObject<GEO_GeometricObject_Ponderation>(ObjectPonderation, name: nameof(ObjectPonderation));
					}
					Elements = s.SerializeObjectArray<GEO_GeometricObjectElement>(Elements, ElementsCount, onPreSerialize: e => e.GeometricObject = this, name: nameof(Elements));
					if (ObjectVersion >= 16 && ObjectPonderation != null) {
						CPP_StitchMatricesCount = s.Serialize<ushort>(CPP_StitchMatricesCount, name: nameof(CPP_StitchMatricesCount));
						CPP_StitchMatrices = s.SerializeObjectArray<GEO_CPP_StitchMatrix>(CPP_StitchMatrices, CPP_StitchMatricesCount, name: nameof(CPP_StitchMatrices));
						foreach(var e in Elements) e?.SerializeStitchBuckets(s);
					}
				} else {
					ColorsCount = s.Serialize<uint>(ColorsCount, name: nameof(ColorsCount));
					if (ObjectVersion >= 3) Montreal_HasColors = s.Serialize<int>(Montreal_HasColors, name: nameof(Montreal_HasColors));
					UVsCount = s.Serialize<uint>(UVsCount, name: nameof(UVsCount));
					ElementsCount = s.Serialize<uint>(ElementsCount, name: nameof(ElementsCount));
					if (!Loader.IsBinaryData) EditorFlags = s.Serialize<uint>(EditorFlags, name: nameof(EditorFlags));
					if (ObjectVersion < 2) MRM_ObjectAdditionalInfoPointer = s.Serialize<uint>(MRM_ObjectAdditionalInfoPointer, name: nameof(MRM_ObjectAdditionalInfoPointer));
					Code_01 = s.Serialize<uint>(Code_01, name: nameof(Code_01));
					if ((Code_01 & (uint)Jade_Code.Code2002) == (uint)Jade_Code.Code2002) {
						ObjectPonderation = s.SerializeObject<GEO_GeometricObject_Ponderation>(ObjectPonderation, name: nameof(ObjectPonderation));
					}
					if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_Montreal)) {
						if (ObjectVersion != 0) Montreal_HasNormals = s.Serialize<int>(Montreal_HasNormals, name: nameof(Montreal_HasNormals));
					} else {
						if ((Code_01 & 1) != 0) {
							OK3_Boxes = s.SerializeObject<COL_OK3>(OK3_Boxes, name: nameof(OK3_Boxes));
						}
					}
					Vertices = s.SerializeObjectArray<Jade_Vector>(Vertices, VerticesCount, name: nameof(Vertices));
					if (!Loader.IsBinaryData || s.GetR1Settings().EngineFlags.HasFlag(EngineFlags.Jade_Xenon)
						|| (Montreal_HasNormals != 0 && ObjectVersion >= 3)) {
						if (Normals == null && s is BinarySerializer.BinarySerializer) {
							ComputeNormals();
						}
						Normals = s.SerializeObjectArray<Jade_Vector>(Normals, VerticesCount, name: nameof(Normals));
					}
					if (MRM_ObjectAdditionalInfoPointer != 0) {
						MRM_ObjectAdditionalInfo = s.SerializeObject<GEO_GeometricObject_MRM_ObjectAdditionalInfo>(MRM_ObjectAdditionalInfo, onPreSerialize: m => {
							m.GeometricObject = this;
						}, name: nameof(MRM_ObjectAdditionalInfo));
					}
					if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_Montpellier)
						|| !Loader.IsBinaryData || Montreal_HasColors != 0) {
						Colors = s.SerializeObjectArray<Jade_Color>(Colors, ColorsCount, name: nameof(Colors));
					}
					UVs = s.SerializeObjectArray<UV>(UVs, UVsCount, name: nameof(UVs));

					// Serialize elements
					Elements = s.SerializeObjectArray<GEO_GeometricObjectElement>(Elements, ElementsCount, onPreSerialize: e => e.GeometricObject = this, name: nameof(Elements));
					foreach (var el in Elements) {
						el.SerializeArrays(s);
					}
				}

				StripFlag = s.Serialize<uint>(StripFlag, name: nameof(StripFlag));
				if ((StripFlag & 1) != 0) {
					foreach (var el in Elements) {
						el.SerializeStripData(s);
					}
				}

				// Serialize MRM
				if (HasMRM != 0) {
					MRM_Levels = s.SerializeObject<GEO_GeometricObject_MRM_Levels>(MRM_Levels, onPreSerialize: m => {
						m.GeometricObject = this;
					}, name: nameof(MRM_Levels));
				}

				SpritesElementsCount = s.Serialize<uint>(SpritesElementsCount, name: nameof(SpritesElementsCount));
				if (SpritesElementsCount != 0 && BitHelpers.ExtractBits((int)SpritesElementsCount, 24, 8) == 0) {
					throw new NotImplementedException($"TODO: Implement {GetType()}: SpritesElements");
				}
				if (Version >= 2) Version2_EndCode = s.Serialize<uint>(Version2_EndCode, name: nameof(Version2_EndCode));
			} else {
				Montreal_WasOptimized = true;
			}
			if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_Montreal) && ObjectVersion >= 7 && !Loader.IsBinaryData) {
				OptimizedGeoObjectKey_PS2 = s.SerializeObject<Jade_Key>(OptimizedGeoObjectKey_PS2, name: nameof(OptimizedGeoObjectKey_PS2));
				OptimizedGeoObjectKey_GC = s.SerializeObject<Jade_Key>(OptimizedGeoObjectKey_GC, name: nameof(OptimizedGeoObjectKey_GC));
				OptimizedGeoObjectKey_PC = s.SerializeObject<Jade_Key>(OptimizedGeoObjectKey_PC, name: nameof(OptimizedGeoObjectKey_PC));
				Montreal_Editor_UInt_9 = s.Serialize<uint>(Montreal_Editor_UInt_9, name: nameof(Montreal_Editor_UInt_9));
			}
			if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_Montreal) && Montreal_IsOptimized(s.GetR1Settings())) {
				switch (s.GetR1Settings().Platform) {
					case Platform.PS2:
					case Platform.PSP:
						OptimizedGeoObject_PS2 = s.SerializeObject<GEO_GeoObject_PS2>(OptimizedGeoObject_PS2, name: nameof(OptimizedGeoObject_PS2));
						break;
					case Platform.GC:
					case Platform.Wii:
						OptimizedGeoObject_GC = s.SerializeObject<GEO_GeoObject_GC>(OptimizedGeoObject_GC, onPreSerialize: opt => opt.GeometricObject = this, name: nameof(OptimizedGeoObject_GC));
						break;
					case Platform.PC:
					case Platform.iOS:
						OptimizedGeoObject_PC = s.SerializeObject<GEO_GeoObject_PC>(OptimizedGeoObject_PC, onPreSerialize: opt => opt.GeometricObject = this, name: nameof(OptimizedGeoObject_PC));
						break;
					case Platform.Xbox:
					case Platform.PS3:
						if (!Montreal_HasUnoptimizedData(s.GetR1Settings())) {
							Code_01 = s.Serialize<uint>(Code_01, name: nameof(Code_01));
							if ((Code_01 & (uint)Jade_Code.Code2002) == (uint)Jade_Code.Code2002) {
								ObjectPonderation = s.SerializeObject<GEO_GeometricObject_Ponderation>(ObjectPonderation, name: nameof(ObjectPonderation));
							}
						}
						OptimizedGeoObject_PC = s.SerializeObject<GEO_GeoObject_PC>(OptimizedGeoObject_PC, onPreSerialize: opt => opt.GeometricObject = this, name: nameof(OptimizedGeoObject_PC));
						break;
					default:
						s.SystemLogger?.LogWarning($"{GetType()}: Skipping unimplemented platform {s.GetR1Settings().Platform}. In case of errors, check this");
						break;
				}
			}
		}

		public void ComputeNormals() {
			Normals = new Jade_Vector[VerticesCount];
			for(int i = 0; i < Normals.Length; i++)
				Normals[i] = new Jade_Vector(0,0,0);

			foreach (var el in Elements) {
				foreach (var tri in el.Triangles) {
					var vec1 = Vertices[tri.Vertex0] - Vertices[tri.Vertex1];
					var vec2 = Vertices[tri.Vertex0] - Vertices[tri.Vertex2];
					var triNormal = Jade_Vector.CrossProduct(vec1, vec2).Normalize();

					Normals[tri.Vertex0] += triNormal;
					Normals[tri.Vertex1] += triNormal;
					Normals[tri.Vertex2] += triNormal;
				}
			}

			for (int i = 0; i < Normals.Length; i++) {
				if((Vertices[i].X * Vertices[i].X) < 0.001f)
					Normals[i].X = 0f;

				Normals[i] = Normals[i].Normalize();
			}

			// Editor only
			ComputeNormals_OBBox_Correction();
		}

		void ComputeNormals_OBBox_Correction() {
			var max = new Jade_Vector(-1000000, -1000000, -1000000);
			var min = new Jade_Vector(1000000, 1000000, 1000000);
			foreach (var vert in Vertices) {
				if (vert.X > max.X) max.X = vert.X;
				if (vert.X < min.X) min.X = vert.X;
				if (vert.Y > max.Y) max.Y = vert.Y;
				if (vert.Y < min.Y) min.Y = vert.Y;
				if (vert.Z > max.Z) max.Z = vert.Z;
				if (vert.Z < min.Z) min.Z = vert.Z;
			}

			max *= 2f;
			min *= 2f;

			if (max.X - min.X < 9.95f) return;
			if (max.Y - min.Y < 9.95f) return;

			const int epsilon_int = 30;
			var modulo = ((uint)((max.X - min.X) * 100)) % 1000;
			if(modulo < 1000 - epsilon_int && modulo > epsilon_int) return;

			    modulo = ((uint)((max.Y - min.Y) * 100)) % 1000;
			if (modulo < 1000 - epsilon_int && modulo > epsilon_int) return;

			max /= 2f;
			min /= 2f;

			var computedNormals = new Jade_Vector[VerticesCount];
			for (int i = 0; i < computedNormals.Length; i++)
				computedNormals[i] = new Jade_Vector(0, 0, 0);


			foreach (var el in Elements) {
				foreach (var tri in el.Triangles) {
					bool IsTouchBorder(Jade_Vector tst, Func<Jade_Vector, float> vfunc, out uint index1, out uint index2) {
						var xOrY = vfunc(tst);
						const float epsilon = (0.30f * 0.30f);
						index1 = 0;
						index2 = 0;
						Jade_Vector tmp = new Jade_Vector(
							MathF.Pow(vfunc(Vertices[tri.Vertex0]) - xOrY, 2),
							MathF.Pow(vfunc(Vertices[tri.Vertex1]) - xOrY, 2),
							MathF.Pow(vfunc(Vertices[tri.Vertex2]) - xOrY, 2)
							);

						if (tmp.X < epsilon && tmp.Y < epsilon) {
							index1 = tri.Vertex0;
							index2 = tri.Vertex1;
							return true;
						}
						if (tmp.Y < epsilon && tmp.Z < epsilon) {
							index1 = tri.Vertex1;
							index2 = tri.Vertex2;
							return true;
						}
						if (tmp.Z < epsilon && tmp.X < epsilon) {
							index1 = tri.Vertex2;
							index2 = tri.Vertex0;
							return true;
						}
						return false;
					}

					uint a, b;

					if (IsTouchBorder(max, v => v.X, out a, out b)) {
						computedNormals[a].Y -= Vertices[b].Z - Vertices[a].Z;
						computedNormals[b].Y -= Vertices[b].Z - Vertices[a].Z;
						computedNormals[a].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
						computedNormals[b].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
					}
					if (IsTouchBorder(min, v => v.X, out a, out b)) {
						computedNormals[a].Y += Vertices[b].Z - Vertices[a].Z;
						computedNormals[b].Y += Vertices[b].Z - Vertices[a].Z;
						computedNormals[a].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
						computedNormals[b].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
					}
					if (IsTouchBorder(max, v => v.Y, out a, out b)) {
						computedNormals[a].X += Vertices[b].Z - Vertices[a].Z;
						computedNormals[b].X += Vertices[b].Z - Vertices[a].Z;
						computedNormals[a].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
						computedNormals[b].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
					}
					if (IsTouchBorder(min, v => v.Y, out a, out b)) {
						computedNormals[a].X -= Vertices[b].Z - Vertices[a].Z;
						computedNormals[b].X -= Vertices[b].Z - Vertices[a].Z;
						computedNormals[a].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
						computedNormals[b].Z += Jade_Vector.Distance(Vertices[b], Vertices[a]);
					}
				}
			}


			for (int i = 0; i < computedNormals.Length; i++) {
				if (computedNormals[i].Z != 0) {
					Normals[i] = computedNormals[i].Normalize();
				}
			}
		}

		public class UV : BinarySerializable, IEquatable<UV>, ISerializerShortLog {
			public float U { get; set; }
			public float V { get; set; }
			
			public UV() { }
			public UV(float u, float v) {
				U = u;
				V = v;
			}

			public override void SerializeImpl(SerializerObject s) {
				U = s.Serialize<float>(U, name: nameof(U));
				V = s.Serialize<float>(V, name: nameof(V));
			}

			public string ShortLog => ToString();
			public override string ToString() => $"UV({U}, {V})";

			#region Equality

			public override bool Equals(object other) {
				if (other is UV cba)
					return Equals(cba);
				else
					return false;
			}

			public bool Equals(UV other) {
				if (other == null)
					return false;
				if (other.U != U || other.V != V)
					return false;
				return true;
			}

			public override int GetHashCode() => (U, V).GetHashCode();

			public static bool operator ==(UV term1, UV term2) {
				if ((object)term1 == null)
					return (object)term2 == null;

				return term1.Equals(term2);
			}

			public static bool operator !=(UV term1, UV term2) => !(term1 == term2);

			#endregion
		}

		[Flags]
		public enum GEO_ObjFlags : uint {
			None = 0,
			UseNormalsInEngine = 1 << 0,
			UseVertexPaintInEngine = 1 << 1,
			StaticMesh = 1 << 2,
			SkinnedMesh = 1 << 3,
			IndexedMesh = 1 << 4,
			ForceStatic = 1 << 5,
			ForceIndexed = 1 << 6,
			PositionIsStatic = 1 << 7,
			MaterialIsStatic = 1 << 8,
			ForceDynamicPosition = 1 << 9,
			ForceDynamicMaterial = 1 << 10,
			UseCompressedVertexInfo = 1 << 11,
			CanBeInstanciated = 1 << 12,
			ForceCanBeInstanciated = 1 << 13,
			ForceNoInstanciation = 1 << 14,
			CanHaveLightmaps = 1 << 15,
			ForceLightmaps = 1 << 16,
			CanUseChrome = 1 << 17,
			CanHaveNormalMaps = 1 << 18,
			OnlyInstanceBecauseOfAnims = 1 << 19,
			Full3DClipping = 1 << 20,
			Index8Bits = 1 << 21,
			HasLightMap = 1 << 22,
			HasShadow = 1 << 23,
			KeepDuplicatedVertex = 1 << 24,
			AllowPerPixelLighting = 1 << 25,
			Vtx16Bits = 1 << 26,
			NCIS_RenderAOOnWii_TFS_CanBeDisplaced = 1 << 27,
			TFS_ForceLoadTextures = 1 << 28,
			Unused29 = 1 << 29,
			Unused30 = 1 << 30,
			Unused31 = (uint)1 << 31
		}


		[Flags]
		public enum GEO_ObjFlags_PoPSoT : uint {
			None = 0,
			UseNormalsInEngine = 1 << 0,
			UseVertexPaintInEngine = 1 << 1,
			StaticMesh = 1 << 2,
			SkinnedMesh = 1 << 3,
			IndexedMesh = 1 << 4,
			ForceStatic = 1 << 5,
			ForceIndexed = 1 << 6,
			PositionIsStatic = 1 << 7,
			MaterialIsStatic = 1 << 8,
			ForceDynamicPosition = 1 << 9,
			ForceDynamicMaterial = 1 << 10,
			UseCompressedVertexInfo = 1 << 11,
			CanBeInstanciated = 1 << 12,
			ForceCanBeInstanciated = 1 << 13,
			ForceNoInstanciation = 1 << 14,
			CanHaveLightmaps = 1 << 15,
			ForceLightmaps = 1 << 16,
			CanUseChrome = 1 << 17,
			CanHaveNormalMaps = 1 << 18,
			OnlyInstanceBecauseOfAnims = 1 << 19,
			//Full3DClipping = 1 << 20, // Not sure if this one was missing in SoT, but it's before Index8Bits
			Index8Bits = 1 << 20,
			HasLightMap = 1 << 21,
			HasShadow = 1 << 22,
			KeepDuplicatedVertex = 1 << 23,
			AllowPerPixelLighting = 1 << 24,
			Vtx16Bits = 1 << 25,
			NCIS_RenderAOOnWii_TFS_CanBeDisplaced = 1 << 26,
			TFS_ForceLoadTextures = 1 << 27,
			Unused28 = 1 << 28,
			Unused29 = 1 << 29,
			Unused30 = (uint)1 << 30,
			Unused31 = (uint)1 << 31
		}
	}
}
