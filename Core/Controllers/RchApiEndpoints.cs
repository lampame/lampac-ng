using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Services;
using Shared.Services.Pools;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Core.Endpoints
{
    public static class RchApiEndpoints
    {
        const long maxRequestSize = 10 * 1024 * 1024;

        public static void MapRchApi(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("rch/result", WriteResult)
                .AllowAnonymous()
                .WithMetadata(new RequestSizeLimitAttribute(maxRequestSize));

            endpoints.MapPost("rch/gzresult", WriteZipResult)
                .AllowAnonymous()
                .WithMetadata(new RequestSizeLimitAttribute(maxRequestSize));
        }


        static async Task<IResult> WriteResult(HttpContext context, [FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return Results.BadRequest(401);

            if (!RchClient.rchIds.TryGetValue(id, out var rchHub) || context.Request.ContentLength > maxRequestSize)
                return Results.BadRequest(403);

            try
            {
                using (var byteBuf = new BufferBytePool(BufferBytePool.sizeSmall))
                {
                    int bytesRead;
                    var memBuf = byteBuf.Memory;

                    while ((bytesRead = await context.Request.Body.ReadAsync(memBuf, rchHub.ct).ConfigureAwait(false)) > 0)
                        rchHub.ms.Write(memBuf.Span.Slice(0, bytesRead));
                }

                rchHub.ms.Position = 0;

                rchHub.tcs.TrySetResult(null);
                return Results.Ok();
            }
            catch
            {
                rchHub.ms.SetLength(0);
                rchHub.tcs.TrySetResult(null);
            }

            return Results.BadRequest(400);
        }


        static async Task<IResult> WriteZipResult(HttpContext context, [FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return Results.BadRequest(401);

            if (!RchClient.rchIds.TryGetValue(id, out var rchHub) || context.Request.ContentLength > maxRequestSize)
                return Results.BadRequest(403);

            try
            {
                using (var gzip = new GZipStream(context.Request.Body, CompressionMode.Decompress, leaveOpen: true))
                {
                    using (var byteBuf = new BufferBytePool(BufferBytePool.sizeSmall))
                    {
                        int bytesRead;
                        var memBuf = byteBuf.Memory;

                        while ((bytesRead = await gzip.ReadAsync(memBuf, rchHub.ct).ConfigureAwait(false)) > 0)
                            rchHub.ms.Write(memBuf.Span.Slice(0, bytesRead));
                    }

                    rchHub.ms.Position = 0;

                    rchHub.tcs.TrySetResult(null);
                    return Results.Ok();
                }
            }
            catch
            {
                rchHub.ms.SetLength(0);
                rchHub.tcs.TrySetResult(null);
            }

            return Results.BadRequest(400);
        }
    }
}
