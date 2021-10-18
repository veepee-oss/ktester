using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KafkaTester.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using KafkaTester.Service;

namespace KafkaTester.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class Tester
    {
        [Inject] private KafkaTesterService TesterService { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }

        private const int MAX_DISPLAY_ELEMENT = 1000;
        // private OrderingEnum _ordering = OrderingEnum.Desc;
        private string _oldFilterValue;
        private string _saveSettingName;
        private KafkaSetting _setting = new();
        private bool _isSearch;
        private CancellationTokenSource _cancellationToken;
        private string _newMessage;
        private string _selectedMessage;
        private readonly LinkedList<KafkaMessage> _messages = new();
        private Dictionary<string, KafkaSetting> _kafkaSettings = new();
        private string _selectedSetting;
        private string _exportConfigurationString;
        private string _importConfigurationString;
        private readonly List<string> _errors = new();

        protected override async Task OnAfterRenderAsync(bool isFirstRender)
        {
            if (isFirstRender)
            {
                await Read();
                StateHasChanged();
            }
        }

        private async Task OnSearch()
        {
            try
            {
                if (_isSearch && _cancellationToken is {IsCancellationRequested: false})
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
                    await foreach (var message in TesterService.RunKafkaTesterServiceAsync(_cancellationToken, Guid.NewGuid().ToString(), _setting.Brokers, _setting.Topic, OnError))
                    {
                        if (_cancellationToken.IsCancellationRequested)
                            return;
                        
                        _messages.AddFirst(message);
                        StateHasChanged();
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

        private async Task SendMessage()
        {
            await TesterService.SendMessageAsync(_setting.Brokers, _setting.Topic, _newMessage);
            _newMessage = string.Empty;
            await JsRuntime.InvokeVoidAsync("closeSendMessageModal");
        }

        private async Task SeeMessage(KafkaMessage message)
        {
            if (IsValidJson(message.Message))
                _selectedMessage = JsonPrettify(message.Message);
            else
                _selectedMessage = message.Message;
            await JsRuntime.InvokeVoidAsync("openSeeMessageModal");
        }

        private bool DoFilter(KafkaMessage message)
        {
            return string.IsNullOrWhiteSpace(_setting.Filter)
                   || !string.IsNullOrWhiteSpace(_setting.Filter)
                   && message.Message.Contains(_setting.Filter, StringComparison.InvariantCultureIgnoreCase);
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

        private static string JsonPrettify(string json)
        {
            return JsonSerializer.Serialize(JsonDocument.Parse(json), new JsonSerializerOptions { WriteIndented = true });
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
    }
}