﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Elastic.Configuration.EnvironmentBased.Java
{
	public class JavaConfiguration 
	{
		private readonly IFileSystem _fileSystem;
		public static JavaConfiguration Default { get; } = new JavaConfiguration(new JavaEnvironmentStateProvider(), new FileSystem());

		private readonly IJavaEnvironmentStateProvider _stateProvider;

		public JavaConfiguration(IJavaEnvironmentStateProvider stateProvider, IFileSystem fileSystem)
		{
			_stateProvider = stateProvider ?? new JavaEnvironmentStateProvider();
			_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		}

		public string JavaExecutable
		{
			get
			{
				try
				{
					return Path.Combine(this.JavaHomeCanonical, @"bin", "java.exe");
				}
				catch (Exception e)
				{
					
					throw new Exception(
						$"There was a problem constructing a path from the detected java home directory '{this.JavaHomeCanonical}'", e);
				}
			}
		}

		public bool JavaExecutableExists => this._fileSystem.File.Exists(this.JavaExecutable);
		
		public string JavaHomeCanonical => JavaHomeCandidates.FirstOrDefault(j=>!string.IsNullOrWhiteSpace(j));
		
		private List<string> JavaHomeCandidates => new List<string> {
			_stateProvider.JavaHomeProcessVariable,
			_stateProvider.JavaHomeUserVariable,
			_stateProvider.JavaHomeMachineVariable,
			_stateProvider.JdkRegistry64,
			_stateProvider.JreRegistry64,
			_stateProvider.JdkRegistry32,
			_stateProvider.JreRegistry32
		};

		public bool JavaInstalled => !string.IsNullOrEmpty(this.JavaHomeCanonical);

		public bool Using32BitJava => JavaHomeCandidates.FindIndex(c => !string.IsNullOrWhiteSpace(c)) >= JavaHomeCandidates.Count - 2;

		public bool JavaMisconfigured 
		{
			get
			{
				if (!JavaInstalled) return false;
				if (string.IsNullOrEmpty(_stateProvider.JavaHomeMachineVariable) && string.IsNullOrWhiteSpace(_stateProvider.JavaHomeUserVariable)) return false;
				if (string.IsNullOrWhiteSpace(_stateProvider.JavaHomeUserVariable)) return false;
				if (string.IsNullOrWhiteSpace(_stateProvider.JavaHomeMachineVariable)) return false;
				return _stateProvider.JavaHomeMachineVariable != _stateProvider.JavaHomeUserVariable;
			}
		}

		public bool ReadJavaVersionInformation(out string version, out bool? is64Bit)
		{
			version = null;
			is64Bit = null;
			if (!this.JavaExecutableExists || !this._stateProvider.ReadJavaVersionInformation(this.JavaHomeCanonical, out var consoleOut))
				return false;
			is64Bit = false;
			foreach (var line in consoleOut)
			{
				if (line.IndexOf("64-bit", StringComparison.OrdinalIgnoreCase) >= 0) is64Bit = true;
				if (line.StartsWith("java version"))
					version = line.Replace("java version ", "").Trim('"').Trim();
			}
			return true;
		}
			
		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"Java paths")
				.AppendLine($"- current = {JavaExecutable}")
				.AppendLine($"- exists = {JavaExecutableExists}")
				.AppendLine($"Java Candidates (in order of precedence)")
				.AppendLine($"- {nameof(_stateProvider.JavaHomeProcessVariable)} = {_stateProvider.JavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_stateProvider.JavaHomeUserVariable)} = {_stateProvider.JavaHomeUserVariable}")
				.AppendLine($"- {nameof(_stateProvider.JavaHomeMachineVariable)} = {_stateProvider.JavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_stateProvider.JdkRegistry64)} = {_stateProvider.JdkRegistry64}")
				.AppendLine($"- {nameof(_stateProvider.JreRegistry64)} = {_stateProvider.JreRegistry64}")
				.AppendLine($"- {nameof(_stateProvider.JdkRegistry32)} = {_stateProvider.JdkRegistry32}")
				.AppendLine($"- {nameof(_stateProvider.JreRegistry32)} = {_stateProvider.JreRegistry32}")
				.AppendLine($"Java checks")
				.AppendLine($"- {nameof(Using32BitJava)} = {Using32BitJava}")
				.AppendLine($"- JAVA_HOME as machine and user variable = {JavaMisconfigured}")
				.ToString();

	}
}
