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
using static System.Net.WebRequestMethods;

namespace KafkaTester.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class Tester
    {
        [Inject] private KafkaTesterService TesterService { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        private DotNetObjectReference<Tester>? objRef;

        // private OrderingEnum _ordering = OrderingEnum.Desc;
        private string _oldFilterValue;
        private string _saveSettingName;
        private KafkaSetting _setting = new();
        private bool _isSearch;
        private CancellationTokenSource _cancellationToken;
        private KafkaMessage _newMessage = new KafkaMessage();
        private KafkaMessage _selectedMessage = new KafkaMessage();
        private bool _isFiltered = false;
        private readonly LinkedList<KafkaMessage> _messages = new();
        private ICollection<KafkaMessage> _filterMessages => _isFiltered ? _messages.Where(DoFilter).ToArray() : _messages;
        private Dictionary<string, KafkaSetting> _kafkaSettings = new();
        private string _selectedSetting;
        private string _shareConfigurationString;
        private string _exportConfigurationString;
        private string _importConfigurationString;
        private readonly List<string> _errors = new();
        private List<string> _topics;
        private List<string> _filteredTopics;
        private bool _isTopicLoading = false;

        protected override async Task OnAfterRenderAsync(bool isFirstRender)
        {
            if (isFirstRender)
            {
                await Read();
                StateHasChanged();

                if (SetShareSetting())
                {
                    StateHasChanged();
                    await OnSearch();
                    StateHasChanged();
                }
            }
        }

        private async Task OnSearch()
        {
            try
            {
                if (_isSearch && _cancellationToken is { IsCancellationRequested: false })
                {
                    _cancellationToken?.Cancel();
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

                    await foreach (var message in TesterService.RunKafkaTesterServiceAsync(_cancellationToken, Guid.NewGuid().ToString(), _setting.Brokers, _setting.Topic, OnError))
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

        private void OnFilterChange()
        {
            _isFiltered = !string.IsNullOrEmpty(_setting.Filter);

            if (_oldFilterValue == _setting.Filter)
                return;
            _oldFilterValue = _setting.Filter;
            StateHasChanged();
        }

        private void OnNewMessageChange()
        {
            StateHasChanged();
        }

        private void CopyMessage(string message)
        {
            JsRuntime.InvokeVoidAsync("clipboardCopy.copyText", message).ConfigureAwait(true);
        }

        private async Task Save()
        {
            var settings = await GetLocalStorageAsync<Dictionary<string, KafkaSetting>>("KafkaSettings");

            if (settings.ContainsKey(_saveSettingName))
            {
                settings[_saveSettingName] = _setting;
            }
            else
            {
                settings.Add(_saveSettingName, _setting);
            }
            await SaveLocalStorageAsync("KafkaSettings", settings);
            await Read();
            _selectedSetting = _saveSettingName;
            _saveSettingName = null;
            await JsRuntime.InvokeVoidAsync("closeSaveSettingModal");
        }

        private async Task Read()
        {
            _kafkaSettings = await GetLocalStorageAsync<Dictionary<string, KafkaSetting>>("KafkaSettings");
        }

        private async Task Delete()
        {
            _kafkaSettings.Remove(_selectedSetting);
            await SaveLocalStorageAsync("KafkaSettings", _kafkaSettings);
            _selectedSetting = string.Empty;
            _setting = new KafkaSetting();
            await JsRuntime.InvokeVoidAsync("closeDeleteSettingModal");
            await Read();
        }

        private void OnSelectSetting(ChangeEventArgs e)
        {
            var selectedString = e.Value.ToString();
            _selectedSetting = selectedString;
            if (!_kafkaSettings.TryGetValue(selectedString, out KafkaSetting setting)) return;

            _setting = setting;
        }

        async Task OnTypeTopic(ChangeEventArgs e)
        {
            if (e != null)
            {
                _setting.Topic = e.Value.ToString();
            }

            var filter = _setting.Topic;
            await JsRuntime.InvokeVoidAsync("console.log", filter);
            await LoadTopics(null);
            _filteredTopics = _topics?.Where(t => t.Contains(filter ?? string.Empty)).ToList();

            if (_filteredTopics?.Count == 1 && filter.Equals(_filteredTopics.First(), StringComparison.InvariantCultureIgnoreCase))
                _filteredTopics = null;
        }

        async Task OnLostTopicFocus(FocusEventArgs e)
        {
            await Task.Delay(200).ContinueWith(t => _filteredTopics = null);
        }

        async Task  OnGetTopicFocus(FocusEventArgs e)
        {
            await OnTypeTopic(null);
        }

        private async Task LoadTopics(MouseEventArgs e)
        {
            if (_topics != null || _isTopicLoading || string.IsNullOrWhiteSpace(_setting.Brokers))
                return;

            _isTopicLoading = true;
            var topics = await TesterService.GetTopicsAsync(_setting.Brokers);
            _topics = topics;
            _isTopicLoading = false;
            StateHasChanged();
        }

        private async Task SelectionTopic(string topic)
        {
            _setting.Topic = topic;
            StateHasChanged();
            _filteredTopics = null;
            await JsRuntime.InvokeVoidAsync("closeTopicSelectionModal");
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
            await TesterService.SendMessageAsync(_setting.Brokers, _setting.Topic, _newMessage);
            _newMessage = new KafkaMessage();
            await JsRuntime.InvokeVoidAsync("closeSendMessageModal");
        }

        private async Task ShowSelectedMessage(KafkaMessage message)
        {
            _selectedMessage = message;
            await JsRuntime.InvokeVoidAsync("openSeeMessageModal", JsonPrettify(_selectedMessage.Message));
        }

        private bool DoFilter(KafkaMessage message)
        {
            return !IsFiltering()
                   || IsFiltering() && message.Message.Contains(_setting.Filter, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsFiltering()
        {
            return !string.IsNullOrWhiteSpace(_setting.Filter);
        }

        private void OnError(string error)
        {
            _ = InvokeAsync(() =>
            {
                _errors.Add(error);
                StateHasChanged();
            });
        }

        private async Task ExportConfiguration()
        {
            _exportConfigurationString = await GetLocalStorageAsync<string>("KafkaSettings");
        }

        private async Task ImportConfiguration()
        {
            await SaveLocalStorageAsync("KafkaSettings", _importConfigurationString);
            await Read();
        }

        private void OnSave()
        {
            _saveSettingName = _selectedSetting;
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

        private string JsonPrettify(string json)
        {
            System.Diagnostics.Trace.WriteLine(json);
            if (IsValidJson(json))
                return JsonSerializer.Serialize(JsonDocument.Parse(json), new JsonSerializerOptions { WriteIndented = true });
            else
                return json;
        }

        private bool IsValidJson(string source)
        {
            if (source == null)
                return false;

            try
            {
                JsonDocument.Parse(source);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private bool SetShareSetting()
        {
            var builder = new UriBuilder(NavigationManager.Uri);
            var query = HttpUtility.ParseQueryString(builder.Query);

            if (query["b"] == null || query["t"] == null)
                return false;

            _setting.Brokers = query["b"];
            _setting.Topic = query["t"];
            if (query["t"] != null)
                _setting.Filter = query["f"];

            return true;
        }

        private string GetSharedSetting()
        {
            var builder = new UriBuilder(NavigationManager.BaseUri);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["b"] = _setting.Brokers;
            query["t"] = _setting.Topic;
            if (!string.IsNullOrWhiteSpace(_setting.Filter))
                query["f"] = _setting.Filter;
            builder.Query = query.ToString();

            return builder.ToString();
        }
    }
}