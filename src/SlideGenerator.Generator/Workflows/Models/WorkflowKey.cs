/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowKey.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

// A workflow's identity within IWorkflowEngine<TKey,TState>: the request it belongs to, plus its ordinal
// position within that request. Aliased project-wide so every Engine/Runner/Observer type using
// it doesn't repeat the tuple shape.

global using WorkflowKey = (string RequestId, int WorkflowId);