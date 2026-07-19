using System.Text;
using System.Xml.Serialization;
using HekCoreApi.Adapters.Erms.Auth;
using HekCoreApi.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// ERMS eReferrals' compat Authenticate endpoint - XML request/response body shape preserved
/// exactly (ERMS_doc.md), including the legacy 200-with-Error-body failure behavior. Namespaced
/// "/erms/authenticate" for the same single-host reason documented on KaroCompatController.
/// </summary>
[ApiController]
[Route("erms")]
public sealed class ErmsCompatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ErmsCompatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("authenticate")]
    [Consumes("application/xml", "text/xml")]
    public async Task<IActionResult> Authenticate(CancellationToken ct)
    {
        ErmsCredential? credential;
        try
        {
            var serializer = new XmlSerializer(typeof(ErmsCredential));
            credential = (ErmsCredential?)serializer.Deserialize(Request.Body);
        }
        catch (InvalidOperationException)
        {
            credential = null;
        }

        if (credential is null)
        {
            return XmlResult(new ErmsErrorResponse());
        }

        var canonicalRequest = ErmsCredentialTranslator.ToCanonical(credential);
        var result = await _mediator.Send(new AuthenticateCommand(canonicalRequest, ErmsCredentialTranslator.Origin), ct);

        return result is { Succeeded: true, Token: not null }
            ? XmlResult(ErmsCredentialTranslator.ToLegacy(result.Token))
            : XmlResult(new ErmsErrorResponse());
    }

    private ContentResult XmlResult<T>(T value)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, value);
        return new ContentResult
        {
            Content = writer.ToString(),
            ContentType = "application/xml; charset=utf-16",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
