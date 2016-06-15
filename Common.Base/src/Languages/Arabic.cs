﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZKWeb.Localize;
using ZKWebStandard.Ioc;

namespace ZKWeb.Plugins.Common.Base.src.Languages {
	/// <summary>
	/// 阿拉伯语
	/// </summary>
	[ExportMany]
	public class Arabic : ILanguage {
		public string Name { get { return "ar-DZ"; } }
	}
}
