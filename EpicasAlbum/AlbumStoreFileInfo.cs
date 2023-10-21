using System;

namespace EpicasAlbum;

public class AlbumStoreFileInfo
{
    public readonly DateTime CreationTime;
    public readonly DateTime LastWriteTime;
    public readonly long Length;

    public AlbumStoreFileInfo(DateTime creationTime, DateTime lastWriteTime, long length)
    {
        CreationTime = creationTime;
        LastWriteTime = lastWriteTime;
        Length = length;
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
        return CreationTime.Equals(other.CreationTime) && LastWriteTime.Equals(other.LastWriteTime) && Length == other.Length;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = CreationTime.GetHashCode();
            hashCode = (hashCode * 397) ^ LastWriteTime.GetHashCode();
            hashCode = (hashCode * 397) ^ Length.GetHashCode();
            return hashCode;
        }
    }
}