﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZKWeb.Localize;
using ZKWebStandard.Ioc;

namespace ZKWeb.Plugins.Common.Base.src.Languages {
	/// <summary>
	/// 波兰语
	/// </summary>
	[ExportMany]
	public class Polish : ILanguage {
		public string Name { get { return "pl-PL"; } }
	}
}
