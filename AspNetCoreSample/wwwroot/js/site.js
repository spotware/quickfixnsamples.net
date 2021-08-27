function isNumberInRange(object) {
    var value = parseFloat(object.value);
    var step = parseFloat(object.step);

    value = value - floatSafeRemainder(value, step);

    object.value = value;

    if (value > parseFloat(object.max))
        object.value = object.max;
    else if (value < parseFloat(object.min))
        object.value = object.min;
}

function floatSafeRemainder(val, step) {
    var valDecCount = (val.toString().split('.')[1] || '').length;
    var stepDecCount = (step.toString().split('.')[1] || '').length;
    var decCount = valDecCount > stepDecCount ? valDecCount : stepDecCount;
    var valInt = parseInt(val.toFixed(decCount).replace('.', ''));
    var stepInt = parseInt(step.toFixed(decCount).replace('.', ''));
    return (valInt % stepInt) / Math.pow(10, decCount);
}

function isNumeric(evt) {
    var theEvent = evt || window.event;
    var key = theEvent.keyCode || theEvent.which;
    key = String.fromCharCode(key);
    var regex = /[0-9]|\./;
    if (!regex.test(key)) {
        theEvent.returnValue = false;
        if (theEvent.preventDefault) theEvent.preventDefault();
    }
}

function showApiCredentialsModal() {
    $("#apiCredentialsModal").modal({
        backdrop: 'static',
        keyboard: false
    });

    $('#apiCredentialsModal').modal('toggle')
}

function showLoadingModal() {
    $("#loadingModal").modal({
        backdrop: 'static',
        keyboard: false
    });

    $('#loadingModal').modal('toggle')
}

function onError(error) {
    console.error(`Error: ${error}`);

    var toastTemplate = $('#toast-template').contents().clone(true, true);

    toastTemplate.find('#toast-title').text('Error');
    toastTemplate.find('#toast-title-small').text(error.hasOwnProperty('type') ? error.type : 'N/A');
    toastTemplate.find('#toast-icon').addClass('fas fa-exclamation-triangle');
    toastTemplate.find('.toast-body').text(error.hasOwnProperty('message') ? error.message : error);

    var toast = toastTemplate.find(".toast");

    $('#toasts-container').append(toastTemplate);

    toast.toast({
        delay: 60000
    });

    $('.toast').toast('show');

    $('.toast').on('hidden.bs.toast', e => e.target.remove());
}

