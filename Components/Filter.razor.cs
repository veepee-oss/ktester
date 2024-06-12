using KafkaTester.Model;
using Microsoft.AspNetCore.Components;

namespace KafkaTester.Components;

public partial class Filter
{
    private string _oldFilterValue;

    [Parameter]
    public Options Options { get; set; }

    [Parameter]
    public EventCallback<FilterSettings> OnChanged { get; set; }

    private void OnFilterChange()
    {
        if (_oldFilterValue == Options.Filter.Text)
            return;
        _oldFilterValue = Options.Filter.Text;
        OnChanged.InvokeAsync();
    }
}
