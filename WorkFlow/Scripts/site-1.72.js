(function ($) {
    $.extend({
        forefullscreen: function () {
            var docElm = document.documentElement;
            if (docElm.requestFullscreen) {
                docElm.requestFullscreen();
            }
            else if (docElm.mozRequestFullScreen) {
                docElm.mozRequestFullScreen();
            }
            else if (docElm.webkitRequestFullScreen) {
                docElm.webkitRequestFullScreen();
            }
            else if (docElm.msRequestFullscreen) {
                docElm.msRequestFullscreen();
            }
        },
        exitfullscreen: function () {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            }
            else if (document.mozCancelFullScreen) {
                document.mozCancelFullScreen();
            }
            else if (document.webkitCancelFullScreen) {
                document.webkitCancelFullScreen();
            }
            else if (document.msExitFullscreen) {
                document.msExitFullscreen();
            }
        },
        postProgressData: function (url, parameters, callback, context, complete) {
            $('#div_progress').show();
            $.ajax({
                type: 'POST',
                url: url,
                data: parameters,
                error: ajaxError,
                context: context,
                success: function (data) {
                    if (data.error) {
                        alert(data.error);
                        return;
                    }
                    callback.call(this, data);
                },
                complete: function (data) {
                    $('#div_progress').hide();
                    if (complete)
                        complete.call(this, data);
                }
            });
        },
        postData: function (url, parameters, callback, context) {
            return $.ajax({
                type: 'POST',
                url: url,
                context: context,
                data: parameters,
                error: ajaxError,
                success: function (data) {
                    if (data.error) {
                        alert(data.error);
                        return;
                    }
                    callback.call(this, data);
                }
            });
        },
        addDateAndTimePicker: function () {
            $(".date").datepicker();
            $(".time").timepicker();
            $(".datetime").datetimepicker();
        },
        formatMoney: function (number, places, symbol, thousand, decimal) {
            number = number || 0;
            places = !isNaN(places = Math.abs(places)) ? places : 2;
            symbol = symbol !== undefined ? symbol : "$";
            thousand = thousand || ",";
            decimal = decimal || ".";
            var negative = number < 0 ? "-" : "",
                i = parseInt(number = Math.abs(+number || 0).toFixed(places), 10) + "",
                j = (j = i.length) > 3 ? j % 3 : 0;
            return symbol + negative + (j ? i.substr(0, j) + thousand : "") + i.substr(j).replace(/(\d{3})(?=\d)/g, "$1" + thousand) + (places ? decimal + Math.abs(number - i).toFixed(places).slice(2) : "");
        },
        modalOnSuccess: function (data) {
            if (data) {
                var $this = $(this);
                var $modal = $($this.data('modal-target') || $this.data('target'));
                if ($modal.size() === 0) $modal = null;
                var $update = $($this.data('target'));
                if ($update.size() === 0) $update = null;
                var callback = $this.data('success');
                if (window[callback] && typeof window[callback] === "function") {
                    var para = $this.data('success-para');
                    var args = [];
                    if (typeof para != 'undefined' && para != null)
                        args.push(para);
                    for (var i = 0; i < arguments.length; i++) {
                        args.push(arguments[i]);
                    }
                    window[callback].apply(this, args);
                }
                if ($modal && data.hasOwnProperty('error')) {
                    $modal.find('.modal-msg').html(data.error).removeClass('hidden');
                    return;
                }
                if ($modal && data.hasOwnProperty('close')) {
                    $modal.html('');
                    return;
                }
                var $html = $(data);
                if ($modal && $html.hasClass('modal')) {
                    $modal.html($html);
                }
                else if ($modal && $html.hasClass('scripts')) {
                    $modal.html($html);
                }
                else if ($update) {
                    $update.html($html);
                    if ($modal && $html.hasClass('close-modal')) {
                        $modal.html('');
                    }
                }
            }
        },
        parseValidationToParent: function () {
            var $form = $(this).parents('form:first');
            if ($form.size() > 0)
                $form.removeData("validator").removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse($form);
        },
        scrollTableData: function (scrollele, url) {
            var initTop = 0;
            $(scrollele).on('scroll', function (e) {
                var scrollTop, maxScroll;
                scrollTop = this.scrollTop;
                if (scrollTop < initTop) {
                    initTop = scrollTop;
                    return false;
                }
                initTop = scrollTop;
                maxScroll = this.scrollHeight - this.offsetHeight;
                if (scrollTop >= maxScroll || (maxScroll - scrollTop) < 50) {
                    if (!$(this).data('isLoading')) {
                        var qrcode = $('tbody', this).data('last-qrcode');
                        if (!qrcode) return false;
                        $(this).data('isLoading', true);
                        $.ajax({
                            type: 'POST',
                            url: url,
                            context: $(this),
                            data: { qrcode: qrcode },
                            error: ajaxError,
                            success: function (data) {
                                var $tbody = $('tbody', scrollele);
                                $tbody.append($(data));
                                $tbody.data('last-qrcode', $tbody.find('tr:last').data('qrcode'));
                            },
                            complete: function () {
                                $(scrollele).data('isLoading', false);
                            }
                        });
                    }
                    return false;
                }
                if (scrollTop <= 0) {
                    return false;
                }
            });
        },
        sortTable: function (table, colIndex, dir) {
            var tbody = table.tBodies[0];
            var colRows = tbody.rows;
            var aTrs = new Array;
            for (var i = 0; i < colRows.length; i++) {
                aTrs[i] = colRows[i];
            }
            var compFunc = (function (index, direction) {
                return function (tr1, tr2) {
                    var vValue1 = tr1.cells[index].getAttribute("data-compare");
                    var vValue2 = tr2.cells[index].getAttribute("data-compare");
                    if (vValue1 < vValue2) {
                        return direction === 'asc' ? -1 : 1;
                    } else if (vValue1 > vValue2) {
                        return direction === 'asc' ? 1 : -1;
                    } else {
                        return 0;
                    }
                };
            })(colIndex, dir);
            aTrs.sort(compFunc);
            var oFragment = document.createDocumentFragment();
            for (var i = 0; i < aTrs.length; i++) {
                oFragment.appendChild(aTrs[i]);
            }
            tbody.appendChild(oFragment);
        }
    });
    $.fn.extend({
        addUnobtrusiveValidation: function () {
            if (this.is('form')) {
                this.removeData("validator").removeData("unobtrusiveValidation");
                $.validator.unobtrusive.parse(this);
            }
            return this;
        },
        showTab: function (options) {
            var $tabs = this;
            this.click(function () {
                var $this = $(this);
                $tabs.removeClass('current');
                $this.addClass('current');
                if (options && typeof options.changed == 'function') options.changed();
                var $target = $(options.target);
                $tabs.each(function () {
                    $target.removeClass($(this).data('class'));
                });
                $target.addClass($this.data('class'));
            });
            return this;
        },
        saveSettings: function (url, success) {
            this.on("change", '[data-key]', function (e) {
                var $me = $(this);
                if ($me.is(':radio')) {
                    e.preventDefault();
                }
                var pattern = $me.data('pattern');
                if (pattern && !new RegExp(pattern).test($me.val())) {
                    if (!$me.hasClass('input-validation-error')) $me.addClass('input-validation-error');
                    return;
                }
                $('#div_progress').show();
                $.ajax({
                    type: 'POST',
                    url: url,
                    error: ajaxError,
                    context: $me,
                    data: { settingName: $me.data('key'), settingValue: $me.val() },
                    success: function (result) {
                        $('#div_progress').hide();
                        if (result) {
                            this.removeClass('input-validation-error').prop('ischecked', true);
                        } else {
                            if (!this.hasClass('input-validation-error')) $(this).addClass('input-validation-error');
                        }
                        if (success && typeof success == "function")
                            success.apply($me.get(0));
                    }
                });
            });
        },
        draggableTo: function (selector, targetSelector, onDropped) {
            var target = null;
            this.on("dragstart", selector, function (e) {
                target = this;
                //e.dataTransfer.effectAllowed = 'all';
                //e.dataTransfer.setData('text/html', this.innerHTML);
            });

            $(targetSelector).on("dragenter", function (e) {
                if (!$(this).hasClass('over')) $(this).addClass('over');
                e.preventDefault();
            });

            $(targetSelector).on("dragover", function (e) {
                e.preventDefault();
            });

            $(targetSelector).on("dragleave", function (e) {
                if ($(this).hasClass('over')) $(this).removeClass('over');
                e.preventDefault();
            });

            this.on("dragend", selector, function (e) {
                if ($(targetSelector).hasClass('over')) $(targetSelector).removeClass('over');
                e.preventDefault();
                target = null;
            });

            $(targetSelector).on("drop", function (e) {
                $(target).remove();
                $(targetSelector).append($(target));
                target = null;
                e.preventDefault();
            });
        },
        pasteImage: function (url, onstart, onsuccess) {

            function uploadFile(file) {
                if (!/^image*/.test(file.type)) {
                    alert('Unsupported file format');
                    return;
                }
                var formData = new FormData();
                formData.append("file", file);
                $.ajax({
                    url: url,
                    type: "POST",
                    processData: false,
                    contentType: false,
                    data: formData,
                    success: function (res) {
                        var args = [];
                        for (var i = 0; i < arguments.length; i++) {
                            args.push(arguments[i]);
                        }
                        if (typeof onsuccess == 'function') onsuccess.apply(this, args);
                    }
                });
            }

            this.on("paste",
                function (e) {
                    if (typeof onstart == 'function') onstart();
                    e.stopPropagation();
                    var clipboardData = e.originalEvent.clipboardData;
                    if (clipboardData.items.length <= 0) {
                        return;
                    }
                    var file = clipboardData.items[0].getAsFile();
                    if (!file) {
                        return;
                    }
                    uploadFile(file, onsuccess);
                    e.preventDefault();
                });
        },
        dragToUploadImage: function (url, onstart, onsuccess) {

            function uploadFile(file) {
                if (!/^image*/.test(file.type)) {
                    alert('Unsupported file format');
                    return;
                }
                var formData = new FormData();
                formData.append("file", file);
                $.ajax({
                    url: url,
                    type: "POST",
                    processData: false,
                    contentType: false,
                    data: formData,
                    success: function (res) {
                        var args = [];
                        for (var i = 0; i < arguments.length; i++) {
                            args.push(arguments[i]);
                        }
                        if (typeof onsuccess == 'function') onsuccess.apply(this, args);
                    }
                });
            }

            $(this).on("dragenter", function (e) {
                if (typeof onstart == 'function') onstart();
                e.preventDefault();
            });

            $(this).on("dragover", function (e) {
                e.preventDefault();
            });

            $(this).on("drop", function (e) {
                e.stopPropagation();
                e.preventDefault();
                var files = e.originalEvent.dataTransfer.files;
                var file;
                for (var i = 0; i < files.length; i++) {
                    file = files[i];
                    uploadFile(file);
                }
            });
        }
    });
})(jQuery);

