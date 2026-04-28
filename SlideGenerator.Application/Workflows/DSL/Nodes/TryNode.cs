namespace SlideGenerator.Application.Workflows.DSL.Nodes;

/// <summary>
///     Wraps the body in a try/catch boundary. When an exception occurs it is logged to
///     <see cref="IActivityContext.State" /> and swallowed so the enclosing loop can continue.
///     If <see cref="Catch" /> is provided it executes in a child scope after logging.
///     If <see cref="ExceptionVar" /> is also provided the caught exception is stored in that
///     variable so the catch node can read it via <see cref="IActivityContext.GetVariable{TVar}" />.
/// </summary>
/// <param name="Body">The primary node to execute inside the try block.</param>
/// <param name="Catch">Optional node to run after an exception is caught and logged.</param>
/// <param name="ExceptionVar">
///     Optional variable key into which the caught exception is stored so <paramref name="Catch" /> can inspect it.
/// </param>
public record TryNode(
    WorkflowNode Body,
    WorkflowNode? Catch = null,
    Variable<Exception>? ExceptionVar = null) : WorkflowNode;
