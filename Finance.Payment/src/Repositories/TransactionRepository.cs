﻿using DryIoc;
using DryIocAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ZKWeb.Localize;
using ZKWeb.Logging;
using ZKWeb.Plugins.Common.Admin.src.Database;
using ZKWeb.Plugins.Common.Base.src.Repositories;
using ZKWeb.Plugins.Common.GenericRecord.src.Database;
using ZKWeb.Plugins.Common.GenericRecord.src.Repositories;
using ZKWeb.Plugins.Common.SerialGenerate.src.Generator;
using ZKWeb.Plugins.Finance.Payment.src.Database;
using ZKWeb.Plugins.Finance.Payment.src.Model;
using ZKWeb.Utils.Extensions;

namespace ZKWeb.Plugins.Finance.Payment.src.Repositories {
	/// <summary>
	/// 支付交易的数据仓储
	/// </summary>
	[ExportMany]
	public class TransactionRepository : GenericRepository<PaymentTransaction> {
		/// <summary>
		/// 创建交易
		/// </summary>
		/// <param name="transactionType">交易类型</param>
		/// <param name="paymentApiId">支付接口Id</param>
		/// <param name="amount">交易金额</param>
		/// <param name="currencyType">货币类型</param>
		/// <param name="payerId">付款人Id</param>
		/// <param name="payeeId">收款人Id</param>
		/// <param name="releatedId">关联对象Id</param>
		/// <param name="description">描述</param>
		/// <param name="extraData">附加数据</param>
		/// <returns></returns>
		public virtual PaymentTransaction CreateTransaction(
			string transactionType, long paymentApiId,
			decimal amount, string currencyType, long? payerId, long? payeeId,
			long? releatedId, string description, object extraData = null) {
			// 检查参数
			var handlers = Application.Ioc.ResolveMany<IPaymentTransactionHandler>()
				.Where(h => h.Type == transactionType).ToList();
			if (!handlers.Any()) {
				throw new HttpException(400, string.Format(new T("Unknown transaction type {0}"), transactionType));
			} else if (amount <= 0) {
				throw new HttpException(400, "Transaction amount must large than zero");
			} else if (string.IsNullOrEmpty(description)) {
				throw new HttpException(400, "Transaction description is required");
			}
			// 检查接口是否可以使用
			var api = Context.Get<PaymentApi>(a => a.Id == paymentApiId);
			if (api == null) {
				throw new HttpException(400, new T("Payment api not exist"));
			} else if (!api.SupportTransactionTypes.Contains(transactionType)) {
				throw new HttpException(400, new T("Selected payment api not support this transaction"));
			} else if (api.Deleted) {
				throw new HttpException(400, new T("Selected payment api is deleted"));
			}
			// 保存交易到数据库
			var transaction = new PaymentTransaction() {
				Type = transactionType,
				Api = api,
				Amount = amount,
				CurrencyType = currencyType,
				Payer = Context.Get<User>(u => u.Id == payerId),
				Payee = Context.Get<User>(u => u.Id == payeeId),
				ReleatedId = releatedId,
				Description = description,
				State = PaymentTransactionState.Initial,
				ExtraData = extraData.ConvertOrDefault<Dictionary<string, object>>(),
				CreateTime = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow
			};
			transaction.Serial = SerialGenerator.GenerateFor(transaction);
			Save(ref transaction);
			// 调用创建交易后的处理
			handlers.ForEach(h => h.OnCreated(Context, transaction));
			// 记录结果到数据库
			AddDetailRecord(transaction.Id, null, new T("Transaction Created"));
			return transaction;
		}

		/// <summary>
		/// 交易明细记录类型
		/// </summary>
		public const string RecordType = "PaymentTransactionDetail";

		/// <summary>
		/// 添加交易明细记录
		/// </summary>
		/// <param name="transactionId">交易Id</param>
		/// <param name="creatorId">创建人Id</param>
		/// <param name="content">内容</param>
		/// <param name="extraData">附加数据</param>
		public void AddDetailRecord(
			long transactionId, long? creatorId, string content, object extraData = null) {
			var recordRepository = RepositoryResolver.Resolve<GenericRecordRepository, GenericRecord>(Context);
			recordRepository.AddRecord(RecordType, transactionId, creatorId, content, null, extraData);
		}

		/// <summary>
		/// 获取指定交易的所有明细记录
		/// </summary>
		/// <param name="transactionId">交易Id</param>
		/// <returns></returns>
		public List<GenericRecord> GetDetailRecords(long transactionId) {
			var recordRepository = RepositoryResolver.Resolve<GenericRecordRepository, GenericRecord>(Context);
			return recordRepository.FindRecords(RecordType, transactionId).ToList();
		}

		/// <summary>
		/// 设置交易最后发生的错误
		/// </summary>
		/// <param name="transactionId">交易Id</param>
		/// <param name="lastError">最后发生的错误</param>
		public void SetLastError(long transactionId, string lastError) {
			// 更新交易
			var transaction = GetById(transactionId);
			if (transaction == null) {
				throw new HttpException(400, new T("Payment transaction not found"));
			}
			Save(ref transaction, t => t.LastError = lastError);
			// 记录错误到日志
			var logManager = Application.Ioc.Resolve<LogManager>();
			var message = string.Format(
				new T("Payment transaction {0} error: {1}"), transaction.Serial, lastError);
			logManager.LogTransaction(message);
			// 记录错误到数据库
			AddDetailRecord(transactionId, null, message, null);
		}

		/// <summary>
		/// 尝试把交易切换到指定的交易状态
		/// </summary>
		/// <param name="transactionId">交易Id</param>
		/// <param name="externalSerial">外部交易流水号</param>
		/// <param name="state">交易状态</param>
		public void Process(long transactionId, string externalSerial, PaymentTransactionState state) {

		}
	}
}