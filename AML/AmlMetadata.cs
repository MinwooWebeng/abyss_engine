using System.Numerics;

namespace AbyssCLI.AML;

/// <summary>
/// Metadata for AML documents.
/// Initiation setting, security policies, and other metadata 
/// that affects initial parsing and execution of the document.
/// </summary>
public class AmlMetadata
{
    public string title;
    public Vector3 pos;
    public Quaternion rot;
    public bool is_item;
    public string sharer_hash; // only if is_item is true
    public Guid uuid; // only if is_item is true
}
