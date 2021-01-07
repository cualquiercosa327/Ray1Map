﻿using System.Text;

namespace R1Engine
{
    public class GBA_Milan_LocTable : R1Serializable
    {
        public Pointer[] Pointers { get; set; }
        public string[] Strings { get; set; }

        public override void SerializeImpl(SerializerObject s)
        {
            var manager = ((GBA_Milan_Manager)s.GameSettings.GetGameManager);

            Pointers = s.SerializePointerArray(Pointers, manager.Milan_LocTableLength * manager.Milan_LocTableLangCount, name: nameof(Pointers));

            if (Strings == null)
                Strings = new string[Pointers.Length];

            for (int i = 0; i < Strings.Length; i++)
                Strings[i] = s.DoAt(Pointers[i], () => s.SerializeString(Strings[i], encoding: Encoding.GetEncoding(1252), name: $"{nameof(Strings)}[{i}]"));
        }
    }
}