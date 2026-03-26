using Iris.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Iris.Api.Hubs;

public sealed class VehicleHub : Hub
{
    private readonly IVehicleStore _store;

    public VehicleHub(IVehicleStore store)
    {
        _store = store;
    }

    public override async Task OnConnectedAsync()
    {
        // Send full current state to the newly connected client
        var snapshots = _store.GetAll();
        await Clients.Caller.SendAsync("VehicleUpdate", snapshots);
        await base.OnConnectedAsync();
    }
}
