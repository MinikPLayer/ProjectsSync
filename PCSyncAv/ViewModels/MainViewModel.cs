using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;

namespace PCSyncAv.ViewModels;

public class MainViewModel : ViewModelBase
{
    private bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public async void Pull()
    {
        Debug.WriteLine("[MOCK] Pull");
        IsBusy = true;
        await System.Threading.Tasks.Task.Delay(1000);
        IsBusy = false;
    }

    public async void Refresh()
    {
        IsBusy = true;
        await System.Threading.Tasks.Task.Delay(1000);
        IsBusy = false;
    }
}
