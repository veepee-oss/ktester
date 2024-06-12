using KafkaTester.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace KafkaTester.Components;

public partial class Settings
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public KafkaSettingsModel Setting { get; set; }

    [Parameter]
    public EventCallback<KafkaSetting> OnChanged { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Read();
            StateHasChanged();
        }
    }

    private void OnSelectSetting(ChangeEventArgs e)
    {
        var selectedString = e.Value.ToString();
        if (!Setting.KafkaSettings.TryGetValue(selectedString, out KafkaSetting setting)) return;

        Setting.CurrentSetting = setting;
        OnChanged.InvokeAsync();
    }

    private async Task Read()
    {
        Setting.KafkaSettings = await GetLocalStorageAsync<Dictionary<string, KafkaSetting>>("KafkaSettings");
        if (Setting.KafkaSettings != null && Setting.KafkaSettings.Count > 0 && Setting.KafkaSettings.First().Value.Name == null)
        {
            foreach (var (key, value) in Setting.KafkaSettings)
            {
                value.Name = key;
            }
        }
    }

    private async Task<T> GetLocalStorageAsync<T>(string key)
    {
        bool isPrimitive = typeof(T).IsPrimitive || typeof(T).IsValueType || typeof(T) == typeof(string);
        var result = await JsRuntime.InvokeAsync<string>("localStorage.getItem", new object[] { key });
        if (result == null)
            return (T)Activator.CreateInstance(typeof(T));
        return isPrimitive ? (T)Convert.ChangeType(result, typeof(T)) : JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(result)));
    }

    private async Task SaveLocalStorageAsync<T>(string key, T value)
    {
        if (value == null)
            return;

        bool isPrimitive = typeof(T).IsPrimitive || typeof(T).IsValueType || typeof(T) == typeof(string);
        await JsRuntime.InvokeAsync<string>("localStorage.setItem",
            new object[] { key, isPrimitive ? value : Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))) });
    }

    private async Task Save()
    {
        var name = Setting.CurrentSetting.Name;
        var settings = await GetLocalStorageAsync<Dictionary<string, KafkaSetting>>("KafkaSettings");

        if (settings.ContainsKey(name))
        {
            settings[name] = Setting.CurrentSetting;
        }
        else
        {
            settings.Add(name, Setting.CurrentSetting);
        }
        await SaveLocalStorageAsync("KafkaSettings", settings);
        await Read();
        await JsRuntime.InvokeVoidAsync("closeSaveSettingModal");
    }

    private async Task Delete()
    {
        Setting.KafkaSettings.Remove(Setting.CurrentSetting.Name);
        await SaveLocalStorageAsync("KafkaSettings", Setting.KafkaSettings);
        Setting.CurrentSetting = new KafkaSetting();
        await JsRuntime.InvokeVoidAsync("closeDeleteSettingModal");
        await Read();
    }
}
