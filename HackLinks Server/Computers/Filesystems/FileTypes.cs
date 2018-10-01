namespace HackLinks_Server.Computers.Filesystems
{
    /// <summary>FilType determines how a file will be handled by the system</summary>
    public enum FileType
    {
        Regular,
        Directory,
        Link,
        Special, // E.G. Character devices, Block devices, and Sockets
    }
}
