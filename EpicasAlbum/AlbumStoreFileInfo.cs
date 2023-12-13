using System;
using System.IO;
using OWML.Common;

namespace EpicasAlbum;

public class AlbumStoreFileInfo
{
    public readonly string Name; // Only for convenience, we have the name already as key
    public readonly DateTime CreationTime;
    public readonly DateTime LastWriteTime;
    public readonly long Length;

    private AlbumStoreFileInfo(string name, DateTime creationTime, DateTime lastWriteTime, long length)
    {
        Name = name;
        CreationTime = creationTime;
        LastWriteTime = lastWriteTime;
        Length = length;
    }
    
    public static AlbumStoreFileInfo ForFile(FileInfo fileInfo)
    {
        try
        {
            return new AlbumStoreFileInfo(
                fileInfo.Name,
                fileInfo.CreationTimeUtc, 
                fileInfo.LastWriteTimeUtc,
                fileInfo.Length);
        }
        catch (FileNotFoundException)
        {
            // This could happen with the Lenght (maybe others?), return null to filter out
            return null;
        }
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AlbumStoreFileInfo)obj);
    }

    protected bool Equals(AlbumStoreFileInfo other)
    {
        return string.Equals(Name, other.Name, StringComparison.InvariantCulture) && 
               CreationTime.Equals(other.CreationTime) && 
               LastWriteTime.Equals(other.LastWriteTime) && 
               Length == other.Length;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Name != null ? StringComparer.InvariantCulture.GetHashCode(Name) : 0);
            hashCode = (hashCode * 397) ^ CreationTime.GetHashCode();
            hashCode = (hashCode * 397) ^ LastWriteTime.GetHashCode();
            hashCode = (hashCode * 397) ^ Length.GetHashCode();
            return hashCode;
        }
    }
}