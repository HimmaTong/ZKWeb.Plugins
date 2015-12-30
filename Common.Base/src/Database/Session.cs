﻿using DryIocAttributes;
using FluentNHibernate;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ZKWeb.Model;
using ZKWeb.Utils.Functions;
using ZKWeb.Core;
using Newtonsoft.Json;

namespace ZKWeb.Plugins.Common.Base.src.Database {
	/// <summary>
	/// 会话
	/// </summary>
	public class Session {
		/// <summary>
		/// 会话Id
		/// </summary>
		public virtual string Id { get; set; }
		/// <summary>
		/// 关联Id
		/// 一般是用户Id
		/// </summary>
		public virtual long ReleatedId { get; set; }
		/// <summary>
		/// 会话数据的内容
		/// </summary>
		public virtual string ItemsJson { get; set; }
		/// <summary>
		/// 会话对应的Ip地址
		/// </summary>
		public virtual string IpAddress { get; set; }
		/// <summary>
		/// 是否记住登录
		/// </summary>
		public virtual bool RememberLogin { get; set; }
		/// <summary>
		/// 过期时间
		/// </summary>
		public virtual DateTime Expires { get; set; }

		/// <summary>
		/// 过期时间是否已更新
		/// 这个值只用于检测是否应该把新的过期时间发送到客户端
		/// </summary>
		public virtual bool ExpiresUpdated { get; set; }
		/// <summary>
		/// 会话数据
		/// 通过ItemsJson保存到数据库中
		/// </summary>
		private Dictionary<string, object> _Items;
		public virtual Dictionary<string, object> Items {
			get {
				// 初次获取时从ItemsJson反序列化
				if (_Items == null) {
					_Items = JsonConvert.DeserializeObject<Dictionary<string, object>>(ItemsJson);
				}
				return _Items;
			}
		}
	}

	/// <summary>
	/// 会话的扩展函数
	/// </summary>
	public static class SessionExtensions {
		/// <summary>
		/// 重新生成Id
		/// </summary>
		public static void ReGenerateId(this Session session) {
			session.Id = HttpServerUtility.UrlTokenEncode(RandomUtils.SystemRandomBytes(20));
		}

		/// <summary>
		/// 设置会话最少在指定的时间后过期
		/// 当前会话的过期时间比指定的时间要晚时不更新当前的过期时间
		/// </summary>
		/// <param name="span">最少在这个时间后过期</param>
		public static void SetExpiresAtLeast(this Session session, TimeSpan span) {
			var expires = DateTime.UtcNow + span;
			if (session.Expires < expires) {
				session.Expires = expires;
				session.ExpiresUpdated = true;
			}
		}
	}

	/// <summary>
	/// 会话的数据库结构
	/// </summary>
	[ExportMany]
	public class SessionMap : ClassMap<Session> {
		/// <summary>
		/// 初始化
		/// </summary>
		public SessionMap() {
			Id(s => s.Id);
			Map(s => s.ReleatedId);
			Map(s => s.ItemsJson).Length(0xffff);
			Map(s => s.IpAddress);
			Map(s => s.RememberLogin);
			Map(s => s.Expires).Index("Idx_Expires");
		}
	}

	/// <summary>
	/// 会话的数据库回调
	/// </summary>
	[ExportMany]
	public class SessionCallback : IDataSaveCallback<Session> {
		public void BeforeSave(DatabaseContext context, Session data) {
			// 添加或更新到数据库前，序列化会话数据
			if (data.Items != null) {
				data.ItemsJson = JsonConvert.SerializeObject(data.Items);
			}
		}

		public void AfterSave(DatabaseContext context, Session data) {
		}
	}
}