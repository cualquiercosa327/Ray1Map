﻿namespace R1Engine
{
    // TODO: Merge with PC_AnimationDescriptor
    /// <summary>
    /// Animation descriptor data for Rayman 1 (PS1)
    /// </summary>
    public class PS1_R1_AnimationDescriptor : R1Serializable
    {
        /// <summary>
        /// Pointer to the animation layers
        /// </summary>
        public Pointer AnimLayersPointer { get; set; }
        
        /// <summary>
        /// Pointer to the animation frames
        /// </summary>
        public Pointer AnimFramesPointer { get; set; }

        /// <summary>
        /// The number of layers to use per frame
        /// </summary>
        public ushort LayersPerFrame { get; set; }

        public byte Unk1 { get; set; }

        /// <summary>
        /// The number of frames in the animation
        /// </summary>
        public ushort FrameCount { get; set; }

        public byte Unk2 { get; set; }

        /// <summary>
        /// The animation layers
        /// </summary>
        public Common_AnimationLayer[] Layers { get; set; }

        /// <summary>
        /// The animation frames
        /// </summary>
        public Common_AnimationFrame[] Frames { get; set; }

        /// <summary>
        /// Handles the data serialization
        /// </summary>
        /// <param name="s">The serializer object</param>
        public override void SerializeImpl(SerializerObject s) 
        {
            // Serialize pointers
            AnimLayersPointer = s.SerializePointer(AnimLayersPointer, name: nameof(AnimLayersPointer));
            AnimFramesPointer = s.SerializePointer(AnimFramesPointer, name: nameof(AnimFramesPointer));
            
            // Serialize data
            if (s.GameSettings.EngineVersion == EngineVersion.RayPS1JPDemoVol3)
            {
                LayersPerFrame = s.Serialize<byte>((byte)LayersPerFrame, name: nameof(LayersPerFrame));
                Unk1 = s.Serialize<byte>(Unk1, name: nameof(Unk1));
                FrameCount = s.Serialize<byte>((byte)FrameCount, name: nameof(FrameCount));
                Unk2 = s.Serialize<byte>(Unk2, name: nameof(Unk2));
            }
            else
            {
                LayersPerFrame = s.Serialize<ushort>(LayersPerFrame, name: nameof(LayersPerFrame));
                FrameCount = s.Serialize<ushort>(FrameCount, name: nameof(FrameCount));
            }

            // Serialize data from pointers
            s.DoAt(AnimLayersPointer, () =>
            {
                Layers = s.SerializeObjectArray(Layers, LayersPerFrame * FrameCount, name: nameof(Layers));
            });
            s.DoAt(AnimFramesPointer, () =>
            {
                Frames = s.SerializeObjectArray(Frames, s.GameSettings.EngineVersion == EngineVersion.RayPS1JPDemoVol3 ? FrameCount : FrameCount + 1, name: nameof(Frames));
            });
        }
    }
}