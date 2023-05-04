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

var shownEventLoaded = false;
window.openSeeMessageModal = (message) => {

    if (!shownEventLoaded) {
        $('#seeMessageModal').on('shown.bs.modal', function() {
            $("#seeMessageModal pre code").html(hljs.highlightAuto(message).value)
        });
        $('#seeMessageModal').on('hidden.bs.modal', function () {
            $("#seeMessageModal pre code").html("Loading...")
        });
        shownEventLoaded = true;
    }

    $('#seeMessageModal').modal('show');
};