Array.prototype.take = function (count) {
    var newArray = [];
    for (var i = 0; i < this.length; i++) {
        if (i < count) {
            newArray.push(this[i]);
        }
        else break;
    }
    return newArray;
}

Array.prototype.skip = function (count) {
    var newArray = [];
    for (var i = 0; i < this.length; i++) {
        if (i >= count) {
            newArray.push(this[i]);
        }
    }
    return newArray;
}

function ajaxError(XMLHttpRequest, textStatus, errorThrown) {
    if (XMLHttpRequest.status === 404) {
        alert('Page not found!');
        return;
    }
}

function removeTableRow(result) {
    $('#div_progress').hide();
    if (result.success)
        $(this).closest('tr').remove();
}

$.page = {
    toggleMenu: function () {
        $('#menu-toggle-btn').click(function () {
            var webreport = $("iframe.myFrame");
            var ishidden = $('#left-menu').hasClass('hidden');
            var menuleft = ishidden ? '0' : '-320px';
            if (ishidden) {
                $(document.body).removeClass('menu-hidden');
                webreport.width($(window).width() - 320);
            } else {
                $(document.body).addClass('menu-hidden');
                webreport.width($(window).width());
            }
            $('#left-menu').removeClass('hidden').animate({ left: menuleft }, 'fast', function () {
                ishidden ? $('#left-menu').removeClass('hidden') : $('#left-menu').addClass('hidden');
                $('.content-section').css('left', ishidden ? '320px' : '0');
            });
        });
    },
    getOrderParams: function () {
        var params = [];
        $('#order_data_table tbody > tr.selected:visible').each(function () {
            var $tr = $(this);
            params.push({
                brand: $tr.data('brand'),
                channel: $tr.data('channel'),
                orderno: $tr.data('orderno'),
                warehouse: $tr.data('warehouse')
            });
        });
        return params;
    },
    getSelectedQRcodes: function () {
        var params = [];
        $('#order_data_table tbody > tr.selected:visible').each(function () {
            var $tr = $(this);
            params.push($tr.data('qrcode'));
        });
        return params;
    },
    badgeSubject: {
        lastBadgeValue: null,
        updateAll: function (data) {
            this.updatePage(data);
            this.updateWarehouse(data);
            this.lastBadgeValue = data;
        },
        findLastPageValue: function (status) {
            if (this.lastBadgeValue != null) {
                for (var i = 0; i < this.lastBadgeValue['Page'].length; i++) {
                    if (this.lastBadgeValue['Page'][i].status === status) {
                        return this.lastBadgeValue['Page'][i];
                    }
                }
            }
            return null;
        },
        findLastWarehouseValue: function (code) {
            if (this.lastBadgeValue != null) {
                for (var i = 0; i < this.lastBadgeValue['Warehouses'].length; i++) {
                    if (this.lastBadgeValue['Warehouses'][i].code === code) {
                        return this.lastBadgeValue['Warehouses'][i];
                    }
                }
            }
            return null;
        },
        updatePage: function (data) {
            if (data && data['Page']) {
                for (var i = 0; i < data['Page'].length; i++) {
                    var item = data['Page'][i];
                    //var lastValue = this.findLastPageValue(item.status);
                    //if (lastValue != null && lastValue.value == item.value) {
                    //    continue;
                    //}
                    $(item.expression).text(item.value).parent().removeClass('hidden');
                }
            }
        },
        updateWarehouse: function (data) {
            if (data && data['Warehouses']) {
                for (var i = 0; i < data['Warehouses'].length; i++) {
                    var item = data['Warehouses'][i];
                    if (item.values) {
                        for (var name in item.values) {
                            var subitem = item.values[name];
                            //var lastValue = this.findLastWarehouseValue(item.code);
                            //if (lastValue != null && lastValue.values[name] == subitem) {
                            //    continue;
                            //}
                            $('.wh-' + item.code + "-" + name).text(subitem).parent().removeClass('hidden');
                        }
                    }
                }
            }
        }
    }
}

