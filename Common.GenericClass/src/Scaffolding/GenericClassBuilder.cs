﻿using DryIoc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ZKWeb.Core;
using ZKWeb.Model;
using ZKWeb.Model.ActionResults;
using ZKWeb.Plugins.Common.Admin.src;
using ZKWeb.Plugins.Common.Admin.src.Extensions;
using ZKWeb.Plugins.Common.Admin.src.Managers;
using ZKWeb.Plugins.Common.AdminSettings.src;
using ZKWeb.Plugins.Common.AdminSettings.src.Scaffolding;
using ZKWeb.Plugins.Common.Base.src;
using ZKWeb.Plugins.Common.Base.src.Extensions;
using ZKWeb.Plugins.Common.Base.src.HtmlBuilder;
using ZKWeb.Plugins.Common.Base.src.Model;
using ZKWeb.Plugins.Common.Base.src.Repositories;
using ZKWeb.Plugins.Common.GenericClass.src.Database;
using ZKWeb.Plugins.Common.GenericClass.src.Repositories;
using ZKWeb.Utils.Extensions;
using ZKWeb.Utils.Functions;

namespace ZKWeb.Plugins.Common.GenericClass.src.Scaffolding {
	/// <summary>
	/// 通用分类构建器
	/// 使用时需要继承，例子
	/// [ExportMany]
	/// public class ExampleClass : GenericClassBuilder {
	///		public override string Name { get { return "ExampleClass"; } }
	/// }
	/// </summary>
	public abstract class GenericClassBuilder :
		GenericListForAdminSettings<Database.GenericClass, GenericClassBuilder> {
		/// <summary>
		/// 分类类型，默认使用名称（除去空格）
		/// </summary>
		public virtual string Type { get { return Name.Replace(" ", ""); } }
		/// <summary>
		/// 使用的权限
		/// </summary>
		public override string Privilege { get { return "ClassManage:" + Type; } }
		/// <summary>
		/// 所属分组
		/// </summary>
		public override string Group { get { return "ClassManage"; } }
		/// <summary>
		/// 分组图标
		/// </summary>
		public override string GroupIcon { get { return "fa fa-list"; } }
		/// <summary>
		/// 图标的Css类
		/// </summary>
		public override string IconClass { get { return "fa fa-list"; } }
		/// <summary>
		/// 模板路径
		/// </summary>
		public override string TemplatePath { get { return "common.generic_class/class_list.html"; } }
		/// <summary>
		/// Url地址
		/// </summary>
		public override string Url { get { return "/admin/settings/generic_class/" + Type.ToLower(); } }
		/// <summary>
		/// 添加使用的Url地址
		/// </summary>
		public virtual string AddUrl { get { return Url + "/add"; } }
		/// <summary>
		/// 编辑使用的Url地址
		/// </summary>
		public virtual string EditUrl { get { return Url + "/edit"; } }
		/// <summary>
		/// 批量操作使用的Url地址
		/// </summary>
		public virtual string BatchUrl { get { return Url + "/batch"; } }

		/// <summary>
		/// 获取表格回调
		/// </summary>
		/// <returns></returns>
		protected override IAjaxTableCallback<Database.GenericClass> GetTableCallback() {
			return new TableCallback(this);
		}

		/// <summary>
		/// 添加分类
		/// </summary>
		/// <returns></returns>
		protected virtual IActionResult AddAction() {
			return EditAction();
		}

		/// <summary>
		/// 编辑分类
		/// </summary>
		/// <returns></returns>
		protected virtual IActionResult EditAction() {
			// 检查权限
			PrivilegesChecker.Check(AllowedUserTypes, RequiredPrivileges);
			// 处理表单绑定或提交
			var form = new Form(Type);
			var request = HttpContext.Current.Request;
			var attribute = ((IModelFormBuilder)form).GetFormAttribute();
			attribute.Action = attribute.Action ?? request.Url.PathAndQuery;
			if (request.HttpMethod == HttpMethods.POST) {
				return new JsonResult(form.Submit());
			} else {
				form.Bind();
				return new TemplateResult("common.admin/generic_edit.html", new { form });
			}
		}

		/// <summary>
		/// 批量操作标签
		/// 目前支持批量删除，恢复，永久删除
		/// </summary>
		/// <returns></returns>
		protected virtual IActionResult BatchAction() {
			// 检查权限
			PrivilegesChecker.Check(AllowedUserTypes, RequiredPrivileges);
			// 拒绝处理非ajax提交的请求，防止跨站攻击
			var request = HttpContext.Current.Request;
			if (!request.IsAjaxRequest()) {
				throw new HttpException(403, new T("Non ajax request batch action is not secure"));
			}
			// 获取参数
			// 其中Id列表需要把顺序倒转，用于先删除子分类再删除上级分类
			var actionName = request.GetParam<string>("action");
			var json = HttpContext.Current.Request.GetParam<string>("json");
			var idList = JsonConvert.DeserializeObject<IList<object>>(json).Reverse().ToList();
			// 检查是否所有Id都属于指定的类型，防止越权操作
			bool isAllClassesTypeMatched = false;
			UnitOfWork.ReadData<GenericClassRepository, Database.GenericClass>(repository => {
				isAllClassesTypeMatched = repository.IsAllClassesTypeEqualTo(idList, Type);
			});
			if (!isAllClassesTypeMatched) {
				throw new HttpException(403, new T("Try to access class that type not matched"));
			}
			// 执行批量操作
			string message = null;
			UnitOfWork.WriteData<Database.GenericClass>(repository => {
				if (actionName == "delete_forever") {
					repository.BatchDeleteForever(idList);
					message = new T("Batch Delete Forever Successful");
				} else if (actionName == "delete") {
					repository.BatchDelete(idList);
					message = new T("Batch Delete Successful");
				} else if (actionName == "recover") {
					repository.BatchRecover(idList);
					message = new T("Batch Recover Successful");
				} else {
					throw new HttpException(404, string.Format(new T("Action {0} not exist"), actionName));
				}
			});
			return new JsonResult(new { message });
		}

		/// <summary>
		/// 网站启动时添加处理函数
		/// </summary>
		public override void OnWebsiteStart() {
			base.OnWebsiteStart();
			var controllerManager = Application.Ioc.Resolve<ControllerManager>();
			controllerManager.RegisterAction(AddUrl, HttpMethods.GET, AddAction);
			controllerManager.RegisterAction(AddUrl, HttpMethods.POST, AddAction);
			controllerManager.RegisterAction(EditUrl, HttpMethods.GET, EditAction);
			controllerManager.RegisterAction(EditUrl, HttpMethods.POST, EditAction);
			controllerManager.RegisterAction(BatchUrl, HttpMethods.POST, BatchAction);
		}

		/// <summary>
		/// 表格回调
		/// </summary>
		public class TableCallback : IAjaxTableCallback<Database.GenericClass> {
			/// <summary>
			/// 分类构建器
			/// </summary>
			public GenericClassBuilder Builder { get; set; }

			/// <summary>
			/// 初始化
			/// </summary>
			public TableCallback(GenericClassBuilder builder) {
				Builder = builder;
			}

			/// <summary>
			/// 构建表格时的处理
			/// </summary>
			public void OnBuildTable(
				AjaxTableBuilder table, AjaxTableSearchBarBuilder searchBar) {
				table.MenuItems.AddToggleAllForAjaxTableTree("Level");
				table.MenuItems.AddDivider();
				table.MenuItems.AddEditAction(
					Builder.Type, Builder.EditUrl, dialogParameters: new { size = "size-wide" });
				table.MenuItems.AddAddAction(
					Builder.Type, Builder.AddUrl,
					name: new T("Add Top Level Class"), dialogParameters: new { size = "size-wide" });
				table.MenuItems.AddRemoteModalForSelectedRow(
					new T("Add Same Level Class"), "fa fa-plus",
					string.Format(new T("Add {0}"), new T(Builder.Type)),
					Builder.AddUrl + "?parentId=<%-row.ParentId%>", new { size = "size-wide" });
				table.MenuItems.AddRemoteModalForSelectedRow(
					new T("Add Child Class"), "fa fa-plus",
					string.Format(new T("Add {0}"), new T(Builder.Type)),
					Builder.AddUrl + "?parentId=<%-row.Id%>", new { size = "size-wide" });
				searchBar.KeywordPlaceHolder = "Name/Remark";
				searchBar.MenuItems.AddDivider();
				searchBar.MenuItems.AddRecycleBin();
				searchBar.MenuItems.AddAddAction(
					Builder.Type, Builder.AddUrl,
					name: new T("Add Top Level Class"), dialogParameters: new { size = "size-wide" });
			}

			/// <summary>
			/// 查询数据
			/// </summary>
			public void OnQuery(
				AjaxTableSearchRequest request, DatabaseContext context, ref IQueryable<Database.GenericClass> query) {
				// 提供类型给其他回调
				request.Conditions["Type"] = Builder.Type;
				// 按类型
				query = query.Where(q => q.Type == Builder.Type);
				// 按回收站
				query = query.FilterByRecycleBin(request);
				// 按关键词
				if (!string.IsNullOrEmpty(request.Keyword)) {
					query = query.Where(q => q.Name.Contains(request.Keyword) || q.Remark.Contains(request.Keyword));
				}
			}

			/// <summary>
			/// 排序数据
			/// </summary>
			public void OnSort(
				AjaxTableSearchRequest request, DatabaseContext context, ref IQueryable<Database.GenericClass> query) {
				// 默认按显示顺序排列
				query = query.OrderBy(q => q.DisplayOrder).ThenByDescending(q => q.Id);
			}

			/// <summary>
			/// 选择数据
			/// </summary>
			public void OnSelect(
				AjaxTableSearchRequest request, List<KeyValuePair<Database.GenericClass, Dictionary<string, object>>> pairs) {
				// 按上下级关系重新生成数据列表
				var classMapping = pairs.ToDictionary(p => p.Key.Id);
				var tree = TreeUtils.CreateTree(pairs,
					p => p, p => classMapping.GetOrDefault(p.Key.Parent == null ? 0 : p.Key.Parent.Id));
				pairs.Clear();
				foreach (var node in tree.EnumerateAllNodes().Skip(1)) {
					var pair = node.Value;
					pair.Value["Id"] = pair.Key.Id;
					pair.Value["Name"] = pair.Key.Name;
					pair.Value["ParentId"] = pair.Key.Parent == null ? 0 : pair.Key.Parent.Id;
					pair.Value["CreateTime"] = pair.Key.CreateTime.ToClientTimeString();
					pair.Value["DisplayOrder"] = pair.Key.DisplayOrder;
					pair.Value["Deleted"] = pair.Key.Deleted ? EnumDeleted.Deleted : EnumDeleted.None;
					pair.Value["Level"] = node.GetParents().Count() - 1;
					pair.Value["NoChilds"] = !node.Childs.Any();
					pairs.Add(pair);
				}
			}

			/// <summary>
			/// 添加列和操作
			/// </summary>
			public void OnResponse(
				AjaxTableSearchRequest request, AjaxTableSearchResponse response) {
				var idColumn = response.Columns.AddIdColumn("Id");
				response.Columns.AddTreeNodeColumn("Name", "Level", "NoChilds");
				response.Columns.AddMemberColumn("CreateTime");
				response.Columns.AddMemberColumn("DisplayOrder");
				response.Columns.AddEnumLabelColumn("Deleted", typeof(EnumDeleted));
				var actionColumn = response.Columns.AddActionColumn();
				actionColumn.AddEditAction(
					Builder.Type, Builder.EditUrl, dialogParameters: new { size = "size-wide" });
				idColumn.AddDivider();
				idColumn.AddDeleteActions(
					request, typeof(Database.GenericClass), Builder.Type, Builder.BatchUrl);
			}
		}

		/// <summary>
		/// 添加和编辑使用的表单
		/// </summary>
		public class Form : DataEditFormBuilder<Database.GenericClass, Form> {
			/// <summary>
			/// 分类类型
			/// </summary>
			public string Type { get; set; }
			/// <summary>
			/// 上级分类名称
			/// </summary>
			[LabelField("ParentClass")]
			public string ParentClass { get; set; }
			/// <summary>
			/// 名称
			/// </summary>
			[Required]
			[StringLength(100)]
			[TextBoxField("Name")]
			public string Name { get; set; }
			/// <summary>
			/// 显示顺序
			/// </summary>
			[Required]
			[TextBoxField("DisplayOrder", "Order from small to large")]
			public long DisplayOrder { get; set; }
			/// <summary>
			/// 备注
			/// </summary>
			[TextAreaField("Remark", 5, "Remark")]
			public string Remark { get; set; }

			/// <summary>
			/// 初始化
			/// </summary>
			/// <param name="type">分类类型</param>
			public Form(string type) {
				Type = type;
			}

			/// <summary>
			/// 根据当前请求传入的parentId参数获取上级分类，不存在时返回null
			/// 这个函数只在添加时使用
			/// </summary>
			protected Database.GenericClass GetParentClass(DatabaseContext context) {
				var parentId = HttpContext.Current.Request.GetParam<long>("parentId");
				if (parentId <= 0) {
					return null;
				}
				var parent = context.Get<Database.GenericClass>(c => c.Id == parentId);
				if (parent == null) {
					return null;
				} else if (parent.Type != Type) {
					throw new HttpException(403, new T("Try to access class that type not matched"));
				}
				return parent;
			}

			/// <summary>
			/// 从数据绑定表单
			/// </summary>
			protected override void OnBind(DatabaseContext context, Database.GenericClass bindFrom) {
				if (bindFrom.Id <= 0) {
					// 添加时
					var parent = GetParentClass(context);
					ParentClass = parent == null ? "" : parent.Name;
				} else {
					// 编辑时
					ParentClass = bindFrom.Parent == null ? "" : bindFrom.Parent.Name;
					// 检查类型，防止越权操作
					if (bindFrom.Type != Type) {
						throw new HttpException(403, new T("Try to access class that type not matched"));
					}
				}
				Name = bindFrom.Name;
				DisplayOrder = bindFrom.DisplayOrder;
				Remark = bindFrom.Remark;
			}

			/// <summary>
			/// 保存表单到数据
			/// </summary>
			protected override object OnSubmit(DatabaseContext context, Database.GenericClass saveTo) {
				if (saveTo.Id <= 0) {
					// 添加时
					saveTo.Type = Type;
					saveTo.Parent = GetParentClass(context);
					saveTo.CreateTime = DateTime.UtcNow;
				} else if (saveTo.Type != Type) {
					// 编辑时检查类型，防止越权操作
					throw new HttpException(403, new T("Try to access class that type not matched"));
				}
				saveTo.Name = Name;
				saveTo.DisplayOrder = DisplayOrder;
				saveTo.Remark = Remark;
				return new {
					message = new T("Saved Successfully"),
					script = ScriptStrings.AjaxtableUpdatedAndCloseModal
				};
			}
		}
	}
}