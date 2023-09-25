using System.Text.Json;

namespace HamedStack.SystemTextJson.JsonPath;

internal static class PathGenerator
{
    internal static JsonLocationNode Generate(JsonLocationNode lastNode,
        int index,
        ProcessingFlags options)
    {
        return (options & ProcessingFlags.Path) != 0 ? new JsonLocationNode(lastNode, index) : lastNode;
    }

    internal static JsonLocationNode Generate(JsonLocationNode lastNode,
        string identifier,
        ProcessingFlags options)
    {
        if ((options & ProcessingFlags.Path) != 0)
        {
            return new JsonLocationNode(lastNode, identifier);
        }

        return lastNode;
    }
}

internal interface ISelector
{
    void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth);

    bool TryEvaluate(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue value);

    void AppendSelector(ISelector tail);

    bool IsRoot();
}

internal abstract class BaseSelector : ISelector
{
    ISelector? Tail { get; set; }

    public abstract void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth);

    public abstract bool TryEvaluate(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue value);

    public void AppendSelector(ISelector tail)
    {
        if (Tail == null)
        {
            Tail = tail;
        }
        else
        {
            Tail.AppendSelector(tail);
        }
    }

    protected void TailSelect(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (Tail == null)
        {
            receiver.Add(lastNode, current);
        }
        else
        {
            Tail.Select(resources, root, lastNode, current, receiver, options, depth);
        }
    }

    protected bool TryEvaluateTail(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue value)
    {
        if (Tail == null)
        {
            value = current;
            return true;
        }

        return Tail.TryEvaluate(resources, root, lastNode, current, options, out value);
    }

    public virtual bool IsRoot()
    {
        return false;
    }
}

internal sealed class RootSelector : BaseSelector
{
    readonly int _id;

    internal RootSelector(int id)
    {
        _id = id;
    }

    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        TailSelect(resources, root, lastNode, root, receiver, options, depth);
    }
    public override bool TryEvaluate(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue result)
    {
        if (resources.TryRetrieveFromCache(_id, out result!))
        {
            return true;
        }

        if (!TryEvaluateTail(resources, root, lastNode, root, options, out result))
        {
            result = JsonConstants.Null;
            return false;
        }
        resources.AddToCache(_id, result);
        return true;
    }

    public override bool IsRoot()
    {
        return true;
    }

    public override string ToString()
    {
        return "RootSelector";
    }
}

internal sealed class CurrentNodeSelector : BaseSelector
{
    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        TailSelect(resources, root, lastNode, current, receiver, options, depth);
    }
    public override bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue value)
    {
        return TryEvaluateTail(resources, root, lastNode, current, options, out value);
    }

    public override bool IsRoot()
    {
        return true;
    }

    public override string ToString()
    {
        return "CurrentNodeSelector";
    }
}

internal sealed class ParentNodeSelector : BaseSelector
{
    readonly int _ancestorDepth;

    internal ParentNodeSelector(int ancestorDepth)
    {
        _ancestorDepth = ancestorDepth;
    }

    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        var ancestor = lastNode;
        var index = 0;
        while (ancestor != null && index < _ancestorDepth)
        {
            ancestor = ancestor.Parent;
            ++index;
        }

        if (ancestor != null)
        {
            var path = new JsonLocation(ancestor);
            if (TryGetValue(root, path, out var value))
            {
                TailSelect(resources, root, path.Last, value, receiver, options, depth);
            }
        }
    }
    public override bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue result)
    {
        var ancestor = lastNode;
        var index = 0;
        while (ancestor != null && index < _ancestorDepth)
        {
            ancestor = ancestor.Parent;
            ++index;
        }

        if (ancestor != null)
        {
            var path = new JsonLocation(ancestor);
            if (TryGetValue(root, path, out var value))
            {

                return TryEvaluateTail(resources, root, path.Last, value, options, out result);
            }

            result = JsonConstants.Null;
            return true;
        }

        result = JsonConstants.Null;
        return true;
    }

    bool TryGetValue(IValue root, JsonLocation path, out IValue element)
    {
        element = root;
        foreach (var pathComponent in path)
        {
            if (pathComponent.ComponentKind == JsonLocationNodeKind.Index)
            {
                if (element.ValueKind != JsonValueKind.Array || pathComponent.GetIndex() >= element.GetArrayLength())
                {
                    return false;
                }
                element = element[pathComponent.GetIndex()];
            }
            else if (pathComponent.ComponentKind == JsonLocationNodeKind.Name)
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(pathComponent.GetName(), out element))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public override string ToString()
    {
        return "RootSelector";
    }
}

internal sealed class IdentifierSelector : BaseSelector
{
    readonly string _identifier;