function handleOrders(returnValue, callback, handler) {
    if (returnValue != null && returnValue.hasOwnProperty('error')) {
        alert(returnValue.error);
        return false;
    }
    var data = returnValue.data;
    var msg = [];
    if (data != null && data.length > 0) {
        var num = 0;
        for (var i = 0; i < data.length; i++) {
            var item = data[i];
            if (item.success) {
                num++;
                if (handler && typeof (handler) == 'function') {
                    handler($('#order_data_table tr[data-qrcode="' + item.qrcode + '"]'));
                } else {
                    $('#order_data_table tr[data-qrcode="' + item.qrcode + '"]').remove();
                }
            } else {
                msg.push(item.message);
            }
        }
        if (callback && typeof (callback) == 'function')
            callback(num);
        if (msg.length > 0) {
            alert(msg.join("\n"));
        }
    }
}

(function ($, undefined) {
    $.fn.extend({
        mySortTable: function () {

            function textExtraction(node) {
                var attr = node.getAttribute('data-compare-number');
                if (attr != null) {
                    return parseFloat(attr);
                }
                attr = node.getAttribute('data-compare');
                if (attr != null) {
                    return attr;
                }
                return $.trim(node.innerHTML);
            };

            function compareRow(iCol, dir) {
                return function (tr1, tr2) {
                    var vValue1 = textExtraction(tr1.cells[iCol]);
                    var vValue2 = textExtraction(tr2.cells[iCol]);
                    if (vValue1 < vValue2) {
                        return dir === 'asc' ? -1 : 1;
                    } else if (vValue1 > vValue2) {
                        return dir === 'asc' ? 1 : -1;
                    } else {
                        return 0;
                    }
                };
            };

            function sortTable(table, iCol) {
                var tbody = table.tBodies[0];
                var colRows = tbody.rows;
                var aTrs = new Array;
                for (var i = 0; i < colRows.length; i++) {
                    aTrs[i] = colRows[i];
                }
                //判断上一次排列的列和现在需要排列的是否同一个。     
                //if (table.sortCol == iCol) {
                //aTrs.reverse();
                //}
                //else
                {
                    //如果不是同一列，使用数组的sort方法，传进排序函数     
                    if (table.sortCol === iCol) {
                        //aTrs.reverse();
                        table.sortDir = (table.sortDir === 'desc' ? 'asc' : 'desc');
                    } else {
                        table.sortDir = 'asc';
                    }
                    var compFunc = compareRow(iCol, table.sortDir);
                    aTrs.sort(compFunc);
                }
                var oFragment = document.createDocumentFragment();
                for (var i = 0; i < aTrs.length; i++) {
                    oFragment.appendChild(aTrs[i]);
                }
                tbody.appendChild(oFragment);
                //记录最后一次排序的列索引     
                table.sortCol = iCol;
            };

            var $this = this;
            $this.find('thead th').each(function (index) { $(this).find('.orderable-display-value').data('col-index', index); }).filter('[data-compare]').find('.orderable-display-value').click(function (e) {
                if ($(e.target).hasClass('glyphicon-menu-down')) {
                    return;
                }
                var colindex = $(this).data('col-index');
                sortTable($this.get(0), colindex);
            });

        },
        sortCommonTable: function () {

            function textExtraction(node) {
                var attr = node.getAttribute('data-compare-number');
                if (attr != null) {
                    return parseFloat(attr);
                }
                attr = node.getAttribute('data-compare');
                if (attr != null) {
                    return attr;
                }
                return $.trim(node.innerHTML);
            };

            function compareRow(iCol) {
                return function (tr1, tr2) {
                    var vValue1 = textExtraction(tr1.cells[iCol]);
                    var vValue2 = textExtraction(tr2.cells[iCol]);
                    if (vValue1 < vValue2) {
                        return -1;
                    } else if (vValue1 > vValue2) {
                        return 1;
                    } else {
                        return 0;
                    }
                };
            };

            function sortTable(table, iCol) {
                var $th = $($(table).find('thead th').get(iCol));
                var $lable = $th.find('label');
                if ($lable.size() > 0) {
                    var asc = $lable.hasClass('sort-asc');
                    $(table).find('thead th>label').remove();
                    if (asc)
                        $th.append($('<label style="font-weight:bold;" class="sort-desc">∨</label>'));
                    else
                        $th.append($('<label style="font-weight:bold;" class="sort-asc">∧</label>'));
                } else {
                    $(table).find('thead th>label').remove();
                    $th.append($('<label style="font-weight:bold;" class="sort-asc">∧</label>'));
                }
                var tbody = table.tBodies[0];
                var colRows = tbody.rows;
                var aTrs = new Array;
                //将将得到的列放入数组，备用     
                for (var i = 0; i < colRows.length; i++) {
                    aTrs[i] = colRows[i];
                }
                //判断上一次排列的列和现在需要排列的是否同一个。     
                if (table.sortCol == iCol) {
                    aTrs.reverse();
                } else {
                    //如果不是同一列，使用数组的sort方法，传进排序函数     
                    var compFunc = compareRow(iCol);
                    aTrs.sort(compFunc);
                }
                var oFragment = document.createDocumentFragment();
                for (var i = 0; i < aTrs.length; i++) {
                    oFragment.appendChild(aTrs[i]);
                }
                tbody.appendChild(oFragment);
                //记录最后一次排序的列索引     
                table.sortCol = iCol;
            };

            var $this = this;
            $this.find('thead th').each(function (index) { $(this).data('col-index', index); }).filter('[data-compare]').click(function (e) {
                var colindex = $(this).data('col-index');
                sortTable($this.get(0), colindex);
            });

        },
        showReasonModal: function (callback) {
            var $this = this;
            //reset
            $this.css('display', 'block').find('.form-control').removeClass('input-validation-error').val('');
            //$this.find('.remark-row').removeClass('hidden').addClass('hidden');
            //handle ok 
            $this.find('.btn-primary').get(0).onclick = function () {
                var reason = $.trim($('#ChangeReason').val());
                var remark = $.trim($('#Remark').val());
                if (reason == '') {
                    $('#ChangeReason').addClass('input-validation-error');
                    return;
                }
                if (reason == '0' && remark == '') {
                    $('#Remark').addClass('input-validation-error');
                    return;
                }
                callback(reason, remark);
            };
            //handle reason
            //$this.find('#ChangeReason').get(0).onclick = function () {
            //    var val = $.trim($(this).val());
            //    if (val != '') $(this).removeClass('input-validation-error');
            //    if (val == '0') $this.find('.remark-row').removeClass('hidden'); else $this.find('.remark-row').addClass('hidden');
            //}
        },
        orderTable: function (options) {
            var $this = this;
            $this.mySortTable();

            function allSelected(event) {
                //#TODO
                $('tbody>tr:not(.disabled) :checkbox:visible', $this).prop('checked', this.checked);
                this.checked ? $('tbody>tr:visible:not(.disabled)', $this).addClass('selected') : $('tbody>tr:visible', $this).removeClass('selected');
                if (options.selectedRowsChanged) {
                    options.selectedRowsChanged();
                }
            };

            function itemRowSelected(event) {
                var $tr = $(this).closest('tr');
                if ($tr.hasClass('disabled')) return;
                $this.find('tr.selected').removeClass('selected'); //do not allow multiple selection, because we need move back one by one
                if (!$tr.hasClass('selected')) {
                    $tr.addClass('selected');
                }
                if (options.selectedRowsChanged) {
                    options.selectedRowsChanged();
                }
            };

            function itemRowChecked(event) {
                var $tr = $(this).closest('tr');
                if ($tr.hasClass('disabled')) {
                    event.preventDefault();
                    return;
                }
                var $all = $('thead :checkbox', $this);
                if (!$(this).prop('checked')) {
                    $tr.removeClass('selected');
                    $all.prop('checked', false);
                } else {
                    $tr.closest('tr').addClass('selected');
                    var allchecked = true;
                    $('tbody :checkbox', $this).each(function () {
                        if (!$(this).prop('checked')) {
                            allchecked = false;
                            return false;
                        }
                    });
                    $all.prop('checked', allchecked);
                }
                if (options.selectedRowsChanged) {
                    options.selectedRowsChanged();
                }
            };

            function updateTbody(opt) {
                $('#div_progress').show();
                $.ajax({
                    type: 'POST',
                    url: opt.url,
                    data: opt.parameters,
                    error: ajaxError,
                    success: function (data) {
                        $this.children('tfoot').remove();
                        $this.children('tbody').replaceWith(data);
                        if (opt.tableRowsChanged) {
                            opt.tableRowsChanged();
                        }
                    },
                    complete: function () {
                        $('#div_progress').hide();
                    }
                });
            }

            this.each(function () {
                options = $.extend({ onFailure: 'ajaxError', oadingElementId: "div_progress" }, options || {});
                var $this = $(this);
                var $cbSelectAll = $('thead :checkbox', $this);
                if ($cbSelectAll.size() > 0) {
                    $cbSelectAll.click(allSelected);
                }
                $this.on('click', 'tbody :checkbox', itemRowChecked);
                $this.on('click', 'tbody .row-selector', itemRowSelected);
                var parameters = {};
                $this.on('click', 'thead .header-filter li', function () { //click on filter title
                    parameters[$(this).data('name')] = $(this).data('value');
                    $($(this).data('target')).find('.orderable-display-value').text($(this).data('display'));
                    parameters['menucode'] = $('#menucode').val();
                    parameters['warehouse'] = $('#warehouse').val();
                    updateTbody({ url: options.url, parameters: parameters, tableRowsChanged: options.tableRowsChanged });
                    $(this).closest('.header-filter').removeClass('open');
                });
                //save picked date
                $this.on('change', 'thead input[type=hidden]', function () { //click on datetime picker
                    //var parameters = {}; //saved state
                    if ($(this).val() != '')
                        $($(this).data('target')).find('.orderable-display-value').text($(this).val());
                    parameters[$(this).prop('id')] = $(this).val();
                    parameters['menucode'] = $('#menucode').val();
                    parameters['warehouse'] = $('#warehouse').val();
                    updateTbody({ url: options.url, parameters: parameters, tableRowsChanged: options.tableRowsChanged });
                });
                //clicked on more
                $this.on('click', 'a.more-a', function () {
                    $(this.parentNode).hasClass('show-more') ? $(this.parentNode).removeClass('show-more') : $(this.parentNode).addClass('show-more');
                });
                //collapse on more
                $this.on('click', 'a.collapse-a', function () {
                    $(this.parentNode).hasClass('show-more') ? $(this.parentNode).removeClass('show-more') : $(this.parentNode).addClass('show-more');
                });
                //click on filter trigger
                $this.find('th .glyphicon-menu-down').click(function (e) {
                    var targetId = $(this).data('target');
                    var $filter = $(targetId);
                    $filter.hasClass('open') ? $filter.removeClass('open') : $filter.addClass('open');
                    popups[targetId] = {
                        container: targetId + " .dropdown-menu", triggerElement: targetId + ' .glyphicon-menu-down', close: function () {
                            $(targetId).removeClass('open');
                        }
                    }
                });

                $this.on('mouseenter', '.flag', function () {
                    var $tr = $(this).closest('tr');
                    if ($tr.hasClass('disabled')) return;
                    if (!$tr.find('.mark-comments').hasClass('hidden')) // it is already showing
                        return;
                    lastInterVal.cancelIfTooShort();
                    var showComment = (function ($table, $flag) {
                        return function () {
                            var $last = $table.data('lastcomments');
                            if ($last) $last.html('').addClass('hidden');
                            var $tr = $flag.closest('tr');
                            var $div = $tr.find('.mark-comments');
                            $table.data('lastcomments', $div);
                            if (options.getcommentsurl) {
                                if (lastInterVal.lastRequest)
                                    lastInterVal.lastRequest.abort();
                                lastInterVal.lastRequest = $.ajax({
                                    type: 'POST',
                                    url: options.getcommentsurl,
                                    context: $flag,
                                    data: {
                                        qrcode: $tr.data('qrcode')
                                    },
                                    success: function (data) {
                                        $div.html(data).removeClass('hidden');
                                        $tr.find('.mycomment').focus();
                                        popups['comments'] = {
                                            container: $div, triggerElement: $flag, close: function () {
                                                $div.html('').addClass('hidden');
                                            }
                                        }
                                    }
                                });
                            }
                        };
                    })($this, $(this));
                    lastInterVal.time = new Date().getTime();
                    lastInterVal.timeout = setTimeout(showComment, lastInterVal.duration);
                });

                $this.on('click', '.frozen', function () {
                    if (options.savefrozenurl) {
                        var $ele = $(this);
                        var $td = $ele.closest('td');
                        var $tr = $td.closest('tr');
                        var isfrozen = $tr.hasClass('disabled');
                        $.postData(options.savefrozenurl, {
                            qrcode: $tr.data('qrcode'), isfrozen: !isfrozen
                        }, function (data) {
                            if (data.error) {
                                alert(data.error);
                                return;
                            }
                            if (data.success) {
                                if (data.isfrozen) {
                                    $td.attr('data-compare', 'True').data('compare', 'True');
                                    $tr.addClass('disabled').removeClass('selected');
                                } else {
                                    $td.attr('data-compare', 'False').data('compare', 'False');
                                    $tr.removeClass('disabled');
                                }
                            }
                        }, $tr);
                    }
                });

                $this.on('mouseleave', '.cmt-area', function () {
                    lastInterVal.cancelIfTooShort();
                    var $tr = $(this).closest('tr');
                    if ($tr.hasClass('disabled')) return;
                    var $div = $tr.find('.mark-comments');
                    if (lastInterVal.lastRequest)
                        lastInterVal.lastRequest.abort();
                    if ($div.hasClass('hidden'))
                        return;
                    $div.html('').addClass('hidden');
                });

                $this.on('click', '.edit-trans', function () {
                    var $row = $(this).closest('tr');
                    if ($row.hasClass('disabled')) return;
                    $('#div_progress').show();
                    $.ajax({
                        type: 'GET',
                        url: options.edittransurl,
                        data: {
                            qrcode: $row.data('qrcode')
                        },
                        context: $row,
                        success: function (data) {
                            $('#div_edit_cont').html(data);
                        },
                        complete: function () {
                            $('#div_progress').hide();
                        }
                    });
                });

                $this.on('click', '.split-trans', function () {
                    var $row = $(this).closest('tr');
                    if ($row.hasClass('disabled')) return;
                    $('#div_progress').show();
                    $.ajax({
                        type: 'GET',
                        url: options.splittransurl,
                        data: {
                            qrcode: $row.data('qrcode')
                        },
                        context: $row,
                        success: function (data) {
                            $('#div_edit_cont').html(data);
                        },
                        complete: function () {
                            $('#div_progress').hide();
                        }
                    });
                });

                //save comments
                $this.on('click', '.btn-primary', function () {
                    var $savebtn = $(this);
                    var $tr = $savebtn.closest('tr');
                    var $comments = $tr.find('.mycomment');
                    var $div = $tr.find('.comment-lines');
                    var cmts = $.trim($comments.val());
                    if (cmts == '') {
                        return;
                    }
                    $tr.find('img.loading').removeClass('hidden');
                    $savebtn.addClass('hidden');
                    $.postData(options.savecommenturl, {
                        qrcode: $tr.data('qrcode'), comment: cmts
                    }, function (data) {
                        if (data.error) {
                            alert(data.error);
                            return;
                        }
                        if (data.success) {
                            this.find('.mark-comments').addClass('hidden').html('');
                            //$comments.val('');
                            //$div.append(data.html);
                            this.find('.flag').addClass('selected');
                        }
                        $tr.find('img.loading').addClass('hidden');
                        $savebtn.removeClass('hidden');
                    }, $tr);
                });
            });
        },
        editRow: function () {
            this.each(function () {
                var $tr = $(this);
                $tr.find('.edit-field').addClass('hidden').each(function () {
                    var $this = $(this);
                    $('<input type="text"/>').val($this.text()).attr('data-property-name', $this.data('property-name')).insertAfter($this);
                });
            });
        }
    });
})(jQuery);

