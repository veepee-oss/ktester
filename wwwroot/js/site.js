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
window.openSeeMessageModal = () => {

    if (!shownEventLoaded) {
        $('#seeMessageModal').on('shown.bs.modal', function() {
            hljs.highlightAll();
        });
        shownEventLoaded = true;
    }

    $('#seeMessageModal').modal('show');
};