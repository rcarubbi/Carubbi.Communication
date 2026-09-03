using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Carubbi.Communication.NamedPipe;

[SupportedOSPlatform("windows")]
public static class NamedPipeSecurity
{
    public static PipeSecurity AllowEveryone()
    {
        var security = Create();
        var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        AddAccessRule(security, everyone, PipeAccessRights.ReadWrite, AccessControlType.Allow);
        return security;
    }

    public static PipeSecurity AllowCurrentUser()
    {
        var security = Create();
        var user = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current user security identifier could not be resolved.");
        AddAccessRule(security, user, PipeAccessRights.ReadWrite, AccessControlType.Allow);
        return security;
    }

    public static PipeSecurity Create()
        => new();

    private static void AddAccessRule(PipeSecurity security, SecurityIdentifier sid, PipeAccessRights rights, AccessControlType type)
        => security.AddAccessRule(new PipeAccessRule(sid, rights, type));
}
