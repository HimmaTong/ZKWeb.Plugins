﻿/*
	商品列表页使用的功能
	商品列表页已通过静态表格和set-url-param实现了过滤商品和分页功能
	
	这个脚本只需要实现以下功能
	- 标记已选择的条件，再次点击时可以取消这个条件
	- 支持按价格过滤
	- 遇到商品搜索框提交时，合并关键字到当前的条件中而不是直接提交
*/

// 标记已选择的条件，再次点击时可以取消这个条件
$(function () {
	var $filters = $(".product-list-filter-row [data-trigger='set-url-param']");
	var uri = new Uri(location.href);
	$filters.each(function () {
		var $filter = $(this);
		var key = $filter.data("key");
		var value = $filter.data("value");
		if (value && uri.getQueryParamValue(key) == value) {
			$filter.attr("data-value", "").addClass("selected").append("<i class='fa fa-remove'></i>");
		}
	});
});

// 支持按价格过滤
$(function () {
	var $filterNav = $(".product-filter-by-price-and-order");
	var $priceLowerBound = $filterNav.find(".price-lower-bound");
	var $priceUpperBound = $filterNav.find(".price-upper-bound");
	var $priceFilter = $filterNav.find(".price-filter");
	// 根据url中的参数设置价格范围的值
	var uri = new Uri(location.href);
	var priceRangeParam = uri.getQueryParamValue("price_range");
	var priceRange = (priceRangeParam || "0~0").split('~');
	$priceLowerBound.val(priceRange[0] || 0);
	$priceUpperBound.val(priceRange[1] || 0);
	// 有指定价格过滤时，显示取消按钮
	if (priceRangeParam) {
		$priceFilter.addClass("selected");
		$filterNav.find(".price-filter-cancel").removeClass("hide");
	}
	// 价格范围有修改时应用到过滤按钮
	var onPriceBoundChanged = function () {
		var lowerBound = parseFloat($priceLowerBound.val()) || 0;
		var upperBound = parseFloat($priceUpperBound.val()) || 0;
		$priceFilter.attr("data-value", lowerBound + "~" + upperBound);
	};
	$priceLowerBound.on("change", onPriceBoundChanged);
	$priceUpperBound.on("change", onPriceBoundChanged);
	onPriceBoundChanged();
	// 在价格范围框中按下enter时触发过滤按钮的点击事件
	var onPriceBoundKeyDown = function (e) {
		if (e.keyCode == 13) {
			onPriceBoundChanged();
			$priceFilter.click();
		}
	};
	$priceLowerBound.on("keydown", onPriceBoundKeyDown);
	$priceUpperBound.on("keydown", onPriceBoundKeyDown);
});
