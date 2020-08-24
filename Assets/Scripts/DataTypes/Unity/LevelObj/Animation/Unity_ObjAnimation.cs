﻿namespace R1Engine
{
    /// <summary>
    /// Common sprite animation data
    /// </summary>
    public class Unity_ObjAnimation
    {
        /// <summary>
        /// The animation frames
        /// </summary>
        public Unity_ObjAnimationFrame[] Frames { get; set; }

        public byte? AnimSpeed { get; set; }
    }
}