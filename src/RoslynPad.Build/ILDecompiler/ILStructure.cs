﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Diagnostics;

using Mono.Cecil.Cil;

namespace RoslynPad.Build.ILDecompiler;

/// <summary>
/// Specifies the type of an IL structure.
/// </summary>
internal enum ILStructureType
{
    /// <summary>
    /// The root block of the method
    /// </summary>
    Root,
    /// <summary>
    /// A nested control structure representing a loop.
    /// </summary>
    Loop,
    /// <summary>
    /// A nested control structure representing a try block.
    /// </summary>
    Try,
    /// <summary>
    /// A nested control structure representing a catch, finally, or fault block.
    /// </summary>
    Handler,
    /// <summary>
    /// A nested control structure representing an exception filter block.
    /// </summary>
    Filter
}

/// <summary>
/// An IL structure.
/// </summary>
internal class ILStructure
{
    public readonly ILStructureType Type;

    /// <summary>
    /// Start position of the structure.
    /// </summary>
    public readonly int StartOffset;

    /// <summary>
    /// End position of the structure. (exclusive)
    /// </summary>
    public readonly int EndOffset;

    /// <summary>
    /// The exception handler associated with the Try, Filter or Handler block.
    /// </summary>
    public readonly ExceptionHandler? ExceptionHandler;

    /// <summary>
    /// The loop's entry point.
    /// </summary>
    public readonly Instruction? LoopEntryPoint;

    /// <summary>
    /// The list of child structures.
    /// </summary>
    public readonly List<ILStructure> Children = [];

    public ILStructure(MethodBody body)
        : this(ILStructureType.Root, 0, body.CodeSize)
    {
        // Build the tree of exception structures:
        for (int i = 0; i < body.ExceptionHandlers.Count; i++)
        {
            ExceptionHandler eh = body.ExceptionHandlers[i];
            if (!body.ExceptionHandlers.Take(i).Any(oldEh => oldEh.TryStart == eh.TryStart && oldEh.TryEnd == eh.TryEnd))
                AddNestedStructure(new ILStructure(ILStructureType.Try, eh.TryStart.Offset, eh.TryEnd.Offset, eh));
            if (eh.HandlerType == ExceptionHandlerType.Filter)
                AddNestedStructure(new ILStructure(ILStructureType.Filter, eh.FilterStart.Offset, eh.HandlerStart.Offset, eh));
            AddNestedStructure(new ILStructure(ILStructureType.Handler, eh.HandlerStart.Offset, eh.HandlerEnd?.Offset ?? body.CodeSize, eh));
        }
        // Very simple loop detection: look for backward branches
        List<KeyValuePair<Instruction, Instruction>> allBranches = FindAllBranches(body);
        // We go through the branches in reverse so that we find the biggest possible loop boundary first (think loops with "continue;")
        for (int i = allBranches.Count - 1; i >= 0; i--)
        {
            int loopEnd = allBranches[i].Key.GetEndOffset();
            int loopStart = allBranches[i].Value.Offset;
            if (loopStart < loopEnd)
            {
                // We found a backward branch. This is a potential loop.
                // Check that is has only one entry point:
                Instruction? entryPoint = null;

                // entry point is first instruction in loop if prev inst isn't an unconditional branch
                Instruction prev = allBranches[i].Value.Previous;
                if (prev != null && !IsUnconditionalBranch(prev.OpCode))
                    entryPoint = allBranches[i].Value;

                bool multipleEntryPoints = false;
                foreach (KeyValuePair<Instruction, Instruction> pair in allBranches)
                {
                    if ((pair.Key.Offset < loopStart || pair.Key.Offset >= loopEnd) &&
                        (loopStart <= pair.Value.Offset && pair.Value.Offset < loopEnd))
                    {
                        // jump from outside the loop into the loop
                        if (entryPoint == null)
                            entryPoint = pair.Value;
                        else if (pair.Value != entryPoint)
                            multipleEntryPoints = true;
                    }
                }
                if (!multipleEntryPoints)
                {
                    AddNestedStructure(new ILStructure(ILStructureType.Loop, loopStart, loopEnd, entryPoint));
                }
            }
        }
        SortChildren();
    }

