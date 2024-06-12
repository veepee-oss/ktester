using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using KafkaTester.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using KafkaTester.Service;
using Microsoft.AspNetCore.Components.Web;

namespace KafkaTester.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Tester
{
    [Inject] private KafkaTesterService TesterService { get; set; }
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }

    private Options _options = new();
    private bool _isSearch;
    private CancellationTokenSource _cancellationToken;
    private KafkaMessage _newMessage = new KafkaMessage();
    private KafkaMessage _selectedMessage = new KafkaMessage();
    private readonly LinkedList<KafkaMessage> _messages = new();
    private ICollection<KafkaMessage> _filterMessages => DoFilter();
    private string _shareConfigurationString;
    private string _exportConfigurationString;
    private string _importConfigurationString;
    private readonly List<string> _errors = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && SetShareSetting())
        {
            StateHasChanged();
            await OnSearch();
        }
    }

    private void Refresh()
    {
        StateHasChanged();
    }

    private async Task OnSearch()
    {
        try
        {
            if (_isSearch && _cancellationToken is { IsCancellationRequested: false })
            {
                _cancellationToken.Cancel();
                _isSearch = false;
                return;
            }

            _messages.Clear();
            _errors.Clear();
            using (_cancellationToken = new CancellationTokenSource(TimeSpan.FromDays(1)))
            {
                _isSearch = true;
                DateTime lastStateHasChanged = DateTime.UtcNow;
                var isUIUpdated = true;
                // Async refresh UI when new message is received and UI is not updated
                new Thread(async () =>
                {
                    while (_isSearch)
                    {
                        if (!isUIUpdated)
                        {
                            await InvokeAsync(StateHasChanged);
                            isUIUpdated = true;
                        }
                        await Task.Delay(1000);
                    }
                }).Start();

                await InvokeAsync(StateHasChanged);
                await foreach (var message in TesterService.RunKafkaTesterServiceAsync(_cancellationToken, Guid.NewGuid().ToString(), _options.KafkaConfig.CurrentSetting, OnError))
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

                    _messages.AddFirst(message);
                    isUIUpdated = false;
                    if (DateTime.UtcNow - lastStateHasChanged > TimeSpan.FromMilliseconds(100))
                    {
                        StateHasChanged();
                        lastStateHasChanged = DateTime.UtcNow;
                        isUIUpdated = true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _errors.Add(e.ToString());
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
        await TesterService.SendMessageAsync(_options.KafkaConfig.CurrentSetting.Brokers, _options.KafkaConfig.CurrentSetting.Topic, _newMessage);
        _newMessage = new KafkaMessage();
        await JsRuntime.InvokeVoidAsync("closeSendMessageModal");
    }

    private ICollection<KafkaMessage> DoFilter()
    {
        if (IsFiltering())
        {
            StringComparison comparison = _options.Filter.IsInvariantCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            var filtered = _messages.AsEnumerable();
            if (_options.Filter.IsCheckKey && _options.Filter.IsCheckMessage)
                return filtered.Where(m => (m.Key?.Contains(_options.Filter.Text, comparison) ?? false) || (m.Message?.Contains(_options.Filter.Text, comparison) ?? false)).ToArray();
            if (_options.Filter.IsCheckKey)
                return filtered.Where(m => m.Key?.Contains(_options.Filter.Text, comparison) ?? false).ToArray();
            if (_options.Filter.IsCheckMessage)
                return filtered.Where(m => m.Message?.Contains(_options.Filter.Text, comparison) ?? false).ToArray();
        }

        return _messages.ToArray();
    }

    private bool IsFiltering()
    {
        return !string.IsNullOrWhiteSpace(_options.Filter.Text);
    }

    private void OnError(string error)
    {
        _ = InvokeAsync(() =>
        {
            _errors.Add(error);
            StateHasChanged();
        });
    }

    private async Task ImportConfiguration()
    {
        await SaveLocalStorageAsync("KafkaSettings", _importConfigurationString);
    }

    private void OnShare()
    {
        _shareConfigurationString = GetSharedSetting();
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

    private bool SetShareSetting()
    {
        var builder = new UriBuilder(NavigationManager.Uri);
        var query = HttpUtility.ParseQueryString(builder.Query);

        if (query["b"] == null || query["t"] == null)
            return false;

        _options.KafkaConfig.CurrentSetting.Brokers = query["b"];
        _options.KafkaConfig.CurrentSetting.Topic = query["t"];
        if (query["t"] != null)
            _options.Filter.Text = query["f"];

        return true;
    }

    private string GetSharedSetting()
    {
        var builder = new UriBuilder(NavigationManager.BaseUri);
        var query = HttpUtility.ParseQueryString(builder.Query);
        query["b"] = _options.KafkaConfig.CurrentSetting.Brokers;
        query["t"] = _options.KafkaConfig.CurrentSetting.Topic;
        if (!string.IsNullOrWhiteSpace(_options.Filter.Text))
            query["f"] = _options.Filter.Text;
        builder.Query = query.ToString();

        return builder.ToString();
    }
}