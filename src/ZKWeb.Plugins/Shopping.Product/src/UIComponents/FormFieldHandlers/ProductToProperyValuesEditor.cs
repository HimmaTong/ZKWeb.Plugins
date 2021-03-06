﻿using Newtonsoft.Json;
using System.Collections.Generic;
using ZKWeb.Localize;
using ZKWeb.Plugins.Common.Base.src.UIComponents.Forms;
using ZKWeb.Plugins.Common.Base.src.UIComponents.Forms.Interfaces;
using ZKWeb.Plugins.Shopping.Product.src.Domain.Structs;
using ZKWeb.Plugins.Shopping.Product.src.UIComponents.FormFieldAttributes;
using ZKWeb.Templating;
using ZKWebStandard.Ioc;

namespace ZKWeb.Plugins.Shopping.Product.src.UIComponents.FormFieldHandlers {
	/// <summary>
	/// 商品关联的属性值的编辑器
	/// 编辑商品时使用
	/// </summary>
	[ExportMany(ContractKey = typeof(ProductToProperyValuesEditorAttribute)), SingletonReuse]
	public class ProductToProperyValuesEditor : IFormFieldHandler {
		/// <summary>
		/// 获取表单字段的html
		/// </summary>
		public string Build(FormField field, IDictionary<string, string> htmlAttributes) {
			var attribute = (ProductToProperyValuesEditorAttribute)field.Attribute;
			var templateManager = Application.Ioc.Resolve<TemplateManager>();
			var translations = new Dictionary<string, string>() {
				{ "Sure to change category? The properties you selected will lost!",
					new T("Sure to change category? The properties you selected will lost!") }
			};
			return templateManager.RenderTemplate(
				"shopping.product/tmpl.form.product_to_property_values_editor.html", new {
					name = attribute.Name,
					value = JsonConvert.SerializeObject(field.Value),
					attributes = htmlAttributes,
					categoryFieldName = attribute.CategoryFieldName,
					translations = JsonConvert.SerializeObject(translations)
				});
		}

		/// <summary>
		/// 解析提交的字段的值
		/// </summary>
		public object Parse(FormField field, IList<string> values) {
			return JsonConvert.DeserializeObject<List<ProductToPropertyValueForEdit>>(values[0]) ??
				new List<ProductToPropertyValueForEdit>();
		}
	}
}