$(document).ready(function () {
    var connection = new signalR.HubConnectionBuilder().withUrl("/fixHub").build();

    connection.start().then(showApiCredentialsModal).catch(onError);

    $(document).on("click", "#apiCredentialsModalConnectButton", function () {
        $('#apiCredentialsModal').modal('hide');

        showLoadingModal();

        connection.invoke("Connect", {
            "QuoteHost": $("#apiQuoteHostInput").val(),
            "TradeHost": $("#apiTradeHostInput").val(),
            "QuotePort": parseInt($('#apiQuotePortInput').val()),
            "TradePort": parseInt($('#apiTradePortInput').val()),
            "QuoteSenderCompId": $("#apiQuoteSenderCompIdInput").val(),
            "TradeSenderCompId": $("#apiTradeSenderCompIdInput").val(),
            "QuoteSenderSubId": $("#apiQuoteSenderSubIdInput").val(),
            "TradeSenderSubId": $("#apiTradeSenderSubIdInput").val(),
            "QuoteTargetCompId": $("#apiQuoteTargetCompIdInput").val(),
            "TradeTargetCompId": $("#apiTradeTargetCompIdInput").val(),
            "QuoteTargetSubId": $("#apiQuoteTargetSubIdInput").val(),
            "TradeTargetSubId": $("#apiTradeTargetSubIdInput").val(),
            "Username": $("#apiUsernameInput").val(),
            "Password": $("#apiPasswordInput").val()
        }).catch(onError);
    });

    connection.on("Connected", function () {
        connection.stream("Logs")
            .subscribe({
                next: log => {
                    var row = `<tr>
                            <td>${log.time}</td>
                            <td>${log.type}</td>
                            <td>${log.message}</td></tr>`;

                    $('#logsTableBody').append(row);
                },
                complete: () => { },
                error: onError,
            });

        connection.stream("Symbols")
            .subscribe({
                next: symbol => {
                    var row = `<tr id="${symbol.id}">
                            <td>${symbol.id}</td>
                            <td id="name">${symbol.name}</td>
                            <td id="bid"></td>
                            <td id="ask"></td></tr>`;

                    $('#symbolsTableBody').append(row);
                },
                complete: () => {
                    var symbolOptions = '';

                    $('#symbolsTableBody > tr').each((index, element) => {
                        var name = $(element).find('#name').text();

                        symbolOptions += `<option value="${name}" id="${element.id}">${name}</option>`;
                    });

                    $('#orderSymbolsList').html(symbolOptions);

                    connection.stream("SymbolQuotes")
                        .subscribe({
                            next: quote => {
                                var bid = $('#symbolsTableBody > #' + quote.symbolId + ' > #bid');
                                var ask = $('#symbolsTableBody > #' + quote.symbolId + ' > #ask');

                                bid.html(quote.bid);
                                ask.html(quote.ask);
                            },
                            complete: () => { },
                            error: onError,
                        });
                },
                error: onError,
            });

        connection.stream("Positions")
            .subscribe({
                next: position => {
                    if (position == null) {
                        $('#positionsTableBody').html('');
                        return;
                    }
                    var row = `<tr id="${position.id}">
                            <td>${position.id}</td>
                            <td>${position.symbolId}</td>
                            <td>${position.symbolName}</td>
                            <td>${position.tradeSide}</td>
                            <td>${position.volume}</td>
                            <td>${position.entryPrice}</td>
                            <td>${position.stopLoss}</td>
                            <td>${position.takeProfit}</td>
                            </tr>`;

                    $('#positionsTableBody').append(row);
                },
                complete: () => { },
                error: onError,
            });

        connection.stream("ExecutionReport")
            .subscribe({
                next: executionReport => {
                    $(`#ordersTableBody tr#${executionReport.order.id}`).remove();

                    if (executionReport.type != '4' && executionReport.type != '8' && executionReport.type != 'C' && executionReport.type != 'F') {
                        var row = `<tr id="${executionReport.order.id}">
                            <td>${executionReport.order.id}</td>
                            <td>${executionReport.order.symbolId}</td>
                            <td>${executionReport.order.symbolName}</td>
                            <td>${executionReport.order.tradeSide}</td>
                            <td>${executionReport.order.volume}</td>
                            <td>${executionReport.order.type}</td>
                            <td>${executionReport.order.time}</td>
                            <td>${executionReport.order.targetPrice}</td>
                            <td>${executionReport.order.stopLossInPips}</td>
                            <td>${executionReport.order.takeProfitInPips}</td>
                            <td>${executionReport.order.expireTime == null ? '' : executionReport.order.expireTime}</td>
                            </tr>`;

                        $('#ordersTableBody').append(row);
                    }
                },
                complete: () => { },
                error: onError,
            });

        $('#newOrderButton').click(() => {
            connection.invoke("SendNewOrderRequest", {
                "Type": $('#orderTypeList').val(),
                "ClOrdId": $('#orderClOrdIdInput').val(),
                "SymbolId": parseInt($('#orderSymbolsList').find('option:selected').attr('id')),
                "TradeSide": $('#orderDirectionList').val(),
                "Quantity": parseFloat($('#orderQuantityInput').val()),
                "TargetPrice": $('#orderPriceInput').val() == "0" ? null : parseFloat($('#orderPriceInput').val()),
                "Expiry": $('#orderExpiryDateTime').val() == "" ? null : new Date($('#orderExpiryDateTime').val()).toISOString(),
                "PositionId": $('#orderPositionIdInput').val() == "0" ? null : parseInt($('#orderPositionIdInput').val()),
                "Designation": $('#orderDesignationTextarea').val()
            }).catch(onError);
        });

        $('#loadingModal').modal('hide');
    });
});