    public ILStructure(ILStructureType type, int startOffset, int endOffset, ExceptionHandler? handler = null)
    {
        Debug.Assert(startOffset < endOffset);
        Type = type;
        StartOffset = startOffset;
        EndOffset = endOffset;
        ExceptionHandler = handler;
    }

    public ILStructure(ILStructureType type, int startOffset, int endOffset, Instruction? loopEntryPoint)
    {
        Debug.Assert(startOffset < endOffset);
        Type = type;
        StartOffset = startOffset;
        EndOffset = endOffset;
        LoopEntryPoint = loopEntryPoint;
    }

    public static bool IsUnconditionalBranch(OpCode opcode)
    {
        if (opcode.OpCodeType == OpCodeType.Prefix)
            return false;
        return opcode.FlowControl switch
        {
            FlowControl.Branch or FlowControl.Throw or FlowControl.Return => true,
            FlowControl.Next or FlowControl.Call or FlowControl.Cond_Branch => false,
            _ => throw new NotSupportedException(opcode.FlowControl.ToString()),
        };
    }

    bool AddNestedStructure(ILStructure newStructure)
    {
        // special case: don't consider the loop-like structure of "continue;" statements to be nested loops
        if (Type == ILStructureType.Loop && newStructure.Type == ILStructureType.Loop && newStructure.StartOffset == StartOffset)
            return false;

        // use <= for end-offset comparisons because both end and EndOffset are exclusive
        Debug.Assert(StartOffset <= newStructure.StartOffset && newStructure.EndOffset <= EndOffset);
        foreach (ILStructure child in Children)
        {
            if (child.StartOffset <= newStructure.StartOffset && newStructure.EndOffset <= child.EndOffset)
            {
                return child.AddNestedStructure(newStructure);
            }
            if ((!(child.EndOffset <= newStructure.StartOffset || newStructure.EndOffset <= child.StartOffset)) &&
                (!(newStructure.StartOffset <= child.StartOffset && child.EndOffset <= newStructure.EndOffset)))
            {
                // child and newStructure overlap
                // Invalid nesting, can't build a tree. -> Don't add the new structure.
                return false;
            }
        }
        // Move existing structures into the new structure:
        for (int i = 0; i < Children.Count; i++)
        {
            ILStructure child = Children[i];
            if (newStructure.StartOffset <= child.StartOffset && child.EndOffset <= newStructure.EndOffset)
            {
#pragma warning disable S127 // "for" loop stop conditions should be invariant
                Children.RemoveAt(i--);
#pragma warning restore S127 // "for" loop stop conditions should be invariant
                newStructure.Children.Add(child);
            }
        }
        // Add the structure here:
        Children.Add(newStructure);
        return true;
    }

    /// <summary>
    /// Finds all branches. Returns list of source offset->target offset mapping.
    /// Multiple entries for the same source offset are possible (switch statements).
    /// The result is sorted by source offset.
    /// </summary>
    static List<KeyValuePair<Instruction, Instruction>> FindAllBranches(MethodBody body)
    {
        List<KeyValuePair<Instruction, Instruction>> result = [];
        foreach (var inst in body.Instructions)
        {
            switch (inst.OpCode.OperandType)
            {
                case OperandType.InlineBrTarget:
                case OperandType.ShortInlineBrTarget:
                    result.Add(new KeyValuePair<Instruction, Instruction>(inst, (Instruction)inst.Operand));
                    break;
                case OperandType.InlineSwitch:
                    foreach (Instruction target in (Instruction[])inst.Operand)
                        result.Add(new KeyValuePair<Instruction, Instruction>(inst, target));
                    break;
            }
        }
        return result;
    }

    void SortChildren()
    {
        Children.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        foreach (ILStructure child in Children)
            child.SortChildren();
    }

    /// <summary>
    /// Gets the innermost structure containing the specified offset.
    /// </summary>
    public ILStructure GetInnermost(int offset)
    {
        Debug.Assert(StartOffset <= offset && offset < EndOffset);
        foreach (ILStructure child in Children)
        {
            if (child.StartOffset <= offset && offset < child.EndOffset)
                return child.GetInnermost(offset);
        }
        return this;
    }
}
