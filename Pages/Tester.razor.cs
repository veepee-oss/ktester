using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KafkaTester.Model;
using Microsoft.AspNetCore.Components;
using KafkaTester.Service;

namespace KafkaTester.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Tester
{
    [Inject] private KafkaTesterService TesterService { get; set; }

    private Options Options = new();
    private LinkedList<KafkaMessage> AllMessages = new();
    private ICollection<KafkaMessage> FilteredMessages => DoFilter();
    private List<string> Errors = new();

    private CancellationTokenSource _cancellationToken;

    private void Refresh()
    {
        StateHasChanged();
    }

    private async Task OnSearch()
    {
        try
        {
            if (Options.IsSearching && _cancellationToken is { IsCancellationRequested: false })
            {
                _cancellationToken.Cancel();
                Options.IsSearching = false;
                return;
            }

            AllMessages.Clear();
            Errors.Clear();
            using (_cancellationToken = new CancellationTokenSource(TimeSpan.FromDays(1)))
            {
                Options.IsSearching = true;
                DateTime lastStateHasChanged = DateTime.UtcNow;
                var isUIUpdated = true;
                // Async refresh UI when new message is received and UI is not updated
                new Thread(async () =>
                {
                    while (Options.IsSearching)
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
                await foreach (var message in TesterService.RunKafkaTesterServiceAsync(_cancellationToken, Guid.NewGuid().ToString(), Options.KafkaConfig.CurrentSetting, OnError))
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

                    AllMessages.AddFirst(message);
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
            Errors.Add(e.ToString());
        }
    }

    private ICollection<KafkaMessage> DoFilter()
    {
        if (IsFiltering())
        {
            StringComparison comparison = Options.Filter.IsInvariantCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            var filtered = AllMessages.AsEnumerable();
            if (Options.Filter.IsCheckKey && Options.Filter.IsCheckMessage)
                return filtered.Where(m => (m.Key?.Contains(Options.Filter.Text, comparison) ?? false) || (m.Message?.Contains(Options.Filter.Text, comparison) ?? false)).ToArray();
            if (Options.Filter.IsCheckKey)
                return filtered.Where(m => m.Key?.Contains(Options.Filter.Text, comparison) ?? false).ToArray();
            if (Options.Filter.IsCheckMessage)
                return filtered.Where(m => m.Message?.Contains(Options.Filter.Text, comparison) ?? false).ToArray();
        }

        return AllMessages.ToArray();
    }

    private bool IsFiltering()
    {
        return !string.IsNullOrWhiteSpace(Options.Filter.Text);
    }

    private void OnError(string error)
    {
        _ = InvokeAsync(() =>
        {
            Errors.Add(error);
            StateHasChanged();
        });
    }
}