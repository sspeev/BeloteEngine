using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

public class ScrollAnimationJsInterop
{
    private readonly IJSRuntime _jsRuntime;

    public ScrollAnimationJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask InitScrollAnimations()
    {
        return _jsRuntime.InvokeVoidAsync("initScrollAnimations");
    }
}