var lastInterVal = {
    duration: 100,
    cancelIfTooShort: function () {
        if (this.time && this.timeout && (new Date().getTime() - this.time) < this.duration) {
            this.time = 0;
            clearTimeout(this.timeout);
            this.timeout = null;
        }
    }
};

var popups = {};

$(function () {
    $(document).on('click', function (e) {
        var $target = $(e.target);
        for (var name in popups) {
            if (popups[name].triggerElement && $target.closest($(popups[name].triggerElement)).length) { // click on triggerElement container
                continue;
            }
            if (popups[name].container && $target.closest($(popups[name].container)).length) { // click on popup container
                continue;
            }
            popups[name].close();
        }
    });
});

function refreshEventQuery(data, params) {
    if (data.success) {
        var url = $('#queryURL').val();
        if (!!!url) {
            return;
        }
        $('#div_progress').show();
        $.ajax({
            type: 'POST',
            url: url,
            data: params,
            error: ajaxError,
            success: function (data) {
                $('#queryResult').html(data);
            },
            complete: function () {
                $('#div_progress').hide();
            }
        });
    }
}

function removeOrderRow(qrcode, result) {
    if (result.close) {
        var $tr = $('#order_data_table tr[data-qrcode="' + qrcode + '"]');
        if (result.data) {
            if (result.data == 'ignore') {
                return;
            }
            if (result.data.newOrderno) {
                $('#orderno').val(result.data.newOrderno).closest('form').submit();
            }
            $tr.remove();
        } else {
            $tr.remove();
        }
    }
}