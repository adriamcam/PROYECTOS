using Microsoft.AspNetCore.Components;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Infrastructure;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Tabs;

public partial class ClientVirtualMachines
{
    [Inject]
    private VirtualMachineService VmService { get; set; } = default!;

    /*
     * Estos valores permiten probar inmediatamente con Automercado.
     * Cuando el componente padre envíe los parámetros, se utilizarán
     * automáticamente los valores seleccionados en el portal.
     */
    [Parameter]
    public string CustomerName { get; set; } = "Automercado";

    [Parameter]
    public string SubscriptionId { get; set; } =
        "2307c794-2423-448c-91aa-b8faab097b5d";

    protected List<VirtualMachineModel> VirtualMachines { get; set; } = [];

    protected bool IsLoading { get; set; } = true;

    protected string ErrorMessage { get; set; } = string.Empty;

    protected int TotalVMs => VirtualMachines.Count;

    protected int Running =>
        VirtualMachines.Count(vm =>
            IsRunningState(vm.DisplayPowerState));

    protected int Stopped =>
        VirtualMachines.Count(vm =>
            !IsRunningState(vm.DisplayPowerState));

    protected int Windows =>
        VirtualMachines.Count(vm =>
            vm.DisplayOSType.Contains(
                "Windows",
                StringComparison.OrdinalIgnoreCase));

    protected int Linux =>
        VirtualMachines.Count(vm =>
            vm.DisplayOSType.Contains(
                "Linux",
                StringComparison.OrdinalIgnoreCase));

    protected int TotalVCPUs =>
        VirtualMachines.Sum(vm => vm.DisplayVCPUs);

    protected decimal TotalMemoryGB =>
        VirtualMachines.Sum(vm => vm.DisplayMemoryGB);

    protected int TotalDisks =>
        VirtualMachines.Sum(vm => vm.DisplayDiskCount);

    protected decimal TotalDiskSizeGB =>
        VirtualMachines.Sum(vm => vm.DisplayDiskSizeGB);

    protected override async Task OnParametersSetAsync()
    {
        await LoadVirtualMachinesAsync();
    }

    private async Task LoadVirtualMachinesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        VirtualMachines = [];

        try
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                ErrorMessage = "No se recibió el nombre del cliente.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SubscriptionId))
            {
                ErrorMessage = "No se recibió la suscripción.";
                return;
            }

            VirtualMachines =
                await VmService.GetVirtualMachinesAsync(
                    CustomerName,
                    SubscriptionId);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"No fue posible cargar el inventario: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected static string StateCssClass(string state)
    {
        if (IsRunningState(state))
        {
            return "vm-state-running";
        }

        if (state.Contains(
                "deallocated",
                StringComparison.OrdinalIgnoreCase) ||
            state.Contains(
                "stopped",
                StringComparison.OrdinalIgnoreCase))
        {
            return "vm-state-stopped";
        }

        return "vm-state-neutral";
    }

    private static bool IsRunningState(string state)
    {
        return state.Contains(
                   "running",
                   StringComparison.OrdinalIgnoreCase) ||
               state.Contains(
                   "encendida",
                   StringComparison.OrdinalIgnoreCase);
    }
}
