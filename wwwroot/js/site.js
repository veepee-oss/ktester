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

var shownEventLoaded = false;
window.openSeeMessageModal = (dotNetHelper) => {

    if (!shownEventLoaded) {
        $('#seeMessageModal').on('shown.bs.modal', function() {
            dotNetHelper.invokeMethodAsync("GetMessage").then(message => {
                $("pre code").html(hljs.highlightAuto(message).value);
            });
        });
        shownEventLoaded = true;
    }

    $('#seeMessageModal').modal('show');
};