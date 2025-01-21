window.clipboardCopy = {
    copyText: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            console.log("Copied to clipboard!");
        })
        .catch(function (error) {
            alert(error);
        });
    }
};

window.closeSaveSettingModal = () => {
    $('#saveSettingModal').modal('hide');
};

window.closeDeleteSettingModal = () => {
    $('#deleteSettingModal').modal('hide');
};

window.closeSendMessageModal = () => {
    $('#sendMessageModal').modal('hide');
};

window.showExportConfigurationModal = () => {
    $('#exportConfigurationModal').modal('show');
};

window.closeTopicSelectionModal = () => {
    $('#topicSelectionModal').modal('hide');
};

window.openSeeMessageModal = (message, shouldHighlight) => {
    if (shouldHighlight) {
        $("#seeMessageModal pre code").html(hljs.highlightAuto(message).value);
    } else {
        $("#seeMessageModal pre code").html(message);
    }
    $('#seeMessageModal').modal('show');
};

$("#seeMessageModal").on("hidden.bs.modal", function () {
    $("#seeMessageModal pre code").html("");
});