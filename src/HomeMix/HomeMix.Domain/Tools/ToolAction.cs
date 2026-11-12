namespace HomeMix.Domain.Tools;

/// <summary>What a declared tool does with its collection (docs/homemix.md, "JARVIS").</summary>
public enum ToolAction
{
    Add,
    List,
    Get,
    Update,
    Delete,

    /// <summary>Runs the add-on's <c>tools/{name}.js</c> in Jint (A7).</summary>
    Script,
}
