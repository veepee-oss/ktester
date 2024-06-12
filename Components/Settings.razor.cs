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
    public Options Options { get; set; }

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
        if (!Options.KafkaConfig.ListKafkaSettings.TryGetValue(selectedString, out KafkaSetting setting)) return;

        Options.KafkaConfig.CurrentSetting = setting;
        OnChanged.InvokeAsync();
    }

    private async Task Read()
    {
        Options.KafkaConfig.ListKafkaSettings = await GetLocalStorageAsync<Dictionary<string, KafkaSetting>>("KafkaSettings");
        if (Options.KafkaConfig.ListKafkaSettings != null && Options.KafkaConfig.ListKafkaSettings.Count > 0 && Options.KafkaConfig.ListKafkaSettings.First().Value.Name == null)
        {
            foreach (var (key, value) in Options.KafkaConfig.ListKafkaSettings)
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
        var name = Options.KafkaConfig.CurrentSetting.Name;
        var settings = await GetLocalStorageAsync<Dictionary<string, KafkaSetting>>("KafkaSettings");

        if (settings.ContainsKey(name))
        {
            settings[name] = Options.KafkaConfig.CurrentSetting;
        }
        else
        {
            settings.Add(name, Options.KafkaConfig.CurrentSetting);
        }
        await SaveLocalStorageAsync("KafkaSettings", settings);
        await Read();
        await JsRuntime.InvokeVoidAsync("closeSaveSettingModal");
    }

    private async Task Delete()
    {
        Options.KafkaConfig.ListKafkaSettings.Remove(Options.KafkaConfig.CurrentSetting.Name);
        await SaveLocalStorageAsync("KafkaSettings", Options.KafkaConfig.ListKafkaSettings);
        Options.KafkaConfig.CurrentSetting = new KafkaSetting();
        await JsRuntime.InvokeVoidAsync("closeDeleteSettingModal");
        await Read();
    }
}
