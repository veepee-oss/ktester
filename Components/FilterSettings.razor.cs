using KafkaTester.Model;
using Microsoft.AspNetCore.Components;

namespace KafkaTester.Components;

public partial class FilterSettings
{
    private string _oldFilterValue;

    [Parameter]
    public FilterSettingsModel Filter { get; set; }

    [Parameter]
    public EventCallback<FilterSettingsModel> OnChanged { get; set; }

    private void OnFilterChange()
    {
        if (_oldFilterValue == Filter.Text)
            return;
        _oldFilterValue = Filter.Text;
        OnChanged.InvokeAsync();
    }
}
