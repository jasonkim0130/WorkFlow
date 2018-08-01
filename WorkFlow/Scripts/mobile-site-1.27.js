function isAncestorOrSelf(parent, child) {
    while (child != null) {
        if (child == parent)
            return true;
        child = child.parentNode;
    }
    return false;
}

function forefullscreen() {
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
}

function exitfullscreen() {
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
}

function ajaxError(XMLHttpRequest, textStatus, errorThrown) {
    if (XMLHttpRequest.status === 404) {
        alert('Page not found!');
        return;
    }
    //alert('An error occured while processing your request!');
}

function runAjax(url, parameters, success, context) {
    if (!!!url) {
        return;
    }
    return $.ajax({
        type: 'POST',
        url: url,
        context: context,
        data: parameters,
        error: ajaxError,
        success: function (data) {
            success.call(this, data);
        }
    });
}

function runProgressAjax(url, parameters, callback, context) {
    if (!!!url) {
        return;
    }
    $('#div_progress').show();
    $.ajax({
        type: 'POST',
        url: url,
        data: parameters,
        error: ajaxError,
        dataType: 'json',
        context: context,
        success: function (data) {
            callback.call(this, data);
        },
        complete: function () {
            $('#div_progress').hide();
        }
    });
}

function gotoUrl(url, parameters, callback, context) {
    if (!!!url) {
        return;
    }
    $('#div_progress').show();
    $.ajax({
        type: 'POST',
        url: url,
        data: parameters,
        error: ajaxError,
        context: context,
        success: function (data) {
            callback.call(this, data);
        },
        complete: function () {
            $('#div_progress').hide();
        }
    });
}

function loaddropdownlist(container, source, target, url) {
    $(container).on('change', source, function () {
        var country = $(this).val();
        $(target).find('option').remove();
        if (country != '') {
            runAjax(url, { contrycode: country }, function (data) {
                if (this == $(source).val()) {
                    $(target).append($(data));
                }
            }, country);
        }
    });
}

function toggleLeftMenu() {
    $('#menu-toggle-btn').click(function () {
        var ishidden = $('#left-menu').hasClass('hidden');
        var menuleft = ishidden ? '0' : '-280px';
        if (ishidden) {
            $(document.body).removeClass('menu-hidden');
        } else {
            $(document.body).addClass('menu-hidden');
        }
        $('#left-menu').removeClass('hidden').animate({ left: menuleft }, 'fast', function () {
            ishidden ? $('#left-menu').removeClass('hidden') : $('#left-menu').addClass('hidden');
        });
    });
}

function displaySummaryDiagramSwitch() {
    $('#div_summary_container').removeClass('hidden');
}

function hideSummaryDiagramSwitch() {
    if (!$('#div_summary_container').hasClass('hidden'))
        $('#div_summary_container').addClass('hidden');
}

function handleOrdersResult(returnValue, callback, handler) {
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
                if (handler) {
                    handler($('#order_data_table>tbody>tr[data-orderno=' + item.orderno + ']'));
                } else {
                    $('#order_data_table>tbody>tr[data-orderno=' + item.orderno + ']').remove();
                }
            } else {
                msg.push(item.message);
            }
        }
        callback(num);
        if (msg.length > 0) {
            alert(msg.join("\n"));
        }
    }
}

function getOrderParams() {
    var params = [];
    $('#order_data_table tbody > tr.selected:visible').each(function () {
        var $tr = $(this);
        params.push({
            brand: $tr.data('brand'),
            channel: $tr.data('channel'),
            orderno: $tr.data('orderno')
        });
    });
    return params;
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
                        $th.append($('<label style="font-weight:bold;font-size:large;margin:0" class="sort-desc">∨</label>'));
                    else
                        $th.append($('<label style="font-weight:bold;font-size:large;margin:0" class="sort-asc">∧</label>'));
                } else {
                    $(table).find('thead th>label').remove();
                    $th.append($('<label style="font-weight:bold;font-size:large;margin:0" class="sort-asc">∧</label>'));
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
                $('tbody :checkbox:visible', $this).prop('checked', this.checked);
                this.checked ? $('tbody>tr:visible', $this).addClass('selected') : $('tbody>tr:visible', $this).removeClass('selected');
                if (options.selectedRowsChanged) {
                    options.selectedRowsChanged();
                }
            };

            function itemRowSelected(event) {
                $this.find('tr.selected').removeClass('selected');
                var $tr = $(this).closest('tr');
                if (!$tr.hasClass('selected')) {
                    $tr.addClass('selected');
                }
                if (options.selectedRowsChanged) {
                    options.selectedRowsChanged();
                }
            };

            function itemRowChecked(event) {
                var $all = $('thead :checkbox', $this);
                if (!$(this).prop('checked')) {
                    $(this).closest('tr').removeClass('selected');
                    $all.prop('checked', false);
                } else {
                    $(this).closest('tr').addClass('selected');
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
                    var parameters = {};
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
                    popups[targetId] = { container: targetId + " .dropdown-menu", triggerElement: targetId + ' .glyphicon-menu-down', close: function () { $(targetId).removeClass('open'); } }
                });

                $this.on('mouseenter', '.flag', function () {
                    if (!$(this).closest('tr').find('.mark-comments').hasClass('hidden')) // it is already showing
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
                                    data: { BRAND: $tr.data('brand'), CHANNEL: $tr.data('channel'), ORDERNO: $tr.data('orderno') },
                                    success: function (data) {
                                        $div.html(data).removeClass('hidden');
                                        $tr.find('.mycomment').focus();
                                        popups['comments'] = { container: $div, triggerElement: $flag, close: function () { $div.html('').addClass('hidden'); } }
                                    }
                                });
                            }
                        };
                    })($this, $(this));
                    lastInterVal.time = new Date().getTime();
                    lastInterVal.timeout = setTimeout(showComment, lastInterVal.duration);
                });

                $this.on('mouseleave', '.cmt-area', function () {
                    lastInterVal.cancelIfTooShort();
                    var $div = $(this).closest('tr').find('.mark-comments');
                    if (lastInterVal.lastRequest)
                        lastInterVal.lastRequest.abort();
                    if ($div.hasClass('hidden'))
                        return;
                    $div.html('').addClass('hidden');
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
                    runAjax(options.savecommenturl, {
                        BRAND: $tr.data('brand'), CHANNEL: $tr.data('channel'), ORDERNO: $tr.data('orderno'), comment: cmts
                    }, function (data) {
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

var badgeSubject = {
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
};

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
    if (data.error) {
        $('#div_modal_message').removeClass('hidden').html(data.error);
        return;
    }
    if (data.success) {
        $('#div_modal_cont').remove();
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
    } else {
        alert('请求未能完成');
    }
}

function handleModalResult(data, params) {
    if (data.error) {
        $('#div_modal_message').removeClass('hidden').html(data.error);
        return;
    }
    if (data.success) {
        $('#div_message_cont').html('');
        $('#log_content table').remove();
        $('#log_content').addClass('hidden');
    } else {
        alert('请求未能完成');
    }
}

function modalCallback(data) {
    if (data.error) {
        $('#div_modal_message').removeClass('hidden').html(data.error);
        return;
    }
    $($(this).data('ajax-update')).html(data);
}

function appendTableRow(target, url) {
    runAjax(url, {}, function (data) {
        $('#' + this).find('tbody').append(data);
    }, target);
}