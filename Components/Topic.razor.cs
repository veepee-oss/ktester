using KafkaTester.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using KafkaTester.Service;

namespace KafkaTester.Components;

public partial class Topic
{
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] private KafkaTesterService TesterService { get; set; }

    [Parameter]
    public Options Options { get; set; }


    private List<string> _topics;
    private List<string> _filteredTopics;
    private bool _isTopicLoading = false;

    async Task OnTypeTopic(ChangeEventArgs e)
    {
        if (e != null)
        {
            Options.KafkaConfig.CurrentSetting.Topic = e.Value.ToString();
        }

        var filter = Options.KafkaConfig.CurrentSetting.Topic;
        await JsRuntime.InvokeVoidAsync("console.log", filter);
        await LoadTopics(null);
        _filteredTopics = _topics?.Where(t => t.Contains(filter ?? string.Empty)).ToList();

        if (_filteredTopics?.Count == 1 && filter.Equals(_filteredTopics[0], StringComparison.InvariantCultureIgnoreCase))
            _filteredTopics = null;
    }

    async Task OnLostTopicFocus(FocusEventArgs e)
    {
        await Task.Delay(200).ContinueWith(t => _filteredTopics = null);
    }

    async Task OnGetTopicFocus(FocusEventArgs e)
    {
        await OnTypeTopic(null);
    }

    private async Task LoadTopics(MouseEventArgs e)
    {
        if (_topics != null || _isTopicLoading || string.IsNullOrWhiteSpace(Options.KafkaConfig.CurrentSetting.Brokers))
            return;

        _isTopicLoading = true;
        var topics = await TesterService.GetTopicsAsync(Options.KafkaConfig.CurrentSetting);
        _topics = topics;
        _isTopicLoading = false;
        StateHasChanged();
    }

    private async Task SelectionTopic(string topic)
    {
        Options.KafkaConfig.CurrentSetting.Topic = topic;
        StateHasChanged();
        _filteredTopics = null;
        await JsRuntime.InvokeVoidAsync("closeTopicSelectionModal");
    }
}
