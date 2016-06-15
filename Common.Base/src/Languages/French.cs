﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZKWeb.Localize;
using ZKWebStandard.Ioc;

namespace ZKWeb.Plugins.Common.Base.src.Languages {
	/// <summary>
	/// 法语
	/// </summary>
	[ExportMany]
	public class French : ILanguage {
		public string Name { get { return "fr-FR"; } }
	}
}
