﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZKWeb.Plugins.Common.Currency.src.Model;
using ZKWebStandard.Ioc;

namespace ZKWeb.Plugins.Common.Currency.src.Currencies {
	/// <summary>
	/// 韩元
	/// </summary>
	[ExportMany]
	public class KRW : ICurrency {
		public string Type { get { return "KRW"; } }
		public string Prefix { get { return "₩"; } }
		public string Suffix { get { return null; } }
	}
}
