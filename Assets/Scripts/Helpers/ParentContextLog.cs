﻿using BinarySerializer;
using System;

namespace Ray1Map
{
	public class ParentContextLog : ISerializerLog {
		public ISerializerLog ParentLog { get; set; }

		public bool IsEnabled => ParentLog.IsEnabled;

		public string OverrideLogPath { get => ParentLog.OverrideLogPath; set => ParentLog.OverrideLogPath = value; }

		public void Log(object obj) => ParentLog.Log(obj);

		public void Dispose() { }

		public ParentContextLog(ISerializerLog parent) {
			ParentLog = parent;
		}
	}
}