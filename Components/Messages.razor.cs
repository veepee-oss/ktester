using KafkaTester.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace KafkaTester.Components;

public partial class Messages
{
    [Inject] private IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public ICollection<KafkaMessage> Items { get; set; }

    [Parameter]
    public List<string> Errors { get; set; }

    [Parameter]
    public KafkaMessage SelectedMessage { get; set; }

    [Parameter]
    public Options Options { get; set; }

    [Parameter]
    public int CountTotalMessages { get; set; }

    private async Task ShowSelectedMessage(KafkaMessage message)
    {
        SelectedMessage = message;
        await JsRuntime.InvokeVoidAsync("openSeeMessageModal",
            JsonPrettify(SelectedMessage.Message.ToText(tryDecompression: Options.KafkaConfig.CurrentSetting.IsTryDecompressMessage)),
            message.Message.Length < 100000);
    }

    private string JsonPrettify(string json)
    {
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
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            return false;
        }
    }

    private void CopyMessage(string message)
    {
        JsRuntime.InvokeVoidAsync("clipboardCopy.copyText", message).ConfigureAwait(true);
    }
}
