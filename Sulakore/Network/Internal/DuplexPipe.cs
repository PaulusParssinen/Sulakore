// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines;

internal sealed class DuplexPipe(PipeReader input, PipeWriter output) : IDuplexPipe
{
    public PipeReader Input { get; } = input;
    public PipeWriter Output { get; } = output;

    public static (IDuplexPipe Transport, IDuplexPipe Application) CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new Pipe(inputOptions);
        var output = new Pipe(outputOptions);

        DuplexPipe transportToApplication = new(output.Reader, input.Writer);
        DuplexPipe applicationToTransport = new(input.Reader, output.Writer);

        return (applicationToTransport, transportToApplication);
    }
}
