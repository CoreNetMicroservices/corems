using CoreMs.Common.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Middleware;

/// <summary>
/// Middleware that automatically calls SaveChangesAsync at the end of a successful request.
/// Acts as an implicit unit of work per HTTP request — all tracked changes are flushed together.
/// If the request throws an exception, nothing is saved (automatic rollback).
/// </summary>
public class AutoSaveChangesMiddleware
{
    private readonly RequestDelegate _next;

    public AutoSaveChangesMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode < 400)
        {
            var db = context.RequestServices.GetService<CoreMsDbContext>();
            if (db != null && db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
