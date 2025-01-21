using KafkaTester.Model;
using KafkaTester.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Web;

namespace KafkaTester.Components;

public partial class SearchButton
{
    [Inject] private KafkaTesterService TesterService { get; set; }
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }

    [Parameter]
    public Options Options { get; set; }

    [Parameter]
    public EventCallback OnSearch { get; set; }

    private KafkaMessage _newMessage = new KafkaMessage();
    private string _newMessageText;
    private string _exportConfigurationString;
    private string _importConfigurationString;
    private string _shareConfigurationString;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && SetShareSetting())
        {
            StateHasChanged();
            await OnSearch.InvokeAsync();
        }
    }

    private void OnNewMessageChange()
    {
        StateHasChanged();
    }

    private async Task ExportConfiguration()
    {
        _exportConfigurationString = await GetLocalStorageAsync<string>("KafkaSettings");
    }

    private void CreateHeader()
    {
        _newMessage.Headers.Add(new KafkaHeader());
    }

    private void RemoveHeader(KafkaHeader header)
    {
        _newMessage.Headers.Remove(header);
    }

    private async Task SendMessage()
    {
        _newMessage.Message = Encoding.UTF8.GetBytes(_newMessageText);
        await TesterService.SendMessageAsync(Options.KafkaConfig.CurrentSetting.Brokers, Options.KafkaConfig.CurrentSetting.Topic, _newMessage);
        _newMessage = new KafkaMessage();
        await JsRuntime.InvokeVoidAsync("closeSendMessageModal");
    }

    private async Task ImportConfiguration()
    {
        await SaveLocalStorageAsync("KafkaSettings", _importConfigurationString);
    }

    private bool SetShareSetting()
    {
        var builder = new UriBuilder(NavigationManager.Uri);
        var query = HttpUtility.ParseQueryString(builder.Query);

        if (query["b"] == null || query["t"] == null)
            return false;

        Options.KafkaConfig.CurrentSetting.Brokers = query["b"];
        Options.KafkaConfig.CurrentSetting.Topic = query["t"];
        if (query["t"] != null)
            Options.Filter.Text = query["f"];

        return true;
    }

    private void OnShare()
    {
        _shareConfigurationString = GetSharedSetting();
    }

    private string GetSharedSetting()
    {
        var builder = new UriBuilder(NavigationManager.BaseUri);
        var query = HttpUtility.ParseQueryString(builder.Query);
        query["b"] = Options.KafkaConfig.CurrentSetting.Brokers;
        query["t"] = Options.KafkaConfig.CurrentSetting.Topic;
        if (!string.IsNullOrWhiteSpace(Options.Filter.Text))
            query["f"] = Options.Filter.Text;
        builder.Query = query.ToString();

        return builder.ToString();
    }

    private async Task SaveLocalStorageAsync<T>(string key, T value)
    {
        if (value == null)
            return;

        bool isPrimitive = typeof(T).IsPrimitive || typeof(T).IsValueType || typeof(T) == typeof(string);
        await JsRuntime.InvokeAsync<string>("localStorage.setItem",
            new object[] { key, isPrimitive ? value : Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))) });
    }

    private async Task<T> GetLocalStorageAsync<T>(string key)
    {
        bool isPrimitive = typeof(T).IsPrimitive || typeof(T).IsValueType || typeof(T) == typeof(string);
        var result = await JsRuntime.InvokeAsync<string>("localStorage.getItem", new object[] { key });
        if (result == null)
            return (T)Activator.CreateInstance(typeof(T));
        return isPrimitive ? (T)Convert.ChangeType(result, typeof(T)) : JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    private void CopyMessage(string message)
    {
        JsRuntime.InvokeVoidAsync("clipboardCopy.copyText", message).ConfigureAwait(true);
    }
}
