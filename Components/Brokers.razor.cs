using KafkaTester.Model;
using Microsoft.AspNetCore.Components;

namespace KafkaTester.Components;

public partial class Brokers
{
    [Parameter]
    public Options Options { get; set; }
}