    internal IdentifierSelector(string identifier)
    {
        _identifier = identifier;
    }

    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (current.ValueKind == JsonValueKind.Object)
        {
            if (current.TryGetProperty(_identifier, out var value))
            {
                TailSelect(resources, root,
                    PathGenerator.Generate(lastNode, _identifier, options),
                    value, receiver, options, depth);
            }
        }
    }

    public override bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue value)
    {
        if (current.ValueKind == JsonValueKind.Object)
        {
            if (current.TryGetProperty(_identifier, out var element))
            {
                return TryEvaluateTail(resources, root,
                    PathGenerator.Generate(lastNode, _identifier, options),
                    element, options, out value);
            }

            value = JsonConstants.Null;
            return true;
        }

        if (current.ValueKind == JsonValueKind.Array && _identifier == "length")
        {
            value = new DecimalValue(new decimal(current.GetArrayLength()));
            return true;
        }
        if (current.ValueKind == JsonValueKind.String && _identifier == "length")
        {
            value = new DecimalValue(new decimal(current.GetString().Length));
            return true;
        }
        value = JsonConstants.Null;
        return true;
    }

    public override string ToString()
    {
        return $"IdentifierSelector {_identifier}";
    }
}

internal sealed class IndexSelector : BaseSelector
{
    readonly int _index;

    internal IndexSelector(int index)
    {
        _index = index;
    }

    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (current.ValueKind == JsonValueKind.Array)
        {
            if (_index >= 0 && _index < current.GetArrayLength())
            {
                TailSelect(resources, root,
                    PathGenerator.Generate(lastNode, _index, options),
                    current[_index], receiver, options, depth);
            }
            else
            {
                var index = current.GetArrayLength() + _index;
                if (index >= 0 && index < current.GetArrayLength())
                {
                    TailSelect(resources, root,
                        PathGenerator.Generate(lastNode, _index, options),
                        current[index], receiver, options, depth);
                }
            }
        }
    }

    public override bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue value)
    {
        if (current.ValueKind == JsonValueKind.Array)
        {
            if (_index >= 0 && _index < current.GetArrayLength())
            {
                return TryEvaluateTail(resources, root,
                    PathGenerator.Generate(lastNode, _index, options),
                    current[_index], options, out value);
            }

            var index = current.GetArrayLength() + _index;
            if (index >= 0 && index < current.GetArrayLength())
            {
                return TryEvaluateTail(resources, root,
                    PathGenerator.Generate(lastNode, _index, options),
                    current[index], options, out value);
            }

            value = JsonConstants.Null;
            return true;
        }

        value = JsonConstants.Null;
        return true;
    }

    public override string ToString()
    {
        return $"IndexSelector {_index}";
    }
}

internal sealed class SliceSelector : BaseSelector
{
    readonly Slice _slice;

    internal SliceSelector(Slice slice)
    {
        _slice = slice;
    }

    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (current.ValueKind == JsonValueKind.Array)
        {
            var start = _slice.GetStart(current.GetArrayLength());
            var end = _slice.GetStop(current.GetArrayLength());
            var step = _slice.Step;

            if (step > 0)
            {
                if (start < 0)
                {
                    start = 0;
                }
                if (end > current.GetArrayLength())
                {
                    end = current.GetArrayLength();
                }
                for (var i = start; i < end; i += step)
                {
                    TailSelect(resources, root,
                        PathGenerator.Generate(lastNode, i, options),
                        current[i], receiver, options, depth);
                }
            }
            else if (step < 0)
            {
                if (start >= current.GetArrayLength())
                {
                    start = current.GetArrayLength() - 1;
                }
                if (end < -1)
                {
                    end = -1;
                }
                for (var i = start; i > end; i += step)
                {
                    if (i < current.GetArrayLength())
                    {
                        TailSelect(resources, root,
                            PathGenerator.Generate(lastNode, i, options),
                            current[i], receiver, options, depth);
                    }
                }
            }
        }
    }

    public override bool TryEvaluate(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue results)
    {
        var elements = new List<IValue>();
        INodeReceiver receiver = new ValueReceiver(elements);
        if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
        {
            receiver = new SynchronizedNodeReceiver(receiver);
        }
        Select(resources,
            root,
            lastNode,
            current,
            receiver,
            options,
            0);
        results = new ArrayValue(elements);
        return true;
    }

    public override string ToString()
    {
        return "SliceSelector";
    }
}

internal sealed class RecursiveDescentSelector : BaseSelector
{
    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (depth >= resources.Options.MaxDepth)
        {
            throw new InvalidOperationException("Maximum depth level exceeded in recursive descent selector.");
        }

