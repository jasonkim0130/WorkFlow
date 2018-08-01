var webSocket;
var printTask = [];
var localPrinters = [];
//备注：webSocket 是全局对象，不要每次发送请求丢去创建一个，做到webSocket对象重用，和打印组件保持长连接。
function doConnect(onOpen, onError) {
    if (webSocket && webSocket.readyState == 1) {
        if (onOpen && typeof onOpen == "function") {
            onOpen();
        }
        return;
    }
    try {
        webSocket = new WebSocket('ws://localhost:13528');
        //如果是https的话，端口是13529
        //socket = new WebSocket('wss://localhost:13529');
        // 打开Socket
        webSocket.onopen = function (event) {
            if (onOpen && typeof onOpen == "function") {
                onOpen();
            }
            // 监听消息
            webSocket.onmessage = function (event) {
                console.log('Client received a message', event);
                var response = JSON.parse(event.data);
                if (response.cmd == 'getPrinters') {
                    //getPrintersHandler(response);//处理打印机列表
                    localPrinters = response.printers;
                    if (printTask.length > 0 && localPrinters.length > 0) {
                        doPrint(localPrinters[0].name);
                    }
                } else if (response.cmd == 'printerConfig') {
                    //printConfigHandler(response);
                }
            };
            // 监听Socket的关闭
            webSocket.onclose = function (event) {
                console.log('Client notified socket has closed', event);
            };
            var request = getRequestObject("getPrinters");
            webSocket.send(JSON.stringify(request));
        };
        webSocket.onerror = function () {
            if (onError && typeof onError == "function") {
                onError();
            }
        }
    } catch (error) {

    }
}
/***
 * 
 * 获取请求的UUID，指定长度和进制,如 
 * getUUID(8, 2)   //"01001010" 8 character (base=2)
 * getUUID(8, 10) // "47473046" 8 character ID (base=10)
 * getUUID(8, 16) // "098F4D35"。 8 character ID (base=16)
 *   
 */
function getUUID(len, radix) {
    var chars = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'.split('');
    var uuid = [], i;
    radix = radix || chars.length;
    if (len) {
        for (i = 0; i < len; i++) uuid[i] = chars[0 | Math.random() * radix];
    } else {
        var r;
        uuid[8] = uuid[13] = uuid[18] = uuid[23] = '-';
        uuid[14] = '4';
        for (i = 0; i < 36; i++) {
            if (!uuid[i]) {
                r = 0 | Math.random() * 16;
                uuid[i] = chars[(i == 19) ? (r & 0x3) | 0x8 : r];
            }
        }
    }
    return uuid.join('');
}
/***
 * 构造request对象
 */
function getRequestObject(cmd) {
    var request = new Object();
    request.requestID = getUUID(8, 16);
    request.version = "1.0";
    request.cmd = cmd;
    return request;
}
/***
 * 获取自定义区数据以及模板URL
 * waybillNO 电子面单号
 */
function getCustomAreaData(waybillNo) {
    //获取waybill对应的自定义区的JSON object，此处的ajaxGet函数是伪代码
    //var jsonObject = ajaxGet(waybillNo);
    var ret = new Object();
    ret.templateURL = waybillNo.content.templateURL;
    ret.data = waybillNo.content.data;
    return ret;
}
/***
 * 获取电子面单Json 数据
 * waybillNO 电子面单号
 */
function getWaybillJson(waybillNO) {
    //获取waybill对应的json object，此处的ajaxGet函数是伪代码
    // var jsonObject = ajaxGet(waybillNO);
    //return jsonObject;
}
/**
 * 打印电子面单
 * printer 指定要使用那台打印机
 * waybillArray 要打印的电子面单的数组
 */
function doPrint(printer) {
    var request = getRequestObject("print");
    request.task = new Object();
    request.task.taskID = getUUID(8, 10);
    request.task.preview = false;
    request.task.printer = printer;
    var documents = new Array();
    var length = printTask.length;
    for (i = 0; i < length; i++) {
        request.task.printer = printTask[i].printer || request.task.printer;
        if (!request.task.printer && localPrinters.length > 0) {
            request.task.printer = localPrinters[0];
        }
        var doc = new Object();
        // doc.documentID = printTask[i];
        doc.documentID = new Date().getTime();
        var content = new Array();
        var waybillJson = getWaybillJson(printTask[i]);
        var customAreaData = getCustomAreaData(printTask[i]);
        //content.push(waybillJson, customAreaData);
        content.push(customAreaData);
        doc.contents = content;
        documents.push(doc);
    }
    while (length > 0) {
        printTask.shift();
        length--;
    }
    request.task.documents = documents;
    webSocket.send(JSON.stringify(request));
}

function printCainiaoSheet(printer, printdata) {
    if (!printdata) {
        return;
    }
    if (typeof printdata.content == 'string')
        printdata.content = JSON.parse(printdata.content);
    printdata.printer = printer;
    printTask.push(printdata);
    if (!webSocket || webSocket.readyState != 1) {
        doConnect();
    } else {
        doPrint(printer);
    }
}
function batchPrintCainiaoSheet(printdatas) {
    if (!printdatas) {
        return;
    }
    for (var i = 0; i < printdatas.length; i++) {
        var printdata = printdatas[i].printData;
        if (typeof printdata.content == 'string')
            printdata.content = JSON.parse(printdata.content);
        printdata.printer = printdatas[i].printer;
        printTask.push(printdata);
    }
    if (!webSocket || webSocket.readyState != 1) {
        doConnect();
    } else {
        doPrint('');
    }
}