using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ReplayAssistant.Core;

/// <summary>
/// User logon scheduled task. Does not appear under Task Manager → Startup
/// (unlike a Startup folder .lnk or HKCU Run). The process is still visible
/// in Processes as Replay Assistant.
/// </summary>
public static class LogonTask
{
    public const string TaskName = "Replay Assistant";

    public static void Register(string targetExePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetExePath);
        var type =
            Type.GetTypeFromProgID("Schedule.Service")
            ?? throw new InvalidOperationException("Task Scheduler COM is unavailable.");
        dynamic service = Activator.CreateInstance(type)!;
        try
        {
            service.Connect();
            dynamic folder = service.GetFolder("\\");
            dynamic def = service.NewTask(0);
            def.RegistrationInfo.Description = "Starts Replay Assistant when you sign in.";
            def.Settings.Enabled = true;
            def.Settings.Hidden = false;
            def.Settings.AllowDemandStart = true;
            def.Settings.DisallowStartIfOnBatteries = false;
            def.Settings.StopIfGoingOnBatteries = false;
            def.Settings.StartWhenAvailable = true;
            def.Settings.ExecutionTimeLimit = "PT0S";
            def.Settings.MultipleInstances = 2;
            def.Principal.LogonType = 3;
            def.Principal.RunLevel = 0;
            dynamic trigger = def.Triggers.Create(9);
            trigger.Enabled = true;
            var sid = WindowsIdentity.GetCurrent().User?.Value;
            if (!string.IsNullOrWhiteSpace(sid))
            {
                trigger.UserId = sid;
            }

            dynamic action = def.Actions.Create(0);
            action.Path = targetExePath;
            action.WorkingDirectory = Path.GetDirectoryName(targetExePath) ?? "";
            folder.RegisterTaskDefinition(TaskName, def, 6, null, null, 3);
        }
        finally
        {
            Marshal.FinalReleaseComObject(service);
        }
    }

    public static void Remove()
    {
        var type = Type.GetTypeFromProgID("Schedule.Service");
        if (type is null)
        {
            return;
        }

        dynamic service = Activator.CreateInstance(type)!;
        try
        {
            service.Connect();
            dynamic folder = service.GetFolder("\\");
            try
            {
                folder.DeleteTask(TaskName, 0);
            }
            catch (COMException)
            {
                // Task was not registered.
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(service);
        }
    }
}
