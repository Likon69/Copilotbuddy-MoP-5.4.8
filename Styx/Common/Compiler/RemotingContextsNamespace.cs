namespace System.Runtime.Remoting.Contexts
{
    /// <summary>
    /// .NET 10 has no System.Runtime.Remoting.Contexts. Singular 5.4.8 (Holy.cs, Arms.cs,
    /// Protection.cs) carries an unused <c>using</c> of it, which fails the runtime compile
    /// with CS0234. This placeholder keeps the namespace resolvable so the routine source
    /// ships unmodified; nothing uses the type.
    /// </summary>
    internal static class RemotingContextsNamespace
    {
    }
}