        switch (current.ValueKind)
        {
            case JsonValueKind.Array:
            {
                TailSelect(resources, root, lastNode, current, receiver, options, depth + 1);
                var index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    Select(resources, root,
                        PathGenerator.Generate(lastNode, index, options),
                        item, receiver, options, depth + 1);
                    ++index;
                }

                break;
            }
            case JsonValueKind.Object:
            {
                TailSelect(resources, root, lastNode, current, receiver, options, depth + 1);
                foreach (var prop in current.EnumerateObject())
                {
                    Select(resources, root,
                        PathGenerator.Generate(lastNode, prop.Name, options),
                        prop.Value, receiver, options, depth + 1);
                }

                break;
            }
        }
    }
    public override bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue results)
    {
        var elements = new List<IValue>();
        INodeReceiver receiver = new ValueReceiver(elements);
        if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
        {
            receiver = new SynchronizedNodeReceiver(receiver);
        }
        Select(resources,
            root,
            lastNode,
            current,
            receiver,
            options,
            0);
        results = new ArrayValue(elements);
        return true;
    }

    public override string ToString()
    {
        return "RecursiveDescentSelector";
    }
}

internal sealed class WildcardSelector : BaseSelector
{
    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (current.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in current.EnumerateArray())
            {
                TailSelect(resources, root,
                    PathGenerator.Generate(lastNode, index, options),
                    item, receiver, options, depth);
                ++index;
            }
        }
        else if (current.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in current.EnumerateObject())
            {
                TailSelect(resources, root,
                    PathGenerator.Generate(lastNode, prop.Name, options),
                    prop.Value, receiver, options, depth);
            }
        }
    }
    public override bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue results)
    {
        var elements = new List<IValue>();
        INodeReceiver receiver = new ValueReceiver(elements);
        if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
        {
            receiver = new SynchronizedNodeReceiver(receiver);
        }
        Select(resources,
            root,
            lastNode,
            current,
            receiver,
            options,
            0);
        results = new ArrayValue(elements);
        return true;
    }

    public override string ToString()
    {
        return "WildcardSelector";
    }
}

internal sealed class UnionSelector : ISelector
{
    readonly IList<ISelector> _selectors;
    ISelector? _tail;

    internal UnionSelector(IList<ISelector> selectors)
    {
        _selectors = selectors;
        _tail = null;
    }

    public void AppendSelector(ISelector tail)
    {
        if (_tail == null)
        {
            _tail = tail;
            foreach (var selector in _selectors)
            {
                selector.AppendSelector(tail);
            }
        }
        else
        {
            _tail.AppendSelector(tail);
        }
    }

    public void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (resources.Options.ExecutionMode == PathExecutionMode.Sequential)
        {
            foreach (var selector in _selectors)
            {
                selector.Select(resources, root, lastNode, current, receiver, options, depth);
            }
        }
        else
        {
            Parallel.For(0, _selectors.Count, i => _selectors[i].Select(resources, root, lastNode, current, receiver, options, depth));
        }
    }

    public bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue results)
    {
        var elements = new List<IValue>();
        INodeReceiver receiver = new ValueReceiver(elements);
        if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
        {
            receiver = new SynchronizedNodeReceiver(receiver);
        }
        Select(resources,
            root,
            lastNode,
            current,
            receiver,
            options,
            0);
        results = new ArrayValue(elements);
        return true;
    }

    public bool IsRoot()
    {
        return false;
    }

    public override string ToString()
    {
        return "UnionSelector";
    }
}

internal sealed class FilterSelector : BaseSelector
{
    readonly IExpression _expr;

    internal FilterSelector(IExpression expr)
    {
        _expr = expr;
    }

    public override void Select(DynamicResources resources,
        IValue root,
        JsonLocationNode lastNode,
        IValue current,
        INodeReceiver receiver,
        ProcessingFlags options,
        int depth)
    {
        if (current.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in current.EnumerateArray())
            {
                if (_expr.TryEvaluate(resources, root, item, options, out var val)
                    && Expression.IsTrue(val))
                {
                    TailSelect(resources, root,
                        PathGenerator.Generate(lastNode, index, options),
                        item, receiver, options, depth);
                }
                ++index;
            }
        }
        else if (current.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in current.EnumerateObject())
            {
                if (_expr.TryEvaluate(resources, root, property.Value, options, out var val)
                    && Expression.IsTrue(val))
                {
                    TailSelect(resources, root,
                        PathGenerator.Generate(lastNode, property.Name, options),
                        property.Value, receiver, options, depth);
                }
            }
        }
    }

    public override bool TryEvaluate(DynamicResources resources, IValue root,
        JsonLocationNode lastNode,
        IValue current,
        ProcessingFlags options,
        out IValue results)
    {
        var elements = new List<IValue>();
        INodeReceiver receiver = new ValueReceiver(elements);
        if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
        {
            receiver = new SynchronizedNodeReceiver(receiver);
        }
        Select(resources,
            root,
            lastNode,
            current,
            receiver,
            options,
            0);
        results = new ArrayValue(elements);
        return true;
    }

    public override string ToString()
    {
        return "FilterSelector";
    }
}


