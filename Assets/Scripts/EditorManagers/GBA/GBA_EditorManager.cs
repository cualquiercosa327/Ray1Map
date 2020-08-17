﻿using R1Engine.Serialize;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace R1Engine
{
    /// <summary>
    /// The editor manager for GBA
    /// </summary>
    public class GBA_EditorManager : BaseEditorManager
    {
        public GBA_EditorManager(Unity_Level level, Context context, IDictionary<int, Unity_ObjGraphics> des, IDictionary<string, R1_EventState[][]> eta) : base(level, context, des.ToDictionary(x => x.Key.ToString(), x => x.Value), new ReadOnlyDictionary<string, R1_EventState[][]>(eta))
        { }

        /// <summary>
        /// Indicates if the local commands should be used
        /// </summary>
        protected override bool UsesLocalCommands => false;

        /// <summary>
        /// Gets the DES key for the specified event data item
        /// </summary>
        /// <param name="eventInfoData">The event info data item</param>
        /// <returns>The DES key</returns>
        public override string GetDesKey(GeneralEventInfoData eventInfoData) => throw new NotImplementedException();

        /// <summary>
        /// Gets the ETA key for the specified event data item
        /// </summary>
        /// <param name="eventInfoData">The event info data item</param>
        /// <returns>The ETA key</returns>
        public override string GetEtaKey(GeneralEventInfoData eventInfoData) => throw new NotImplementedException();

        /// <summary>
        /// Checks if the event is available in the current world
        /// </summary>
        /// <param name="eventInfoData">The event info data item</param>
        /// <returns>True if it's available, otherwise false</returns>
        public override bool IsAvailableInWorld(GeneralEventInfoData eventInfoData) => false;

        public override Enum GetCollisionTypeAsEnum(byte collisionType) => (GBA_TileCollisionType)collisionType;

        public override Unity_MapCollisionTypeGraphic GetCollisionTypeGraphic(byte collisionType) => ((GBA_TileCollisionType)collisionType).GetCollisionTypeGraphic();

        public override int PixelsPerUnit => 16;
        public override int CellSize => 8;
	}
}